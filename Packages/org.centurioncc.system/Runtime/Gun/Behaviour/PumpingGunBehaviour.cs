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

        private bool CanSlide(GunState state)
        {
            return state == GunState.Idle
                   || state == GunState.IdleWithCocked
                   || state == GunState.Pulling
                   || state == GunState.PullingWithBullet
                   || state == GunState.Pushing
                   || state == GunState.PushingWithBullet;
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
            if (!instance.State.IsReadyToShoot())
            {
                instance.Trigger = TriggerState.Armed;
                instance.EmptyShoot();
            }
        }

        public override void OnGunUpdate(GunBase instance)
        {
            var currentState = instance.State;
            float progressNormalized;

            if (instance.Trigger == TriggerState.Firing &&
                currentState.IsReadyToShoot() &&
                instance.TryToShoot() == ShotResult.Succeeded)
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

            // calculate progress
            if (Networking.LocalPlayer.IsUserInVR())
            {
                progressNormalized =
                    GetProgressNormalized(
                        instance.Target.worldToLocalMatrix.MultiplyPoint3x4(instance.SubHandle.transform.position).z);
            }
            else
            {
                // initiate desktop cocking
                if (!_isOnDesktopCocking && (currentState == GunState.Idle || Input.GetKeyDown(KeyCode.F)))
                {
                    _isOnDesktopCocking = true;
                    _desktopCockingTimer = 0F;
                }

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

            // clamp progress
            if (!CanSlide(currentState)) progressNormalized = 0;

            // do state changing work
            GunHelper.UpdateStateStraightPull
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