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

        public void UpdateLightweightCollider(VRCPlayerApi api)
        {
            var height = (api.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - transform.position).y;
            lightweightColliderAnchor.localScale = new Vector3(1, height + 0.25F, 1);
            lightweightColliderAnchor.localPosition = Vector3.zero;
        }

        public void UpdateHeavyCollider(VRCPlayerApi api)
        {
            if (_playerManager.UseBaseCollider)
            {
                _headTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.Head),
                    _GetBoneRotation(api, HumanBodyBones.Head, HumanBodyBones.Neck));

                _bodyTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.Chest),
                    _GetBoneRotation(api, HumanBodyBones.Chest, HumanBodyBones.Hips));

                _leftLowerLegTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.LeftLowerLeg),
                    _GetBoneRotation(api, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot));
                _rightLowerLegTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.RightLowerLeg),
                    _GetBoneRotation(api, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot));
            }

            if (_playerManager.UseAdditionalCollider)
            {
                _leftUpperArmTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.LeftUpperArm),
                    _GetBoneRotation(api, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm));
                _rightUpperArmTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.RightUpperArm),
                    _GetBoneRotation(api, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm));

                _leftUpperLegTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.LeftUpperLeg),
                    _GetBoneRotation(api, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg));
                _rightUpperLegTransform.SetPositionAndRotation(
                    api.GetBonePosition(HumanBodyBones.RightUpperLeg),
                    _GetBoneRotation(api, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg));
            }
        }

        public void CheckLightweightState(VRCPlayerApi api)
        {
            if (_playerManager.UseLightweightCollider && !_playerManager.AlwaysUseLightweightCollider)
            {
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

        public PlayerCollider[] GetColliderIterator()
        {
            return new[]
            {
                headCollider, bodyCollider,
                leftUpperArmCollider, rightUpperArmCollider,
                leftUpperLegCollider, rightUpperLegCollider,
                leftLowerLegCollider, rightLowerLegCollider
            };
        }

        private static Quaternion _GetBoneRotation(VRCPlayerApi api, HumanBodyBones pivotBone, HumanBodyBones nextBone)
        {
            var vec1 = api.GetBonePosition(pivotBone);
            var vec2 = api.GetBonePosition(nextBone);
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