using CenturionCC.System.Gun.DataStore;
using UnityEngine;

namespace CenturionCC.System.Gun.Behaviour
{
    public class PumpingGunBehaviour : GunBehaviourBase
    {
        [SerializeField] private float cockingLength;

        [SerializeField] [Range(0, 1)] private float maxAutoLoadMargin = 0.8F;

        [SerializeField] [Range(0, 1)] private float minAutoLoadMargin = 0.2F;

        [SerializeField] private float minimumZOffset = 0.1F;

        [SerializeField] private GunCockingHapticDataStore cockingHapticData;

        [Header("Desktop")] [SerializeField] private bool doDesktopCockingAutomatically = true;

        [SerializeField] private KeyCode desktopCockingKey = KeyCode.F;

        [SerializeField] private float desktopCockingTime;

        private float _cockingRefZ;

        private float _desktopCockingTimer;
        private bool _isOnDesktopCocking;
        private float _mainHandleRefZ;

        private float _subHandleRefZ;

        private static bool CanSlide(GunBase target)
        {
            return target.State != GunState.Idle || !target.HasBulletInChamber;
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
            var diff = _cockingRefZ - localZ;
            if (cockingLength < diff)
                _cockingRefZ -= diff - cockingLength;
            return Mathf.Clamp(diff, 0, cockingLength);
        }

        #region BehaviourBase
        public override void OnTriggerDown(GunBase instance)
        {
            if (instance.State != GunState.Idle || instance.HasBulletInChamber == false)
            {
                instance.Trigger = TriggerState.Fired;
                instance._EmptyShoot();
            }
        }

        public override void OnGunUpdate(GunBase instance)
        {
            float progressNormalized;

            // Shoot a gun whenever it's able to shoot
            if (CanShoot(instance))
            {
                var shotResult = instance._TryToShoot();

                switch (shotResult)
                {
                    // IF paused, don't process further to freeze handle movement.
                    case ShotResult.Paused:
                        return;
                    // IF succeeded continuously (most likely not happen), loads bullet then return to try shooting again.
                    case ShotResult.SucceededContinuously:
                        instance._LoadBullet();
                        return;
                    // IF succeeded, sets state to Idle then change cocking reference Z(which will be used to determine cocking progress) will be updated.
                    case ShotResult.Succeeded:
                        instance.State = GunState.Idle;
                        var localSubHandlePos =
                            instance.transform.worldToLocalMatrix.MultiplyPoint3x4(
                                instance.SubHandle.transform.position);
                        _cockingRefZ = Mathf.Max(_mainHandleRefZ + cockingLength + minimumZOffset, localSubHandlePos.z);
                        break;
                    case ShotResult.Cancelled:
                    case ShotResult.Failed:
                    default:
                        break;
                }
            }

            // Calculate cocking progress
            if (instance.IsVR)
            {
                var worldToLocalMatrix = instance.transform.worldToLocalMatrix;
                var subHandleLocalPos = worldToLocalMatrix.MultiplyPoint3x4(instance.SubHandle.transform.position);
                progressNormalized = GetProgressNormalized(subHandleLocalPos.z);
            }
            else
            {
                // Initiate desktop cocking on key press
                var cockingInput = Input.GetKeyDown(desktopCockingKey) || doDesktopCockingAutomatically;
                var shouldCock = instance.State == GunState.Idle && !instance.HasCocked && instance._HasNextBullet();
                if (!_isOnDesktopCocking && CanSlide(instance) && shouldCock && cockingInput)
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
            if (instance.VariantData)
            {
                _mainHandleRefZ = instance.VariantData.MainHandlePositionOffset.z;
                _subHandleRefZ = instance.VariantData.SubHandlePositionOffset.z;
            }

            _cockingRefZ = _subHandleRefZ;
        }

        public override void Dispose(GunBase instance)
        {
            _cockingRefZ = 0F;
        }

        public override void OnGunPickup(GunBase instance)
        {
            // Reset cocking reference position
            if (instance.VariantData)
            {
                _subHandleRefZ = instance.VariantData.SubHandlePositionOffset.z;
            }

            _cockingRefZ = _subHandleRefZ;
        }
        #endregion
    }
}
