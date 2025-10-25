using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunController : GunManagerCallbackBase
    {
        private const string LeftHandTrigger = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        private const string RightHandTrigger = "Oculus_CrossPlatform_SecondaryIndexTrigger";

        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        private float _currentPrimaryXOffset;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        private void Update()
        {
            HandleDesktopInputs();
            HandleLocalHeldGuns();
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value)
                return;

            CycleFireMode();
        }

        private void HandleDesktopInputs()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                CycleFireMode();
            }

            var scrollDelta = Input.GetAxisRaw("Mouse ScrollWheel") * 80F;
            if (!Mathf.Approximately(scrollDelta, 0))
            {
                _currentPrimaryXOffset += scrollDelta;
                ApplyPrimaryXOffset();
            }
        }

        private void HandleLocalHeldGuns()
        {
            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                gun.AnimationHelper._SetTriggerProgress(GetMainTriggerPull(gun));

                foreach (var behaviour in gun.Behaviours)
                {
                    behaviour.OnGunUpdate(gun);
                }

                if (gun.IsInWall)
                {
                    if (gun.MainHandle.IsPickedUpLocally)
                        Networking.LocalPlayer.PlayHapticEventInHand(gun.MainHandle.CurrentHand, .2F, .02F, .1F);
                    if (gun.SubHandle && gun.SubHandle.IsPickedUpLocally)
                        Networking.LocalPlayer.PlayHapticEventInHand(gun.SubHandle.CurrentHand, .2F, .02F, .1F);
                }
            }
        }

        private void CycleFireMode()
        {
            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                gun.FireMode = GunUtility.CycleFireMode(gun.FireMode, gun.AvailableFireModes);
            }
        }

        private void ApplyPrimaryXOffset()
        {
            foreach (var gun in gunManager.GetLocallyHeldGunInstances())
            {
                gun.PositioningHelper.SetPrimaryXAngleOffset(_currentPrimaryXOffset);
            }
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
    }
}
