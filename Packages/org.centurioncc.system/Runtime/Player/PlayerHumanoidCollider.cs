using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerHumanoidCollider : UdonSharpBehaviour
    {
        [SerializeField]
        private Transform lightweightColliderAnchor;
        [Header("Player Colliders")]
        [SerializeField]
        private PlayerCollider lightweightPlayerCollider;
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

        private readonly Vector3 _colliderResetPosition = new Vector3(0, -10, -20);

        private bool _isUsingLightweightCollider;

        private PlayerBase _player;
        private PlayerManager _playerManager;
        public bool IsUsingLightweightCollider
        {
            get => (_playerManager.UseLightweightCollider && _isUsingLightweightCollider) ||
                   _playerManager.AlwaysUseLightweightCollider;
            private set
            {
                if (_isUsingLightweightCollider != value)
                {
                    _isUsingLightweightCollider = value;
                    UpdateView();
                }
            }
        }

        public bool HasInitialized { get; private set; }

        private void Start()
        {
            // get and cache transforms

            _headTransform = headCollider.transform;
            _bodyTransform = bodyCollider.transform;

            _leftUpperArmTransform = leftUpperArmCollider.transform;
            _rightUpperArmTransform = rightUpperArmCollider.transform;

            _leftUpperLegTransform = leftUpperLegCollider.transform;
            _rightUpperLegTransform = rightUpperLegCollider.transform;

            _leftLowerLegTransform = leftLowerLegCollider.transform;
            _rightLowerLegTransform = rightLowerLegCollider.transform;
        }

        public void Init(PlayerBase player, PlayerManager playerManager)
        {
            _player = player;
            _playerManager = playerManager;

            foreach (var playerCollider in GetColliderIterator())
                if (playerCollider != null)
                    playerCollider.player = player;
            lightweightPlayerCollider.player = player;
            HasInitialized = true;
        }

        public void UpdateCollider(VRCPlayerApi api)
        {
            if (!IsUsingLightweightCollider)
                UpdateHeavyCollider(api);
        }

        public void SlowUpdateCollider(VRCPlayerApi api)
        {
            CheckLightweightState(api);
            if (IsUsingLightweightCollider)
                UpdateLightweightCollider(api);
        }

        private void UpdateLightweightCollider(VRCPlayerApi api)
        {
            var height = (api.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - transform.position).y;
            lightweightColliderAnchor.localScale = new Vector3(1, height + 0.25F, 1);
            lightweightColliderAnchor.localPosition = Vector3.zero;
        }

        private void UpdateHeavyCollider(VRCPlayerApi api)
        {
            var head = api.GetBonePosition(HumanBodyBones.Head);
            var neck = api.GetBonePosition(HumanBodyBones.Neck);
            var chest = api.GetBonePosition(HumanBodyBones.Chest);
            var hips = api.GetBonePosition(HumanBodyBones.Hips);
            var leftUpperArm = api.GetBonePosition(HumanBodyBones.LeftUpperArm);
            var leftLowerArm = api.GetBonePosition(HumanBodyBones.LeftLowerArm);
            var leftUpperLeg = api.GetBonePosition(HumanBodyBones.LeftUpperLeg);
            var leftLowerLeg = api.GetBonePosition(HumanBodyBones.LeftLowerLeg);
            var leftFoot = api.GetBonePosition(HumanBodyBones.LeftFoot);
            var rightUpperArm = api.GetBonePosition(HumanBodyBones.RightUpperArm);
            var rightLowerArm = api.GetBonePosition(HumanBodyBones.RightLowerArm);
            var rightUpperLeg = api.GetBonePosition(HumanBodyBones.RightUpperLeg);
            var rightLowerLeg = api.GetBonePosition(HumanBodyBones.RightLowerLeg);
            var rightFoot = api.GetBonePosition(HumanBodyBones.RightFoot);

            if (_playerManager.UseBaseCollider)
            {
                _headTransform.SetPositionAndRotation(head, _GetRotation(head, neck));
                _bodyTransform.SetPositionAndRotation(chest, _GetRotation(chest, hips));
                _leftLowerLegTransform.SetPositionAndRotation(leftLowerLeg, _GetRotation(leftLowerLeg, leftFoot));
                _rightLowerLegTransform.SetPositionAndRotation(rightLowerLeg, _GetRotation(rightLowerLeg, rightFoot));
            }

            if (_playerManager.UseAdditionalCollider)
            {
                _leftUpperArmTransform.SetPositionAndRotation(leftUpperArm, _GetRotation(leftUpperArm, leftLowerArm));
                _rightUpperArmTransform.SetPositionAndRotation(rightUpperArm,
                    _GetRotation(rightUpperArm, rightLowerArm));

                _leftUpperLegTransform.SetPositionAndRotation(leftUpperLeg, _GetRotation(leftUpperLeg, leftLowerLeg));
                _rightUpperLegTransform.SetPositionAndRotation(rightUpperLeg,
                    _GetRotation(rightUpperLeg, rightLowerLeg));
            }
        }

        private void CheckLightweightState(VRCPlayerApi api)
        {
            if (_playerManager.UseLightweightCollider && !_playerManager.AlwaysUseLightweightCollider)
            {
                if (_player.IsLocal)
                {
                    IsUsingLightweightCollider = false;
                    return;
                }

                var localHeadTrackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                var remoteHeadPos = api.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                var localHeadPos = localHeadTrackingData.position;
                var distanceFromLocal = Vector3.Distance(localHeadPos, remoteHeadPos);
                var isLooking = Vector3.Dot(
                    (localHeadPos - remoteHeadPos).normalized,
                    (localHeadTrackingData.rotation * Vector3.forward).normalized);
                IsUsingLightweightCollider = !(isLooking < -0.5 && distanceFromLocal < 45);
            }
        }

        public void UpdateView()
        {
            if (!HasInitialized)
                return;

            if (_playerManager.IsStaffTeamId(_player.TeamId))
            {
                foreach (var playerCollider in GetColliderIterator())
                {
                    playerCollider.transform.position = _colliderResetPosition;
                    playerCollider.IsVisible = false;
                    playerCollider.ActualCollider.enabled = false;
                }

                lightweightPlayerCollider.IsVisible = false;
                lightweightPlayerCollider.ActualCollider.enabled = false;
                lightweightColliderAnchor.position = _colliderResetPosition;

                return;
            }

            var isUsingLwc = IsUsingLightweightCollider;
            var isDebug = _playerManager.IsDebug;
            var isAssigned = _player.IsAssigned;

            foreach (var playerCollider in GetColliderIterator())
            {
                playerCollider.transform.position = _colliderResetPosition;
                playerCollider.IsVisible = isDebug && !isUsingLwc;
                playerCollider.ActualCollider.enabled = isAssigned && !isUsingLwc;
            }

            lightweightPlayerCollider.IsVisible = isDebug && isUsingLwc;
            lightweightPlayerCollider.ActualCollider.enabled = isAssigned && isUsingLwc;
            lightweightColliderAnchor.position = _colliderResetPosition;
        }

        private PlayerCollider[] GetColliderIterator()
        {
            return new[]
            {
                headCollider, bodyCollider,
                leftUpperArmCollider, rightUpperArmCollider,
                leftUpperLegCollider, rightUpperLegCollider,
                leftLowerLegCollider, rightLowerLegCollider
            };
        }

        private static Quaternion _GetRotation(Vector3 vec1, Vector3 vec2)
        {
            if (vec1.sqrMagnitude == 0F && vec2.sqrMagnitude == 0F)
                return Quaternion.identity;
            return Quaternion.LookRotation(vec1 - vec2);
        }

        #region ColliderTransforms

        private Transform _headTransform;
        private Transform _bodyTransform;
        private Transform _leftUpperArmTransform;
        private Transform _rightUpperArmTransform;
        private Transform _leftUpperLegTransform;
        private Transform _rightUpperLegTransform;
        private Transform _leftLowerLegTransform;
        private Transform _rightLowerLegTransform;

        #endregion
    }
}