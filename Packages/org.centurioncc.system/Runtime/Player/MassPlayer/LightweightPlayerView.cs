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

        private PlayerBase _playerModel;

        private Transform _transform;

        public override PlayerBase PlayerModel
        {
            get => _playerModel;
            set
            {
                if (_playerModel == value)
                    return;

                _playerModel = value;
                lwCollider.player = value;

                UpdateView();
            }
        }

        private void Start()
        {
            Init();
        }

        public override void Init()
        {
            _transform = transform;
        }

        public override PlayerCollider[] GetColliders()
        {
            return new[] { lwCollider };
        }

        public override void UpdateView()
        {
            lwCollider.IsVisible = playerManager.IsDebug && _playerModel != null && _playerModel.IsAssigned;
        }

        public override void UpdateCollider()
        {
            if (_playerModel == null)
            {
                MoveViewToOrigin();
                MoveCollidersToOrigin();
                return;
            }

            var vrcPlayer = _playerModel.VrcPlayer;
            if (!Utilities.IsValid(vrcPlayer))
            {
                MoveViewToOrigin();
                MoveCollidersToOrigin();
                return;
            }

            // Utilities.IsValid will null-check vrcPlayer
            // ReSharper disable once PossibleNullReferenceException
            var head = vrcPlayer.GetBonePosition(HumanBodyBones.Head);
            var foot = (vrcPlayer.GetBonePosition(HumanBodyBones.LeftFoot) +
                        vrcPlayer.GetBonePosition(HumanBodyBones.RightFoot)) / 2F;

            _transform.SetPositionAndRotation(vrcPlayer.GetPosition(), vrcPlayer.GetRotation());

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