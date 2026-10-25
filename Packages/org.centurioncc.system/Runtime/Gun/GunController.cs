using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
namespace CenturionCC.System.Gun
{
    [Flags]
    public enum VRActionType
    {
        None = 0,
        InputJump = 1,
        InputUse = 2,
        InputLookDown = 4,
        InputLookUp = 8,
        GunDirectionDown = 16,
        GunDirectionUp = 32,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunController : GunManagerCallbackBase
    {
        private const string LeftHandTrigger = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        private const string RightHandTrigger = "Oculus_CrossPlatform_SecondaryIndexTrigger";

        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;
        [SerializeField]
        private VRActionType reloadActionType = VRActionType.InputJump | VRActionType.GunDirectionDown;
        [SerializeField]
        private VRActionType fireModeActionType = VRActionType.InputJump;
        [SerializeField]
        private bool allowVRInteractionOnDesktop;

        private float _currentPrimaryXOffset;
        private int _isActionPerformed;

        private int _lastIsActionPressed;
        private GunBase[] _offsetAppliedGuns = new GunBase[0];
        private int _wasPerformedThisFrame;

        public float CurrentPrimaryXOffset
        {
            get => _currentPrimaryXOffset;
            set
            {
                _currentPrimaryXOffset = value;
                ApplyPrimaryXOffset();
            }
        }

        public VRActionType ReloadAction
        {
            get => reloadActionType;
            set => reloadActionType = value;
        }

        public VRActionType FireModeAction
        {
            get => fireModeActionType;
            set => fireModeActionType = value;
        }


        private void Start()
        {
            gunManager.Subscribe(this);
        }

        public override void PostLateUpdate()
        {
            var isUserInVR = Networking.LocalPlayer.IsUserInVR();
            HandleVRInputs(isUserInVR);
            HandleDesktopInputs(isUserInVR);

            // Dropping replaces the array, so iterating the current one is safe
            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                // Locally held guns include ones local player is only supporting, which may become local after polling
                gun._PollHolders();
                RefreshPrimaryXOffset(gun);

                if (gun.IsLocal) UpdateLocalGun(gun);
            }
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            _isActionPerformed = BitFlag.Set(_isActionPerformed, (int)VRActionType.InputJump, value);
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            _isActionPerformed = BitFlag.Set(_isActionPerformed, (int)VRActionType.InputUse, value);
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            _isActionPerformed = BitFlag.Set(_isActionPerformed, (int)VRActionType.InputLookDown, value < -0.5f);
            _isActionPerformed = BitFlag.Set(_isActionPerformed, (int)VRActionType.InputLookUp, value > 0.5f);
        }

        private void InputGunDirection()
        {
            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                if (!gun.IsLocal) continue;

                // UdonSharp cannot discard a variable with _
                // ReSharper disable once UnusedVariable
                gun._GetFiringPositionAndRotation(out var firingPos, out var firingRot);
                var gunForward = firingRot * Vector3.forward;
                _isActionPerformed = BitFlag.Set(_isActionPerformed, (int)VRActionType.GunDirectionUp, Vector3.Dot(Vector3.up, gunForward) > 0.5f);
                _isActionPerformed = BitFlag.Set(_isActionPerformed, (int)VRActionType.GunDirectionDown, Vector3.Dot(Vector3.down, gunForward) > 0.5f);
                break;
            }
        }

        private void HandleVRInputs(bool isUserInVR)
        {
            if (!isUserInVR && !allowVRInteractionOnDesktop)
            {
                return;
            }

            InputGunDirection();
            _wasPerformedThisFrame = (_lastIsActionPressed ^ _isActionPerformed) & _isActionPerformed;
            _lastIsActionPressed = _isActionPerformed;

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            if (_wasPerformedThisFrame != 0)
            {
                Debug.Log($"[GunController] wasPressedThisFrame: {_wasPerformedThisFrame}, isActionProcessed: {_isActionPerformed}");
            }
#endif

            if (WasPerformedThisFrame((int)fireModeActionType))
            {
                CycleFireMode();
            }

            if (WasPerformedThisFrame((int)reloadActionType))
            {
                SimplifiedReload();
            }
        }

        private void HandleDesktopInputs(bool isUserInVR)
        {
            if (isUserInVR && !Input.GetKey(KeyCode.LeftShift))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                CycleFireMode();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                SimplifiedReload();
            }

            var scrollDelta = Input.GetAxisRaw("Mouse ScrollWheel") * 80F;
            if (!Mathf.Approximately(scrollDelta, 0))
            {
                CurrentPrimaryXOffset += scrollDelta;
            }
        }

        /// <summary>
        /// Supporting holders must not apply their offset, so track which guns have it applied as they switch between local and supporting.
        /// </summary>
        private void RefreshPrimaryXOffset(GunBase gun)
        {
            if (!gun.IsLocal)
            {
                _offsetAppliedGuns = _offsetAppliedGuns.RemoveItem(gun);
                return;
            }

            if (_offsetAppliedGuns.ContainsItem(gun)) return;

            _offsetAppliedGuns = _offsetAppliedGuns.AddAsSet(gun);
            ApplyPrimaryXOffsetTo(gun);
        }

        private void ApplyPrimaryXOffsetTo(GunBase gun)
        {
            gun.PositioningHelper.SetPrimaryXAngleOffset(_currentPrimaryXOffset);
            gun.PositioningHelper._RequestSync();
        }

        /// <summary>
        /// Per-frame work for a gun the local player is in control of.
        /// </summary>
        private static void UpdateLocalGun(GunBase gun)
        {
            gun.AnimationHelper._SetTriggerProgress(GetMainTriggerPull(gun));
            gun.PositioningHelper._UpdatePosition();

            foreach (var behaviour in gun.Behaviours)
            {
                if (!behaviour) continue;
                behaviour.OnGunUpdate(gun);
            }

            if (gun.IsInWall)
            {
                PlayInWallHaptic(gun.MainHandle);
                PlayInWallHaptic(gun.SubHandle);
            }
        }

        private static void PlayInWallHaptic(GunHandle handle)
        {
            if (handle && handle.IsPickedUpLocally)
                Networking.LocalPlayer.PlayHapticEventInHand(handle.CurrentHand, .2F, .02F, .1F);
        }

        [PublicAPI]
        public void SimplifiedReload()
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log("[GunController] SimplifiedReload");
#endif

            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                if (!gun.IsLocal || gun.ReloadType == ReloadType.None)
                {
                    continue;
                }

                gun.ReloadHelper._DoSimplifiedReload();
            }
        }

        [PublicAPI]
        public void CycleFireMode()
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log("[GunController] CycleFireMode");
#endif

            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                if (!gun.IsLocal) continue;
                gun.CurrentFireModeIndex++;
            }
        }

        private void ApplyPrimaryXOffset()
        {
            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                if (!gun.IsLocal) continue;
                ApplyPrimaryXOffsetTo(gun);
            }
        }

        private bool WasPerformedThisFrame(int flag)
        {
            return (_wasPerformedThisFrame & flag) != 0 && (_isActionPerformed & flag) == flag;
        }


        private static float GetMainTriggerPull(GunBase gun)
        {
            if (!gun.IsLocal || !gun.MainHandle.IsPickedUpLocally) return 0F;

            if (!gun.IsVR)
                return gun.Trigger == TriggerState.Fired || gun.Trigger == TriggerState.Firing ? 1 : 0;

            return gun.MainHandle.CurrentHand == VRC_Pickup.PickupHand.Left
                ? Input.GetAxis(LeftHandTrigger)
                : Input.GetAxis(RightHandTrigger);
        }

        public override void OnPickedUpLocally(GunBase instance)
        {
            RefreshPrimaryXOffset(instance);
        }

        public override void OnDropLocally(GunBase instance)
        {
            _offsetAppliedGuns = _offsetAppliedGuns.RemoveItem(instance);
        }
    }
}
