using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gun
{
    public enum ControlType
    {
        OneHanded,
        TwoHanded,
    }

    public enum PivotType
    {
        Primary,
        Secondary,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunPositioningHelper : UdonSharpBehaviour
    {
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

        private void Start()
        {
            if (target == null)
                target = transform;

            if (primaryHandle == null || secondaryHandle == null)
                Debug.LogError("Primary and Secondary handles are required.", this);

            RecalculatePrimary();
            RecalculatePivot();
        }

        private void Update()
        {
            var t = 1 - Mathf.Exp(-_recoilErgonomics * Time.deltaTime);
            _recoilOffsetRot = Quaternion.Lerp(_recoilOffsetRot, Quaternion.identity, t);
            _recoilOffsetPos = Vector3.Lerp(_recoilOffsetPos, Vector3.zero, t);
            var recoilMatrix = Matrix4x4.TRS(_recoilOffsetPos, _recoilOffsetRot, Vector3.one);

            switch (_controlType)
            {
                default:
                case ControlType.OneHanded:
                {
                    var targetMatrix = _pivotTransform.localToWorldMatrix * _pivotOffset * recoilMatrix;
                    target.SetPositionAndRotation(targetMatrix.GetPosition(), targetMatrix.rotation);

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

                    target.SetPositionAndRotation(targetMatrix.GetPosition(), rotCorrection * targetMatrix.rotation);
                    break;
                }
            }
        }

        public override void OnDeserialization()
        {
            _pivotTransform = _pivotType == PivotType.Primary ? primaryHandle : secondaryHandle;
            _pivotLookAtTransform = _pivotType == PivotType.Primary ? secondaryHandle : primaryHandle;
            _pivotLookAtOffset = _pivotType == PivotType.Primary ? _secondaryOffset : _primaryOffset;
            _pivotOffset = Matrix4x4.TRS(_pivotOffsetPos, _pivotOffsetRot, Vector3.one);
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

        private void Sync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            RequestSerialization();
        }

        public void SetOffsets(Matrix4x4 primaryOffset, Matrix4x4 secondaryOffset)
        {
            _primaryOffset = primaryOffset;
            _secondaryOffset = secondaryOffset;

            RecalculatePivot();
            Debug.Log("Offsets updated");
        }

        public void SetControlAndPivot(ControlType controlType, PivotType pivotType)
        {
            _controlType = controlType;
            _pivotType = pivotType;

            if (pivotType == PivotType.Secondary)
            {
                _controlType = ControlType.OneHanded;
            }

            RecalculatePivot();
            Sync();
        }

        public void SetRecoilErgonomics(float recoilErgonomics)
        {
            _recoilErgonomics = recoilErgonomics;
        }

        public void AddRecoil(Vector3 position, Quaternion rotation)
        {
            _recoilOffsetPos += position;
            _recoilOffsetRot *= rotation;
        }

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            var targetMatrix = Matrix4x4.TRS(position, rotation, Vector3.one) * _primaryOffset.inverse;
            target.SetPositionAndRotation(targetMatrix.GetPosition(), targetMatrix.rotation);

            var primaryMatrix = targetMatrix * _primaryOffset;
            primaryHandle.SetPositionAndRotation(primaryMatrix.GetPosition(), primaryMatrix.rotation);

            var secondaryMatrix = targetMatrix * _secondaryOffset;
            secondaryHandle.SetPositionAndRotation(secondaryMatrix.GetPosition(), secondaryMatrix.rotation);
        }

        public void SetPrimaryXAngleOffset(float angle)
        {
            _primaryXAngleOffset = angle;
            RecalculatePivot();
            Sync();
        }
    }
}
