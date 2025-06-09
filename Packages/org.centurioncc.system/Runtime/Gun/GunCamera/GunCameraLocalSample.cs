using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunCameraLocalSample : UdonSharpBehaviour
    {
        [SerializeField]
        private GunCamera gunCameraInstance;

        [SerializeField]
        private GunCameraDataStore gunCameraDataStore;

        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        private int _callFrameCounts;

        private Vector3 _resetPos;
        private Quaternion _resetRot;

        private void Start()
        {
            var thisTransform = transform;
            _resetPos = thisTransform.position;
            _resetRot = thisTransform.rotation;

            gunCameraInstance.SetGunCamera(thisTransform, gunCameraDataStore);
        }

        public override void OnPickup()
        {
            if (gunCameraInstance.CustomTargetIndex == 0)
                gunCameraInstance.SetGunCamera(transform, gunCameraDataStore);
            _callFrameCounts = 0;
            updateManager.UnsubscribeSlowFixedUpdate(this);
        }

        public override void OnDrop()
        {
            updateManager.UnsubscribeSlowFixedUpdate(this);
            updateManager.SubscribeSlowFixedUpdate(this);
        }

        public void _SlowFixedUpdate()
        {
            ++_callFrameCounts;

            if (_callFrameCounts > 120)
            {
                _callFrameCounts = 0;
                updateManager.UnsubscribeSlowFixedUpdate(this);
                _ResetPosition();
            }
        }

        private void _ResetPosition()
        {
            transform.SetPositionAndRotation(_resetPos, _resetRot);
        }
    }
}