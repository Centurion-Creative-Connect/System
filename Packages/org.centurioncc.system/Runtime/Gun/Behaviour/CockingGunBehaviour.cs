using CenturionCC.System.Gun.DataStore;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CockingGunBehaviour : GunBehaviourBase
    {
        [SerializeField]
        private bool returnToCockingPositionOnDrop;

        [Header("Cocking")]
        [SerializeField]
        private bool canCockAfterCock;
        [SerializeField]
        private bool isBlowBack;
        [SerializeField]
        private bool isDoubleAction;
        [SerializeField]
        private Transform cockingPosition;
        [SerializeField]
        private float cockingLength;
        [SerializeField] [Range(0, 1)]
        private float cockingMargin;
        [SerializeField] [Range(0, 1)]
        private float cockingAutoLoadMargin;

        [Header("Twisting")]
        [SerializeField]
        private bool requireTwist;
        [SerializeField]
        private Transform idleTwistPosition;
        [SerializeField]
        private Transform activeTwistPosition;
        [SerializeField]
        private float twistAngleOffset;
        [SerializeField]
        private float twistMaxAngle;
        [SerializeField]
        private float twistMinAngle;
        [SerializeField] [Range(0, 1)]
        private float twistMargin;
        [SerializeField] [Range(0, 1)]
        private float twistAutoLoadMargin;

        [Header("Desktop")]
        [SerializeField]
        private bool canDesktopCock = true;
        [SerializeField]
        private KeyCode desktopCockingKey = KeyCode.F;
        [SerializeField]
        private float desktopCockingTime = 1F;

        [Header("Haptic")]
        [SerializeField]
        private GunCockingHapticDataStore cockingHapticData;

        private float _desktopCockingTimer;
        private bool _isDesktopCockingCurrentlyPulling;

        private bool _isOnDesktopCocking;

        private void Start()
        {
            // assign placeholders
            if (idleTwistPosition == null)
                idleTwistPosition = cockingPosition;
            if (activeTwistPosition == null)
                activeTwistPosition = cockingPosition;
        }

        private float GetProgressNormalized(Vector3 gunHandleLocalPosition)
        {
            return GetProgress(gunHandleLocalPosition) / cockingLength;
        }

        private float GetProgress(Vector3 gunHandleLocalPosition)
        {
            return Mathf.Clamp(cockingPosition.localPosition.z - gunHandleLocalPosition.z, 0, cockingLength);
        }

        private float GetTwistNormalized(Vector3 gunHandleLocalPosition)
        {
            return GetTwist(gunHandleLocalPosition) / twistMaxAngle;
        }

        private float GetTwist(Vector3 gunHandleLocalPosition)
        {
            var oriented = Quaternion.AngleAxis(twistAngleOffset, Vector3.forward) *
                           (gunHandleLocalPosition - cockingPosition.localPosition);

            var signedAngle = Vector2.SignedAngle(Vector3.up, oriented.normalized);
            return Mathf.Clamp(signedAngle, twistMinAngle, twistMaxAngle);
        }

        #region StateCheckMethods

        private bool CanShoot(GunBase target)
        {
            return target.Trigger == TriggerState.Firing &&
                   target.State == GunState.Idle &&
                   (target.HasBulletInChamber || isDoubleAction);
        }

        #endregion


        #region GunBehaviourBase

        public override bool RequireCustomHandle => true;

        public override void OnTriggerDown(GunBase instance)
        {
            if (!CanShoot(instance))
            {
                instance.EmptyShoot();
                instance.Trigger = TriggerState.Armed;
            }
        }

        public override void OnGunPickup(GunBase instance)
        {
            Debug.Log("[CockingGunBehaviour] OnGunPickup");

            // お祈り
            if (requireTwist && idleTwistPosition != null)
                instance.CustomHandle.transform.localPosition = idleTwistPosition.localPosition;
            else
                instance.CustomHandle.transform.localPosition = cockingPosition.localPosition;
        }

        public override void OnGunDrop(GunBase instance)
        {
            Debug.Log("[CockingGunBehaviour] OnGunDrop");
            // お祈り
            if (requireTwist && idleTwistPosition != null)
                instance.CustomHandle.transform.localPosition = idleTwistPosition.localPosition;
            else
                instance.CustomHandle.transform.localPosition = cockingPosition.localPosition;
        }

        public override void OnGunUpdate(GunBase instance)
        {
            var currentState = instance.State;
            float progressNormalized, twistNormalized;

            // Shoot a gun whenever it's able to shoot. load new bullet if it's blow back variant
            if (CanShoot(instance))
            {
                var shotResult = instance.TryToShoot();
                var hasSucceeded = shotResult == ShotResult.Succeeded || shotResult == ShotResult.SucceededContinuously;
                if (hasSucceeded && isBlowBack)
                {
                    instance.HasCocked = true;
                    instance.LoadBullet();
                }
            }

            // Calculate cocking/twist progress
            if (Networking.LocalPlayer.IsUserInVR())
            {
                var customHandleLocalPos = instance.CustomHandle.transform.localPosition;
                progressNormalized = GetProgressNormalized(customHandleLocalPos);
                twistNormalized = GetTwistNormalized(customHandleLocalPos);
            }
            else
            {
                // Initiate desktop cocking on key press
                if (canDesktopCock && Input.GetKeyDown(desktopCockingKey) && !_isOnDesktopCocking)
                {
                    Debug.Log("[CockingGunBehaviour] Begin Desktop Reloading");
                    _isOnDesktopCocking = true;
                    _isDesktopCockingCurrentlyPulling = true;
                    _desktopCockingTimer = 0F;
                }

                // Do desktop cocking work
                if (_isOnDesktopCocking)
                {
                    var timeScale = requireTwist ? desktopCockingTime / 4 : desktopCockingTime / 2;
                    var progressTime = requireTwist ? _desktopCockingTimer - timeScale : _desktopCockingTimer;
                    var twistTime = _desktopCockingTimer;

                    if (_isDesktopCockingCurrentlyPulling)
                    {
                        _desktopCockingTimer += Time.deltaTime;

                        twistNormalized = Mathf.Clamp01(twistTime / timeScale);
                        progressNormalized = Mathf.Clamp01(progressTime / timeScale);

                        // If completed pulling back process
                        if (progressNormalized >= 1) _isDesktopCockingCurrentlyPulling = false;
                    }
                    else
                    {
                        _desktopCockingTimer -= Time.deltaTime;

                        progressNormalized = Mathf.Clamp01(progressTime / timeScale);
                        twistNormalized = Mathf.Clamp01(twistTime / timeScale);

                        // If completed reloading process
                        if (_desktopCockingTimer <= 0)
                        {
                            _isOnDesktopCocking = false;
                            _desktopCockingTimer = 0F;
                        }
                    }
                }
                else
                {
                    progressNormalized = 0;
                    twistNormalized = 0;
                }
            }

            // Clamp calculated progresses
            if (!requireTwist) twistNormalized = 1;

            if (twistNormalized < 1 - twistMargin &&
                (currentState == GunState.InCockingTwisting || currentState == GunState.Idle))
                progressNormalized = Mathf.Clamp(progressNormalized, 0, cockingMargin);
            else if (progressNormalized > cockingMargin)
                twistNormalized = Mathf.Clamp(twistNormalized, 1 - twistMargin, 1);

            if (!canCockAfterCock && instance.State == GunState.Idle && instance.HasBulletInChamber)
            {
                progressNormalized = Mathf.Clamp(progressNormalized, 0, cockingMargin);
                twistNormalized = Mathf.Clamp(twistNormalized, 1 - twistMargin, 1);
            }

            // Change states using GunUtility
            if (requireTwist)
                GunUtility.UpdateStateBoltAction(
                    instance,
                    cockingHapticData,
                    instance.CustomHandle.CurrentHand,
                    progressNormalized,
                    cockingAutoLoadMargin,
                    1 - cockingAutoLoadMargin,
                    twistNormalized,
                    twistMargin
                );
            else
                GunUtility.UpdateStateStraightPull(
                    instance,
                    cockingHapticData,
                    instance.CustomHandle.CurrentHand,
                    progressNormalized,
                    cockingAutoLoadMargin,
                    1 - cockingAutoLoadMargin
                );
        }

        public override void Setup(GunBase instance)
        {
            Debug.Log($"[CockingGunBehaviour] setup called for {instance.name}");
            var state = instance.State;
            if (state == GunState.Unknown)
            {
                Debug.LogWarning(
                    $"[CockingGunBehaviour] resetting state because it was invalid: {state.GetStateString()}");
                instance.State = GunState.Idle;
            }
        }

        public override void OnHandleDrop(GunBase instance, GunHandle handle)
        {
            if (handle.handleType != HandleType.CustomHandle)
            {
                Debug.Log(
                    $"[CockingGunBehaviour] OnHandleDrop: {instance.name} handle.handleId ({handle.handleType}) != {HandleType.CustomHandle}");
                return;
            }

            Vector3 expectedPos;
            var handlePos = instance.CustomHandle.transform.localPosition;
            var cockingPos = cockingPosition.localPosition;


            if (returnToCockingPositionOnDrop)
            {
                expectedPos = cockingPosition.localPosition;
                if (cockingHapticData && cockingHapticData.Done && instance)
                    cockingHapticData.Done.PlayInHand(instance.MainHandle.CurrentHand);
            }
            else if (requireTwist)
            {
                var twistNormalized = GetTwistNormalized(handlePos);
                var progressNormalized = GetProgressNormalized(handlePos);

                if (requireTwist && twistAutoLoadMargin > twistNormalized && cockingAutoLoadMargin > progressNormalized)
                {
                    expectedPos = idleTwistPosition.localPosition;
                }
                else
                {
                    var atp = activeTwistPosition.localPosition;
                    var expectedZ = Mathf.Clamp(handlePos.z, cockingPos.z - cockingLength, cockingPos.z);
                    expectedPos = new Vector3(atp.x, atp.y, expectedZ);
                }
            }
            else
            {
                if (cockingAutoLoadMargin > GetProgressNormalized(handlePos))
                {
                    expectedPos = cockingPosition.localPosition;
                }
                else
                {
                    var expectedZ = Mathf.Clamp(handlePos.z, cockingPos.z - cockingLength, cockingPos.z);
                    expectedPos = new Vector3(cockingPos.x, cockingPos.y, expectedZ);
                }
            }

            handle.MoveToLocalPosition(expectedPos, Quaternion.identity);
            Debug.Log(
                $"[CockingGunBehaviour] OnHandleDrop: {instance.name} moved handle to {expectedPos.ToString("F2")}");
        }

        #endregion
    }
}