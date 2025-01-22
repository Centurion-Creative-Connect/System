using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerView : PlayerViewBase
    {
        [SerializeField] private PlayerCollider headCollider;
        [SerializeField] private PlayerCollider bodyCollider;
        [SerializeField] private PlayerCollider leftUpperArmCollider;
        [SerializeField] private PlayerCollider rightUpperArmCollider;
        [SerializeField] private PlayerCollider leftUpperLegCollider;
        [SerializeField] private PlayerCollider rightUpperLegCollider;
        [SerializeField] private PlayerCollider leftLowerLegCollider;
        [SerializeField] private PlayerCollider rightLowerLegCollider;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private Transform _bodyTransform;

        private PlayerCollider[] _colliders;
        private VRCPlayerApi _followingPlayer;

        private Transform _headTransform;
        private Transform _leftLowerLegTransform;
        private Transform _leftUpperArmTransform;
        private Transform _leftUpperLegTransform;
        private PlayerBase _playerModel;

        private Transform _rightLowerLegTransform;
        private Transform _rightUpperArmTransform;
        private Transform _rightUpperLegTransform;
        private bool _vrcPlayerInvalid = true;

        public override PlayerBase PlayerModel
        {
            get => _playerModel;
            set
            {
                _playerModel = value;
                var pcs = GetColliders();
                foreach (var pc in pcs) pc.player = value;

                UpdateTarget();
            }
        }

        private void Start()
        {
            Init();
        }

        public override void Init()
        {
            _headTransform = headCollider.transform;
            _bodyTransform = bodyCollider.transform;

            _leftUpperArmTransform = leftUpperArmCollider.transform;
            _rightUpperArmTransform = rightUpperArmCollider.transform;

            _leftUpperLegTransform = leftUpperLegCollider.transform;
            _rightUpperLegTransform = rightUpperLegCollider.transform;

            _leftLowerLegTransform = leftLowerLegCollider.transform;
            _rightLowerLegTransform = rightLowerLegCollider.transform;

            _colliders = new[]
            {
                headCollider, bodyCollider,
                leftUpperArmCollider, rightUpperArmCollider,
                leftUpperLegCollider, rightUpperLegCollider,
                leftLowerLegCollider, rightLowerLegCollider
            };
        }

        public override PlayerCollider[] GetColliders()
        {
            return _colliders;
        }

        public override void UpdateTarget()
        {
            _followingPlayer = _playerModel ? _playerModel.VrcPlayer : null;
            _vrcPlayerInvalid = !_playerModel || !Utilities.IsValid(_followingPlayer);

            foreach (var playerCollider in GetColliders())
                playerCollider.IsVisible = playerManager.IsDebug && _playerModel && _playerModel.IsAssigned;
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
            var neck = _followingPlayer.GetBonePosition(HumanBodyBones.Neck);
            var chest = _followingPlayer.GetBonePosition(HumanBodyBones.Chest);
            var hips = _followingPlayer.GetBonePosition(HumanBodyBones.Hips);
            var leftUpperArm = _followingPlayer.GetBonePosition(HumanBodyBones.LeftUpperArm);
            var leftLowerArm = _followingPlayer.GetBonePosition(HumanBodyBones.LeftLowerArm);
            var leftUpperLeg = _followingPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg);
            var leftLowerLeg = _followingPlayer.GetBonePosition(HumanBodyBones.LeftLowerLeg);
            var leftFoot = _followingPlayer.GetBonePosition(HumanBodyBones.LeftFoot);
            var rightUpperArm = _followingPlayer.GetBonePosition(HumanBodyBones.RightUpperArm);
            var rightLowerArm = _followingPlayer.GetBonePosition(HumanBodyBones.RightLowerArm);
            var rightUpperLeg = _followingPlayer.GetBonePosition(HumanBodyBones.RightUpperLeg);
            var rightLowerLeg = _followingPlayer.GetBonePosition(HumanBodyBones.RightLowerLeg);
            var rightFoot = _followingPlayer.GetBonePosition(HumanBodyBones.RightFoot);

            transform.SetPositionAndRotation(_followingPlayer.GetPosition(), _followingPlayer.GetRotation());

            if (playerManager.IsStaffTeamId(_playerModel.TeamId))
            {
                MoveCollidersToOrigin();
                return;
            }

            _headTransform.SetPositionAndRotation(head, GetRotation(head, neck));
            _bodyTransform.SetPositionAndRotation(chest, GetRotation(chest, hips));

            _leftUpperArmTransform.SetPositionAndRotation(leftUpperArm, GetRotation(leftUpperArm, leftLowerArm));
            _leftUpperLegTransform.SetPositionAndRotation(leftUpperLeg, GetRotation(leftUpperLeg, leftLowerLeg));
            _leftLowerLegTransform.SetPositionAndRotation(leftLowerLeg, GetRotation(leftLowerLeg, leftFoot));

            _rightUpperArmTransform.SetPositionAndRotation(rightUpperArm, GetRotation(rightUpperArm, rightLowerArm));
            _rightUpperLegTransform.SetPositionAndRotation(rightUpperLeg, GetRotation(rightUpperLeg, rightLowerLeg));
            _rightLowerLegTransform.SetPositionAndRotation(rightLowerLeg, GetRotation(rightLowerLeg, rightFoot));
        }

        private static Quaternion GetRotation(Vector3 vec1, Vector3 vec2)
        {
            if (vec1.sqrMagnitude == 0F && vec2.sqrMagnitude == 0F)
                return Quaternion.identity;
            return Quaternion.LookRotation(vec1 - vec2);
        }

        private void MoveViewToOrigin()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void MoveCollidersToOrigin()
        {
            _headTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _bodyTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _leftUpperArmTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _leftUpperLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _leftLowerLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _rightUpperArmTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _rightUpperLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _rightLowerLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}