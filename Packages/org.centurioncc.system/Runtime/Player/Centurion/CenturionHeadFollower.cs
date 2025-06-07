using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionHeadFollower : UdonSharpBehaviour
    {
        [SerializeField]
        private Vector3 followingOffset = new Vector3(0, 0.3F, 0);

        [SerializeField]
        private Vector3 localHeadOffset = new Vector3(0, 0, -1);

        private VRCPlayerApi _localPlayer;

        private VRCPlayerApi _targetPlayer;
        private Transform _transform;

        private void Start()
        {
            _transform = transform;
            _targetPlayer = Networking.GetOwner(gameObject);
            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            var localHead = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            _transform.position = _targetPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position +
                                  followingOffset;
            _transform.LookAt(localHead.position + localHead.rotation * localHeadOffset);
        }
    }
}