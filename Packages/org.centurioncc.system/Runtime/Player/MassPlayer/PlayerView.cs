using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerView : UdonSharpBehaviour
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

        public PlayerModel playerModel;

        private Transform bodyTransform;

        private Transform headTransform;
        private Transform leftLowerLegTransform;
        private Transform leftUpperArmTransform;
        private Transform leftUpperLegTransform;
        private Transform rightLowerLegTransform;
        private Transform rightUpperArmTransform;
        private Transform rightUpperLegTransform;

        private void Start()
        {
            headTransform = headCollider.transform;
            bodyTransform = bodyCollider.transform;

            leftUpperArmTransform = leftUpperArmCollider.transform;
            rightUpperArmTransform = rightUpperArmCollider.transform;

            leftUpperLegTransform = leftUpperLegCollider.transform;
            rightUpperLegTransform = rightUpperLegCollider.transform;

            leftLowerLegTransform = leftLowerLegCollider.transform;
            rightLowerLegTransform = rightLowerLegCollider.transform;
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

        public void UpdateView()
        {
            foreach (var playerCollider in GetIterator())
                playerCollider.IsVisible = playerManager.IsDebug && playerModel != null && playerModel.IsAssigned;
        }

        public void UpdateCollider()
        {
            if (playerModel == null)
            {
                MoveViewToOrigin();
                MoveCollidersToOrigin();
                return;
            }

            var vrcPlayer = playerModel.cachedVrcPlayerApi;
            if (!Utilities.IsValid(vrcPlayer))
            {
                MoveViewToOrigin();
                MoveCollidersToOrigin();
                return;
            }

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

            if (playerManager.IsStaffTeamId(playerModel.TeamId))
            {
                MoveCollidersToOrigin();
                return;
            }

            headTransform.SetPositionAndRotation(head, GetRotation(head, neck));
            bodyTransform.SetPositionAndRotation(chest, GetRotation(chest, hips));

            leftUpperArmTransform.SetPositionAndRotation(leftUpperArm, GetRotation(leftUpperArm, leftLowerArm));
            leftUpperLegTransform.SetPositionAndRotation(leftUpperLeg, GetRotation(leftUpperLeg, leftLowerLeg));
            leftLowerLegTransform.SetPositionAndRotation(leftLowerLeg, GetRotation(leftLowerLeg, leftFoot));

            rightUpperArmTransform.SetPositionAndRotation(rightUpperArm, GetRotation(rightUpperArm, rightLowerArm));
            rightUpperLegTransform.SetPositionAndRotation(rightUpperLeg, GetRotation(rightUpperLeg, rightLowerLeg));
            rightLowerLegTransform.SetPositionAndRotation(rightLowerLeg, GetRotation(rightLowerLeg, rightFoot));
        }

        public void SetPlayerModel(PlayerModel value)
        {
            if (playerModel == value)
                return;

            playerModel = value;

            var pcs = GetIterator();
            foreach (var pc in pcs) pc.player = value;

            UpdateView();
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
            headTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            bodyTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            leftUpperArmTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            leftUpperLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            leftLowerLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            rightUpperArmTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            rightUpperLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            rightLowerLegTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}