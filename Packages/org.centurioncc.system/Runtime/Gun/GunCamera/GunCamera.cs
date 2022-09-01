using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [DefaultExecutionOrder(50000000)]
    public class GunCamera : GunManagerCallbackBase
    {
        [SerializeField]
        private GunManager gunManager;
        [SerializeField]
        private Transform targetTransform;
        [SerializeField]
        private Camera targetGunCamera;
        [SerializeField]
        private GameObject targetCameraObject;
        [SerializeField]
        private VRC_Pickup targetPickup;

        private bool _hasCustomOffset;
        private GunCameraDataStore _lastGunCameraData;
        private int _lastGunOffsetIndex;
        private Transform _lastGunTransform;

        [PublicAPI]
        public bool IsOn
        {
            get => targetGunCamera.enabled;
            set => targetGunCamera.enabled = value;
        }
        [PublicAPI]
        public bool IsPickupable
        {
            get => targetPickup.pickupable;
            set
            {
                targetPickup.pickupable = value;
                if (value)
                    _hasCustomOffset = true;
            }
        }
        [PublicAPI]
        public bool IsVisible
        {
            get => targetCameraObject.activeSelf;
            set => targetCameraObject.SetActive(value);
        }
        [PublicAPI]
        public int OffsetIndex
        {
            get => _lastGunOffsetIndex;
            set
            {
                if (_lastGunCameraData == null || _lastGunTransform == null)
                {
                    Debug.LogError(
                        $"[GunCamera] GunCameraData or TargetTransform is not set! aborting OffsetIndex set call of {value}!");
                    return;
                }

                if (!_GunOffsetExist(value, _lastGunCameraData))
                {
                    Debug.LogError($"[GunCamera] Offset index of {value} does not exist in current gun! resetting!");
                    value = 0;
                }

                _lastGunOffsetIndex = value;
                if (!IsPickupable)
                    _hasCustomOffset = false;

                UpdateGunCameraPosition();
            }
        }

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        public override void OnPickedUpLocally(ManagedGun instance)
        {
            SetGunCamera(instance.transform, instance.CameraData);
        }

        [PublicAPI]
        public void SetGunCamera(Transform target, GunCameraDataStore cameraData)
        {
            _lastGunTransform = target;
            _lastGunCameraData = cameraData;
            UpdateGunCameraPosition();
        }

        [PublicAPI]
        public void UpdateGunCameraPosition()
        {
            if (_lastGunCameraData == null || _lastGunTransform == null)
            {
                Debug.LogError("[GunCamera] GunCameraDataStore or Transform is not set!");
                return;
            }

            if (!_GunOffsetExist(OffsetIndex, _lastGunCameraData))
            {
                Debug.LogError($"[GunCamera] OffsetIndex {OffsetIndex} is invalid! resetting to 0...");
                OffsetIndex = 0;
            }

            targetTransform.SetParent(_lastGunTransform, false);
            if (!_hasCustomOffset)
            {
                var camData = _lastGunCameraData;
                var gunCamPoses = GunCameraDataStore.GetOrDefaultPositionOffsets(camData);
                var gunCamRots = GunCameraDataStore.GetOrDefaultRotationOffsets(camData);
                targetTransform.localPosition = gunCamPoses[OffsetIndex];
                targetTransform.localRotation = gunCamRots[OffsetIndex];
            }
        }

        private static bool _GunOffsetExist(int index, GunCameraDataStore camData)
        {
            if (camData == null) return false;
            var gunCamPoses = GunCameraDataStore.GetOrDefaultPositionOffsets(camData);
            var gunCamRots = GunCameraDataStore.GetOrDefaultRotationOffsets(camData);
            return index >= 0 && index < Mathf.Min(gunCamPoses.Length, gunCamRots.Length);
        }
    }
}