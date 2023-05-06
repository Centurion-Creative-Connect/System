using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.UI.HeadUI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HeadUIMover : UdonSharpBehaviour
    {
        [SerializeField]
        private float speed = 10;
        private VRCPlayerApi _localPlayer;
        private Transform _target;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            _target = transform;
        }

        public override void PostLateUpdate()
        {
            var trackingData = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            _target.SetPositionAndRotation(
                trackingData.position,
                Quaternion.Lerp(_target.transform.rotation, trackingData.rotation, Time.deltaTime * speed)
            );
        }
    }
}