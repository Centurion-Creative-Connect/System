using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
namespace CenturionCC.System.Gun
{
    public enum ControlType
    {
        None,
        OneHanded,
        TwoHanded,
    }

    public static class ControlTypeHelper
    {
        public static string ToEnumString(this ControlType type)
        {
            switch (type)
            {
                case ControlType.None: return "None";
                case ControlType.OneHanded: return "OneHanded";
                case ControlType.TwoHanded: return "TwoHanded";
                default: return $"UnknownState:{type}";
            }
        }
    }

    public enum PivotType
    {
        Primary,
        Secondary,
    }

    public static class PivotTypeHelper
    {
        public static string ToEnumString(this PivotType type)
        {
            switch (type)
            {
                case PivotType.Primary: return "Primary";
                case PivotType.Secondary: return "Secondary";
                default: return $"UnknownState:{type}";
            }
        }
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunPositioningHelper : UdonSharpBehaviour
    {
        [NewbieInject(SearchScope.Children)]
        [SerializeField]
        private VRCObjectSync[] objectSyncs;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private Rigidbody rb;

        [SerializeField] private Transform target;
        [SerializeField] private Transform primaryHandle;
        [SerializeField] private Transform secondaryHandle;

        [UdonSynced] private ControlType _controlType;

        private Matrix4x4 _pivotLookAtOffset;
        [UdonSynced] private Vector3 _pivotLookAtOffsetPos;

        private Transform _pivotLookAtTransform;
        private Matrix4x4 _pivotOffset;

        [UdonSynced] private Vector3 _pivotOffsetPos;
        [UdonSynced] private Quaternion _pivotOffsetRot;

        private Transform _pivotTransform;

        [UdonSynced] private PivotType _pivotType;

        private Matrix4x4 _primaryOffset;

        [UdonSynced] private float _primaryXAngleOffset;

        private float _recoilErgonomics;
        private Vector3 _recoilOffsetPos = Vector3.zero;
        private Quaternion _recoilOffsetRot = Quaternion.identity;
        private Matrix4x4 _secondaryOffset;
        [UdonSynced] private bool _useGravity;

        private void Start()
        {
            if (target == null)
                target = transform;

            if (primaryHandle == null || secondaryHandle == null)
                Debug.LogError("Primary and Secondary handles are required.", this);

            RecalculatePrimary();
            RecalculatePivot();
        }

#if CENTURIONSYSTEM_GUN_PHYSICS
        private void FixedUpdate()
#else
        private void Update()
#endif
        {
            var t = 1 - Mathf.Exp(-_recoilErgonomics * Time.deltaTime);
            _recoilOffsetRot = Quaternion.Lerp(_recoilOffsetRot, Quaternion.identity, t);
            _recoilOffsetPos = Vector3.Lerp(_recoilOffsetPos, Vector3.zero, t);

            _UpdatePosition();
        }

        public override void OnDeserialization()
        {
            _pivotTransform = _pivotType == PivotType.Primary ? primaryHandle : secondaryHandle;
            _pivotLookAtTransform = _pivotType == PivotType.Primary ? secondaryHandle : primaryHandle;
            _pivotLookAtOffset = _pivotType == PivotType.Primary ? _secondaryOffset : _primaryOffset;
            _pivotOffset = Matrix4x4.TRS(_pivotOffsetPos, _pivotOffsetRot, Vector3.one);

            UpdateRigidbody();

#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_GUN_LOGGING
            Debug.Log($"{name}: OnDeserialization: {ToString()}");
#endif
        }

        private void RecalculatePrimary()
        {
            var primaryOffset = primaryHandle.worldToLocalMatrix * target.localToWorldMatrix;
            var secondaryOffset = secondaryHandle.worldToLocalMatrix * target.localToWorldMatrix;

            _primaryOffset = Matrix4x4.TRS(primaryOffset.GetPosition(), primaryOffset.rotation, Vector3.one);
            _secondaryOffset = Matrix4x4.TRS(secondaryOffset.GetPosition(), secondaryOffset.rotation, Vector3.one);
        }

        private void RecalculatePivot()
        {
            _pivotTransform = _pivotType == PivotType.Primary ? primaryHandle : secondaryHandle;
            _pivotLookAtTransform = _pivotType == PivotType.Primary ? secondaryHandle : primaryHandle;
            _pivotLookAtOffset = _pivotType == PivotType.Primary ? _secondaryOffset : _primaryOffset * Matrix4x4.Rotate(Quaternion.AngleAxis(_primaryXAngleOffset, Vector3.right));

            _pivotOffset = _pivotType == PivotType.Primary ? _primaryOffset * Matrix4x4.Rotate(Quaternion.AngleAxis(_primaryXAngleOffset, Vector3.right)) : _pivotTransform.worldToLocalMatrix * target.localToWorldMatrix;
            _pivotOffsetPos = _pivotOffset.GetPosition();
            _pivotOffsetRot = _pivotOffset.rotation;

            _pivotLookAtOffsetPos = target.worldToLocalMatrix.MultiplyPoint3x4(_pivotLookAtTransform.position);
        }

        private void UpdateRigidbody()
        {
            if (rb == null) return;

#if CENTURIONSYSTEM_GUN_PHYSICS
            rb.isKinematic = false;
            rb.useGravity = _useGravity && _controlType == ControlType.None;
            rb.drag = 0.5f;
#else
            rb.isKinematic = !_useGravity || _controlType != ControlType.None;
            rb.useGravity = _useGravity && _controlType == ControlType.None;
            rb.drag = 0;
#endif
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        [PublicAPI]
        public void _UpdatePosition()
        {
            var recoilMatrix = Matrix4x4.TRS(_recoilOffsetPos, _recoilOffsetRot, Vector3.one);

            switch (_controlType)
            {
                default:
                case ControlType.None:
                {
                    if (Networking.IsOwner(gameObject))
                    {
                        var targetMatrix = target.localToWorldMatrix * _pivotOffset.inverse;
                        _pivotTransform.SetPositionAndRotation(targetMatrix.GetPosition(), targetMatrix.rotation);
                        var lookAtMatrix = targetMatrix * _pivotLookAtOffset;
                        _pivotLookAtTransform.SetPositionAndRotation(lookAtMatrix.GetPosition(), lookAtMatrix.rotation);
                    }
                    else
                    {
                        var targetMatrix = _pivotTransform.localToWorldMatrix * _pivotOffset * recoilMatrix;

#if CENTURIONSYSTEM_GUN_PHYSICS
                        rb.MovePosition(targetMatrix.GetPosition());
                        rb.MoveRotation(targetMatrix.rotation);
#else
                        target.SetPositionAndRotation(targetMatrix.GetPosition(), targetMatrix.rotation);
#endif
                    }

                    break;
                }
                case ControlType.OneHanded:
                {
                    var targetMatrix = _pivotTransform.localToWorldMatrix * _pivotOffset * recoilMatrix;

#if CENTURIONSYSTEM_GUN_PHYSICS
                    rb.MovePosition(targetMatrix.GetPosition());
                    rb.MoveRotation(targetMatrix.rotation);
#else
                    target.SetPositionAndRotation(targetMatrix.GetPosition(), targetMatrix.rotation);
#endif
                    var lookAtMatrix = targetMatrix * _pivotLookAtOffset;
                    _pivotLookAtTransform.SetPositionAndRotation(lookAtMatrix.GetPosition(), lookAtMatrix.rotation);
                    break;
                }
                case ControlType.TwoHanded:
                {
                    var targetMatrix = _pivotTransform.localToWorldMatrix * _pivotOffset * recoilMatrix;

                    var desiredSecondaryPos = _pivotLookAtTransform.position;
                    var currentSecondaryPos = targetMatrix.MultiplyPoint3x4(_pivotLookAtOffsetPos);

                    var currentDir = currentSecondaryPos - targetMatrix.GetPosition();
                    var desiredDir = desiredSecondaryPos - targetMatrix.GetPosition();

                    var rotCorrection = Quaternion.FromToRotation(currentDir, desiredDir);

#if CENTURIONSYSTEM_GUN_PHYSICS
                    rb.MovePosition(targetMatrix.GetPosition());
                    rb.MoveRotation(rotCorrection * targetMatrix.rotation);
#else
                    target.SetPositionAndRotation(targetMatrix.GetPosition(), rotCorrection * targetMatrix.rotation);
#endif
                    break;
                }
            }
        }

        [PublicAPI]
        public void _RequestSync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            foreach (var sync in objectSyncs)
                if (!Networking.IsOwner(sync.gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);

            RequestSerialization();
        }

        [PublicAPI]
        public void SetOffsets(Matrix4x4 primaryOffset, Matrix4x4 secondaryOffset)
        {
            _primaryOffset = primaryOffset;
            _secondaryOffset = secondaryOffset;

            RecalculatePivot();

#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_GUN_LOGGING
            Debug.Log("Offsets updated");
#endif
        }

        [PublicAPI]
        public void SetControlAndPivot(ControlType controlType, PivotType pivotType)
        {
            _controlType = controlType;
            _pivotType = pivotType;

            if (pivotType == PivotType.Secondary)
            {
                _controlType = ControlType.OneHanded;
            }

            UpdateRigidbody();
            RecalculatePivot();
            _RequestSync();
        }

        [PublicAPI]
        public void SetGravity(bool useGravity)
        {
            _useGravity = useGravity;
            UpdateRigidbody();
        }

        [PublicAPI]
        public void SetRecoilErgonomics(float recoilErgonomics)
        {
            _recoilErgonomics = recoilErgonomics;
        }

        [PublicAPI]
        public void AddRecoil(Vector3 position, Quaternion rotation)
        {
            _recoilOffsetPos += position;
            _recoilOffsetRot *= rotation;
        }

        [PublicAPI]
        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            FlagDiscontinuity();

            var targetMatrix = Matrix4x4.TRS(position, rotation, Vector3.one) * _primaryOffset.inverse;
            target.SetPositionAndRotation(targetMatrix.GetPosition(), targetMatrix.rotation);

            var primaryMatrix = targetMatrix * _primaryOffset;
            primaryHandle.SetPositionAndRotation(primaryMatrix.GetPosition(), primaryMatrix.rotation);

            var secondaryMatrix = targetMatrix * _secondaryOffset;
            secondaryHandle.SetPositionAndRotation(secondaryMatrix.GetPosition(), secondaryMatrix.rotation);
        }

        [PublicAPI]
        public void SetPrimaryXAngleOffset(float angle)
        {
            _primaryXAngleOffset = angle;
            RecalculatePivot();
        }

        [PublicAPI]
        public void FlagDiscontinuity()
        {
            UpdateRigidbody();

            foreach (var sync in objectSyncs)
            {
                if (!Networking.IsOwner(sync.gameObject))
                    Networking.SetOwner(Networking.LocalPlayer, sync.gameObject);

                sync.FlagDiscontinuity();
            }
        }

        public override string ToString()
        {
            return "GunPositioningHelper: " +
                   $"{_controlType.ToEnumString()} {_pivotType.ToEnumString()} {_useGravity} {_recoilErgonomics}";
        }
    }
}
