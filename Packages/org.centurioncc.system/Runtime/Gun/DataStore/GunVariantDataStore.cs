using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.GunCamera;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.DataStore
{
    [SelectionBase] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunVariantDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private byte uniqueId;
        [SerializeField]
        private string weaponName;
        [SerializeField]
        private int holsterSize = 100;
        [SerializeField]
        private FireMode[] availableFiringModes = { FireMode.SemiAuto };
        [SerializeField]
        private float maxRoundsPerSecond;
        [SerializeField]
        private GameObject model;
        [SerializeField]
        private ProjectileDataProvider projectileData;
        [SerializeField]
        private GunAudioDataStore audioData;
        [SerializeField]
        private GunHapticDataStore hapticData;
        [SerializeField]
        private GunCameraDataStore cameraData;
        [SerializeField]
        private GunBehaviourBase behaviour;
        [SerializeField]
        private bool isDoubleHanded;
        [SerializeField]
        private bool useRePickupDelayForMainHandle;
        [SerializeField]
        private bool useRePickupDelayForSubHandle;
        [SerializeField]
        private Transform modelOffset;
        [SerializeField]
        private Transform shooterOffset;
        [SerializeField]
        private Transform mainHandleOffset;
        [SerializeField]
        private float mainHandlePitchOffset;
        [SerializeField]
        private Transform subHandleOffset;
        [SerializeField]
        private BoxCollider colliderSetting;
        [Header("Messages")]
        [SerializeField]
        private TranslatableMessage desktopTooltip;
        [SerializeField]
        private TranslatableMessage vrTooltip;

        public byte UniqueId => uniqueId;
        public string WeaponName => weaponName;
        public int HolsterSize => holsterSize;
        public FireMode[] AvailableFiringModes => availableFiringModes;
        public float MaxRoundsPerSecond => maxRoundsPerSecond;
        public GameObject Model => model;
        public ProjectileDataProvider ProjectileData => projectileData;
        public GunAudioDataStore AudioData => audioData;
        public GunCameraDataStore CameraData => cameraData;
        public GunHapticDataStore HapticData => hapticData;
        public GunBehaviourBase Behaviour => behaviour;
        public bool IsDoubleHanded => isDoubleHanded;
        public bool UseRePickupDelayForMainHandle => useRePickupDelayForMainHandle;
        public bool UseRePickupDelayForSubHandle => useRePickupDelayForSubHandle;
        public Vector3 ModelPositionOffset =>
            modelOffset ? modelOffset.localPosition : Vector3.zero;
        public Quaternion ModelRotationOffset =>
            modelOffset ? modelOffset.localRotation : Quaternion.identity;
        public Vector3 FiringPositionOffset =>
            shooterOffset ? shooterOffset.localPosition : Vector3.zero;
        public Quaternion FiringRotationOffset =>
            shooterOffset ? shooterOffset.localRotation : Quaternion.identity;
        public Vector3 MainHandlePositionOffset =>
            mainHandleOffset ? mainHandleOffset.localPosition : Vector3.back;
        public Quaternion MainHandleRotationOffset =>
            mainHandleOffset ? mainHandleOffset.localRotation : Quaternion.identity;
        public Vector3 SubHandlePositionOffset =>
            subHandleOffset ? subHandleOffset.localPosition : Vector3.forward;
        public Quaternion SubHandleRotationOffset =>
            subHandleOffset ? subHandleOffset.localRotation : Quaternion.identity;
        public float MainHandlePitchOffset => mainHandlePitchOffset;
        public bool HasColliderSetting => colliderSetting != null;
        public Vector3 ColliderCenter => HasColliderSetting ? colliderSetting.center : Vector3.zero;
        public Vector3 ColliderSize => HasColliderSetting ? colliderSetting.size : Vector3.zero;

        public string TooltipMessage =>
            Networking.LocalPlayer.IsUserInVR() ? _MessageOrEmpty(vrTooltip) : _MessageOrEmpty(desktopTooltip);

        private string _MessageOrEmpty(TranslatableMessage message)
        {
            return message != null ? message.Message : "";
        }
    }
}