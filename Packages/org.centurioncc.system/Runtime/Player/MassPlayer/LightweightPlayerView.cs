using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-10000)]
    public class LightweightPlayerView : PlayerViewBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] private PlayerCollider lwCollider;

        [SerializeField] private Transform groundPivot;
        private PlayerCollider[] _colliders;
        private VRCPlayerApi _followingPlayer;

        private PlayerBase _playerModel;

        private Transform _transform;
        private bool _vrcPlayerInvalid = true;

        public override PlayerBase PlayerModel
        {
            get => _playerModel;
            set
            {
                if (_playerModel == value) return;

                _playerModel = value;
                lwCollider.player = value;

                UpdateTarget();
            }
        }

        private void Start()
        {
            Init();
        }

        public override void Init()
        {
            _transform = transform;
            _colliders = new[] { lwCollider };
        }

        public override PlayerCollider[] GetColliders()
        {
            return _colliders;
        }

        public override void UpdateTarget()
        {
            _followingPlayer = _playerModel ? _playerModel.VrcPlayer : null;
            _vrcPlayerInvalid = !_playerModel || Utilities.IsValid(_followingPlayer);
            lwCollider.IsVisible = playerManager.IsDebug && _playerModel != null && _playerModel.IsAssigned;
        }

        public override void UpdateCollider()
        {
            if (_vrcPlayerInvalid)
            {
                MoveViewToOrigin();
                MoveCollidersToOrigin();
                return;
            }

            // Utilities.IsValid will null-check vrcPlayer
            // ReSharper disable once PossibleNullReferenceException
            var head = _followingPlayer.GetBonePosition(HumanBodyBones.Head);
            var foot = (_followingPlayer.GetBonePosition(HumanBodyBones.LeftFoot) +
                        _followingPlayer.GetBonePosition(HumanBodyBones.RightFoot)) / 2F;

            _transform.SetPositionAndRotation(_followingPlayer.GetPosition(), _followingPlayer.GetRotation());

            if (playerManager.IsStaffTeamId(_playerModel.TeamId))
            {
                MoveCollidersToOrigin();
                return;
            }

            groundPivot.SetPositionAndRotation(foot, GetRotation(head, foot));
            var height = (head - foot).magnitude;
            groundPivot.localScale = new Vector3(1, 1, height + 0.25F);
        }

        private void MoveViewToOrigin()
        {
            _transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void MoveCollidersToOrigin()
        {
            groundPivot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private static Quaternion GetRotation(Vector3 vec1, Vector3 vec2)
        {
            if (vec1.sqrMagnitude == 0F && vec2.sqrMagnitude == 0F)
                return Quaternion.identity;
            return Quaternion.LookRotation(vec1 - vec2);
        }
    }
}