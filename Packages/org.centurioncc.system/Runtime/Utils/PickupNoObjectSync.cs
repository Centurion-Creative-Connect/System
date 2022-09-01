using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PickupNoObjectSync : UdonSharpBehaviour
    {
        // private bool _isCheckingPosition = false;

        private const string DebugLogText =
            "[PNOS] IsHeld: {0}, PlayerId: {1}, trackingDataType: {2}, offsetPos: {3}, offsetRot: {4}";
        public Transform target;
        public float lateEventDelay = 1F;
        public float offsetUpdateCheckDelay = 15F;
        private VRCPlayerApi _cachedApi;

        [UdonSynced]
        private Vector3 _dropPos;
        [UdonSynced]
        private Quaternion _dropRot;

        [UdonSynced] [FieldChangeCallback(nameof(IsHeld))]
        private bool _isHeld;

        private VRCPickup _pickup;
        [UdonSynced] [FieldChangeCallback(nameof(PlayerId))]
        private int _playerId;

        [UdonSynced]
        private Vector3 _relativePos;
        [UdonSynced]
        private float _rotAngle;
        [UdonSynced]
        private Vector3 _rotAxis;
        [UdonSynced]
        private byte _trackingBone;

        public bool IsHeld
        {
            get => _isHeld;
            private set
            {
                if (value)
                    MoveToTrackingPosition();
                else
                    SendCustomEventDelayedSeconds(nameof(MoveToDropPosition), lateEventDelay);

                _isHeld = value;
            }
        }
        public int PlayerId
        {
            get => _playerId;
            private set
            {
                if (IsHeld && _playerId == Networking.LocalPlayer.playerId && _playerId != value)
                {
                    Debug.Log("[PickupNoObjectSync] remote possibly stolen local's pickup");
                    _pickup.Drop(Networking.LocalPlayer);
                }

                _playerId = value;
                _cachedApi = VRCPlayerApi.GetPlayerById(value);
            }
        }
        public HumanBodyBones TrackingBone
        {
            get => (HumanBodyBones) _trackingBone;
            private set => _trackingBone = (byte) value;
        }

        private void Start()
        {
            _pickup = (VRCPickup) GetComponent(typeof(VRCPickup));
            if (target == null)
                target = transform;
            if (Networking.IsMaster)
            {
                // Initialize first drop position
                var pickupTransform = _pickup.transform;
                _dropPos = pickupTransform.position;
                _dropRot = pickupTransform.rotation;
            }
        }

        public override void PostLateUpdate()
        {
            if (IsHeld)
            {
                if (_cachedApi == null || !_cachedApi.IsValid())
                {
                    OnDrop();
                    return;
                }

                var bonePos = _cachedApi.GetBonePosition(TrackingBone);
                var boneRot = _cachedApi.GetBoneRotation(TrackingBone);
                var boneScale = Vector3.one;

                var localToWorldMatrix = Matrix4x4.TRS(bonePos, boneRot, boneScale);

                var worldPos = localToWorldMatrix.MultiplyPoint3x4(_relativePos);
                var worldAxis = localToWorldMatrix.MultiplyVector(_rotAxis);
                var worldRot = Quaternion.AngleAxis(_rotAngle, worldAxis) * boneRot;

                target.SetPositionAndRotation(worldPos, worldRot);
            }
        }

        public void MoveToLocalPosition(Vector3 pos, Quaternion rot)
        {
            Debug.LogError("PickupNoObjectSync: NotImplemented: MoveToLocalPosition");
            // TODO: impl
        }

        public void MoveToTrackingPosition()
        {
            if (_cachedApi == null || !_cachedApi.IsValid())
            {
                OnDrop();
                return;
            }

            var bonePos = _cachedApi.GetBonePosition(TrackingBone);
            var boneRot = _cachedApi.GetBoneRotation(TrackingBone);
            var boneScale = Vector3.one;

            var localToWorldMatrix = Matrix4x4.TRS(bonePos, boneRot, boneScale);

            var worldPos = localToWorldMatrix.MultiplyPoint3x4(_relativePos);
            var worldAxis = localToWorldMatrix.MultiplyVector(_rotAxis);
            var worldRot = Quaternion.AngleAxis(_rotAngle, worldAxis) * boneRot;

            target.SetPositionAndRotation(worldPos, worldRot);
        }

        public void MoveToDropPosition()
        {
            target.SetPositionAndRotation(_dropPos, _dropRot);
        }


        public override void OnDeserialization()
        {
            Debug.Log("[PickupNoObjectSync] on deserialization");
            Debug.LogFormat("IsHeld: {0}, PlayerId: {1}, TrackingBone: {2}, deltaPos: {3}, deltaRot: {4}",
                IsHeld, PlayerId, TrackingBone, _relativePos, _rotAngle);
        }

        public override void OnPickup()
        {
            IsHeld = true;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(_SendData), lateEventDelay);
        }

        public override void OnDrop()
        {
            IsHeld = false;
            _pickup.pickupable = true;
            SendCustomEventDelayedSeconds(nameof(_SendData), lateEventDelay);
        }

        public void _SendData()
        {
            var api = Networking.LocalPlayer;
            if (api == null || !api.IsValid())
            {
                Debug.LogError("[PickupNoObjectSync] LocalPlayer is null or not valid!", this);
                return;
            }

            PlayerId = api.playerId;
            TrackingBone = ToHumanBone(_pickup.currentHand);

            // NOTE: Calculate relative position and rotation
            {
                var bonePos = api.GetBonePosition(TrackingBone);
                var boneRot = api.GetBoneRotation(TrackingBone);
                var boneScale = Vector3.one;

                // Basically Transform.worldToLocalMatrix
                var worldToLocalMatrix = Matrix4x4.TRS(bonePos, boneRot, boneScale).inverse;

                // Transform.InverseTransformPoint
                _relativePos = worldToLocalMatrix.MultiplyPoint3x4(target.position);
                var rotMatrix = target.rotation * Quaternion.Inverse(boneRot);

                // ReSharper disable once InlineOutVariableDeclaration
                Vector3 rotGlobalAxis;
                rotMatrix.ToAngleAxis(out _rotAngle, out rotGlobalAxis);

                // Transform.InverseTransformVector
                _rotAxis = worldToLocalMatrix.MultiplyVector(rotGlobalAxis);
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        //
        // private void _StartOffsetCheck()
        // {
        //     if (_isCheckingPosition)
        //     {
        //         Debug.Log("[PickupNoObjectSync] object's already checking it's offset position");
        //         return;
        //     }
        //
        //     _isCheckingPosition = true;
        //     SendCustomEventDelayedSeconds(nameof(_CheckOffsetUpdate), offsetUpdateCheckDelay);
        // }
        //
        // public void _CheckOffsetUpdate()
        // {
        //     if (!IsHeld)
        //     {
        //         Debug.Log("[PickupNoObjectSync] CheckOffsetUpdate: object's no longer in pickup", this);
        //         _isCheckingPosition = false;
        //         return;
        //     }
        //
        //     var api = VRCPlayerApi.GetPlayerById(PlayerId);
        //
        //     if (api == null || !api.IsValid())
        //     {
        //         Debug.LogWarning("[PickupNoObjectSync] CheckOffsetUpdate: object's holder is no longer in instance",
        //             this);
        //         _isCheckingPosition = false;
        //         return;
        //     }
        //
        //     if (!api.isLocal)
        //     {
        //         Debug.LogWarning("[PickupNoObjectSync] CheckOffsetUpdate: object's holder is not local", this);
        //         _isCheckingPosition = false;
        //         return;
        //     }
        //
        //     Vector3 newRelativePos;
        //     float newRotAngle;
        //
        //     // NOTE: Calc relative pos and angle
        //     {
        //         var bonePos = api.GetBonePosition(TrackingBone);
        //         var boneRot = api.GetBoneRotation(TrackingBone);
        //         var boneScale = Vector3.one;
        //
        //         // Basically Transform.worldToLocalMatrix
        //         var worldToLocalMatrix = Matrix4x4.TRS(bonePos, boneRot, boneScale).inverse;
        //
        //         // Transform.InverseTransformPoint
        //         newRelativePos = worldToLocalMatrix.MultiplyPoint3x4(target.position);
        //         var rotMatrix = target.rotation * Quaternion.Inverse(boneRot);
        //
        //         // ReSharper disable once InlineOutVariableDeclaration
        //         Vector3 _;
        //         rotMatrix.ToAngleAxis(out newRotAngle, out _);
        //     }
        //
        //     // NOTE: Check Positions
        //     {
        //         var posDistanceDiff = Vector3.Distance(newRelativePos, _relativePos);
        //         var angleDiff = Mathf.Abs(newRotAngle - _rotAngle);
        //
        //         if (posDistanceDiff > 1F || angleDiff > 1F)
        //         {
        //             Debug.Log(
        //                 "[PickupNoObjectSync] difference between new and old relative position is too large! updating!");
        //             Debug.LogFormat("[PickupNoObjectSync] posDistance: {0}, newRotAngle: {1}",
        //                 posDistanceDiff, newRotAngle);
        //
        //             _SendData();
        //         }
        //     }
        //
        //     // NOTE: Re-schedule check
        //     SendCustomEventDelayedSeconds(nameof(_CheckOffsetUpdate), offsetUpdateCheckDelay);
        // }

        private HumanBodyBones ToHumanBone(VRC_Pickup.PickupHand hand)
        {
            switch (hand)
            {
                case VRC_Pickup.PickupHand.Left:
                    return HumanBodyBones.LeftHand;
                case VRC_Pickup.PickupHand.None:
                case VRC_Pickup.PickupHand.Right:
                default:
                    return HumanBodyBones.RightHand;
            }
        }

        private VRC_Pickup.PickupHand ToPickupHand(HumanBodyBones hand)
        {
            switch (hand)
            {
                case HumanBodyBones.LeftHand:
                    return VRC_Pickup.PickupHand.Left;
                case HumanBodyBones.RightHand:
                    return VRC_Pickup.PickupHand.Right;
                default:
                    return VRC_Pickup.PickupHand.None;
            }
        }
    }
}