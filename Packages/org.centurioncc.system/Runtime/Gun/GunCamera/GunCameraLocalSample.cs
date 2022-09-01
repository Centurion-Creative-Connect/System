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

        private int _callFrameCounts;

        private Vector3 _resetPos;
        private Quaternion _resetRot;

        private UpdateManager _updateManager;

        private void Start()
        {
            var thisTransform = transform;
            _resetPos = thisTransform.position;
            _resetRot = thisTransform.rotation;

            _updateManager = GameManagerHelper.GetUpdateManager();

            gunCameraInstance.SetGunCamera(transform, gunCameraDataStore);
        }

        public override void OnPickup()
        {
            gunCameraInstance.SetGunCamera(transform, gunCameraDataStore);
            _callFrameCounts = 0;
            _updateManager.UnsubscribeSlowFixedUpdate(this);
        }

        public override void OnDrop()
        {
            _updateManager.UnsubscribeSlowFixedUpdate(this);
            _updateManager.SubscribeSlowFixedUpdate(this);
        }

        public void _SlowFixedUpdate()
        {
            ++_callFrameCounts;

            if (_callFrameCounts > 120)
            {
                _callFrameCounts = 0;
                _updateManager.UnsubscribeSlowFixedUpdate(this);
                _ResetPosition();
            }
        }

        private void _ResetPosition()
        {
            transform.SetPositionAndRotation(_resetPos, _resetRot);
        }
    }
}