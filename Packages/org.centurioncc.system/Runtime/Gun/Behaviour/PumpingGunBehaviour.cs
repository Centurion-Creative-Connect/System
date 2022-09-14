using CenturionCC.System.Gun.DataStore;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.Behaviour
{
    public class PumpingGunBehaviour : GunBehaviourBase
    {
        [SerializeField]
        private float cockingLength;
        [SerializeField] [Range(0, 1)]
        private float maxAutoLoadMargin = 0.8F;
        [SerializeField] [Range(0, 1)]
        private float minAutoLoadMargin = 0.2F;
        [SerializeField]
        private GunCockingHapticDataStore cockingHapticData;
        [Header("Desktop")]
        [SerializeField]
        private float desktopCockingTime;
        private float _cockingRefZ;

        private float _desktopCockingTimer;
        private bool _isOnDesktopCocking;

        private float _subHandleRefZ;

        private static bool CanSlide(GunBase target)
        {
            return target.HasBulletInChamber == false;
        }

        private static bool CanShoot(GunBase target)
        {
            return target.Trigger == TriggerState.Firing && target.State == GunState.Idle && target.HasBulletInChamber;
        }

        private float GetProgressNormalized(float localZ)
        {
            return GetProgress(localZ) / cockingLength;
        }

        private float GetProgress(float localZ)
        {
            return Mathf.Clamp(_cockingRefZ - localZ, 0, cockingLength);
        }

        #region BehaviourBase

        public override bool RequireCustomHandle => false;


        public override void OnTriggerDown(GunBase instance)
        {
            if (instance.State != GunState.Idle || instance.HasBulletInChamber == false)
            {
                instance.Trigger = TriggerState.Fired;
                instance.EmptyShoot();
            }
        }

        public override void OnGunUpdate(GunBase instance)
        {
            var currentState = instance.State;
            float progressNormalized;

            // Shoot a gun whenever it's able to shoot
            if (CanShoot(instance))
            {
                var shotResult = instance.TryToShoot();
                if (shotResult == ShotResult.Succeeded)
                {
                    instance.State = GunState.Idle;
                    var localSubHandlePos =
                        instance.Target.worldToLocalMatrix.MultiplyPoint3x4(instance.SubHandle.transform.position);
                    // limit ref pos going further away
                    const float allowedMoveRange = 0.02F;
                    _cockingRefZ =
                        Mathf.Clamp
                        (
                            localSubHandlePos.z,
                            _subHandleRefZ - allowedMoveRange,
                            _subHandleRefZ + allowedMoveRange
                        );
                }
            }

            // Calculate cocking progress
            if (Networking.LocalPlayer.IsUserInVR())
            {
                progressNormalized =
                    GetProgressNormalized(
                        instance.Target.worldToLocalMatrix.MultiplyPoint3x4(instance.SubHandle.transform.position).z);
            }
            else
            {
                // Initiate desktop cocking on key press
                if (!_isOnDesktopCocking && (currentState == GunState.Idle || Input.GetKeyDown(KeyCode.F)))
                {
                    _isOnDesktopCocking = true;
                    _desktopCockingTimer = 0F;
                }

                // Do desktop cocking work
                if (_isOnDesktopCocking)
                {
                    var timeScale = desktopCockingTime / 2;
                    _desktopCockingTimer += Time.deltaTime;

                    progressNormalized = Mathf.Clamp01(
                        Mathf.PingPong(_desktopCockingTimer, timeScale) / timeScale);

                    if (_desktopCockingTimer >= desktopCockingTime)
                    {
                        _isOnDesktopCocking = false;
                        _desktopCockingTimer = 0F;
                    }
                }
                else
                {
                    progressNormalized = 0;
                }
            }

            // Clamp calculated progresses
            if (!CanSlide(instance)) progressNormalized = 0;

            // Change states using GunUtility
            GunUtility.UpdateStateStraightPull
            (
                instance,
                cockingHapticData,
                instance.SubHandle.CurrentHand,
                progressNormalized,
                minAutoLoadMargin,
                maxAutoLoadMargin
            );
        }

        public override void Setup(GunBase instance)
        {
            // Reset cocking reference position
            _subHandleRefZ = instance.SubHandlePositionOffset.z;
            _cockingRefZ = _subHandleRefZ;
        }

        public override void Dispose(GunBase instance)
        {
            _cockingRefZ = 0F;
        }

        public override void OnGunPickup(GunBase instance)
        {
            // Reset cocking reference position
            _subHandleRefZ = instance.SubHandlePositionOffset.z;
            _cockingRefZ = _subHandleRefZ;
        }

        #endregion
    }
}