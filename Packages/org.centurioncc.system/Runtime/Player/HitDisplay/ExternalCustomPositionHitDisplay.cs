using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.HitDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalCustomPositionHitDisplay : ExternalHitDisplayBase
    {
        [SerializeField]
        private float duration = 0.5F;

        [Header("Position Settings")]
        [SerializeField]
        private HumanBodyBones referenceBone = HumanBodyBones.Chest;

        [SerializeField]
        private Vector3 offsetForReference = Vector3.up * 0.05F;

        [SerializeField]
        private Vector3 offsetForView = Vector3.forward * .4F;

        private VRCPlayerApi _followingPlayer;
        private VRCPlayerApi _localPlayer;

        private Vector3 _offset;
        private Transform _transform;

        private void Start()
        {
            _transform = transform;
            _localPlayer = Networking.LocalPlayer;
            if (_followingPlayer == null)
                _followingPlayer = _localPlayer;
        }

        private void Update()
        {
            if (!Utilities.IsValid(_followingPlayer) || !Utilities.IsValid(_localPlayer))
            {
                Destroy(gameObject);
                return;
            }

            var localHead = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var followingPos = _followingPlayer.GetPosition();
            var destPos = followingPos + _offset;
            _transform.position = destPos + Quaternion.LookRotation(localHead.position - destPos) * offsetForView;
            _transform.LookAt(localHead.position);
        }

        public override void Play(PlayerBase player)
        {
            var followingPlayer = player.VrcPlayer;
            if (followingPlayer == null || !Utilities.IsValid(followingPlayer))
            {
                DestroyThis();
                return;
            }

            _followingPlayer = followingPlayer;

            var playerPos = followingPlayer.GetPosition();
            var bonePos = followingPlayer.GetBonePosition(referenceBone) + offsetForReference;
            if (bonePos == Vector3.zero)
                bonePos = playerPos + Vector3.up * 1.7F;
            _offset = bonePos - playerPos;

            Start();
            Update();
            gameObject.SetActive(true);

            SendCustomEventDelayedSeconds(nameof(DestroyThis), duration);
        }

        public void DestroyThis()
        {
            Destroy(gameObject);
        }
    }
}