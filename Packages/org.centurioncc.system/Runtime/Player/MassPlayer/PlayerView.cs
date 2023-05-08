using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerView : PlayerViewBase
    {
        [SerializeField]
        private PlayerCollider headCollider;
        [SerializeField]
        private PlayerCollider bodyCollider;
        [SerializeField]
        private PlayerCollider leftUpperArmCollider;
        [SerializeField]
        private PlayerCollider rightUpperArmCollider;
        [SerializeField]
        private PlayerCollider leftUpperLegCollider;
        [SerializeField]
        private PlayerCollider rightUpperLegCollider;
        [SerializeField]
        private PlayerCollider leftLowerLegCollider;
        [SerializeField]
        private PlayerCollider rightLowerLegCollider;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private Transform _bodyTransform;

        private Transform _headTransform;
        private Transform _leftLowerLegTransform;
        private Transform _leftUpperArmTransform;
        private Transform _leftUpperLegTransform;

        private PlayerBase _playerModel;
        private Transform _rightLowerLegTransform;
        private Transform _rightUpperArmTransform;
        private Transform _rightUpperLegTransform;

        public override PlayerBase PlayerModel
        {
            get => _playerModel;
            set
            {
                if (_playerModel == value)
                    return;

                _playerModel = value;

                var pcs = GetIterator();
                foreach (var pc in pcs) pc.player = value;

                UpdateView();
            }
        }

        private void Start()
        {
            _headTransform = headCollider.transform;
            _bodyTransform = bodyCollider.transform;

            _leftUpperArmTransform = leftUpperArmCollider.transform;
            _rightUpperArmTransform = rightUpperArmCollider.transform;

            _leftUpperLegTransform = leftUpperLegCollider.transform;
            _rightUpperLegTransform = rightUpperLegCollider.transform;

            _leftLowerLegTransform = leftLowerLegCollider.transform;
            _rightLowerLegTransform = rightLowerLegCollider.transform;
        }

        private PlayerCollider[] GetIterator()
        {
            return new[]
            {
                headCollider, bodyCollider,
                leftUpperArmCollider, rightUpperArmCollider,
                leftUpperLegCollider, rightUpperLegCollider,
                leftLowerLegCollider, rightLowerLegCollider
            };
        }

        public override void UpdateView()
        {
            foreach (var playerCollider in GetIterator())
                playerCollider.IsVisible = playerManager.IsDebug && _playerModel != null && _playerModel.IsAssigned;
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
            var neck = vrcPlayer.GetBonePosition(HumanBodyBones.Neck);
            var chest = vrcPlayer.GetBonePosition(HumanBodyBones.Chest);
            var hips = vrcPlayer.GetBonePosition(HumanBodyBones.Hips);
            var leftUpperArm = vrcPlayer.GetBonePosition(HumanBodyBones.LeftUpperArm);
            var leftLowerArm = vrcPlayer.GetBonePosition(HumanBodyBones.LeftLowerArm);
            var leftUpperLeg = vrcPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg);
            var leftLowerLeg = vrcPlayer.GetBonePosition(HumanBodyBones.LeftLowerLeg);
            var leftFoot = vrcPlayer.GetBonePosition(HumanBodyBones.LeftFoot);
            var rightUpperArm = vrcPlayer.GetBonePosition(HumanBodyBones.RightUpperArm);
            var rightLowerArm = vrcPlayer.GetBonePosition(HumanBodyBones.RightLowerArm);
            var rightUpperLeg = vrcPlayer.GetBonePosition(HumanBodyBones.RightUpperLeg);
            var rightLowerLeg = vrcPlayer.GetBonePosition(HumanBodyBones.RightLowerLeg);
            var rightFoot = vrcPlayer.GetBonePosition(HumanBodyBones.RightFoot);

            transform.SetPositionAndRotation(vrcPlayer.GetPosition(), vrcPlayer.GetRotation());

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