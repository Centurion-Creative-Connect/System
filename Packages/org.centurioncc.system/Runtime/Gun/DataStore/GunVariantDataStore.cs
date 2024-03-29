using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.GunCamera;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using JetBrains.Annotations;
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
        [Tooltip(
            "Override default ProjectilePool for this variant. leave this empty to use GunManager's ProjectilePool")]
        private ProjectilePool projectilePoolOverride;
        [SerializeField]
        private bool isDoubleHanded;
        [SerializeField]
        private bool useRePickupDelayForMainHandle;
        [SerializeField]
        private bool useRePickupDelayForSubHandle;
        [SerializeField]
        private bool useWallCheck = true;
        [SerializeField]
        private bool useSafeZoneCheck = true;
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
        [Header("ObjectMarker Properties")]
        [SerializeField]
        private ObjectType objectType = ObjectType.Metallic;
        [SerializeField]
        private float objectWeight = 0F;
        [SerializeField]
        private string[] tags = { "NoFootstep" };

        public byte UniqueId => uniqueId;
        public string WeaponName => weaponName;
        public int HolsterSize => holsterSize;
        public FireMode[] AvailableFiringModes => availableFiringModes;
        public float MaxRoundsPerSecond => maxRoundsPerSecond;

        [CanBeNull]
        public GameObject Model => model;
        [CanBeNull]
        public ProjectileDataProvider ProjectileData => projectileData;
        [CanBeNull]
        public GunAudioDataStore AudioData => audioData;
        [CanBeNull]
        public GunCameraDataStore CameraData => cameraData;
        [CanBeNull]
        public GunHapticDataStore HapticData => hapticData;
        [CanBeNull]
        public GunBehaviourBase Behaviour => behaviour;
        [CanBeNull]
        public ProjectilePool ProjectilePoolOverride => projectilePoolOverride;

        public bool IsDoubleHanded => isDoubleHanded;
        public bool UseRePickupDelayForMainHandle => useRePickupDelayForMainHandle;
        public bool UseRePickupDelayForSubHandle => useRePickupDelayForSubHandle;
        public bool UseWallCheck => useWallCheck;
        public bool UseSafeZoneCheck => useSafeZoneCheck;
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

        public ObjectType ObjectType => objectType;
        public float ObjectWeight => objectWeight;
        public string[] Tags => tags;

        private string _MessageOrEmpty(TranslatableMessage message)
        {
            return message != null ? message.Message : "";
        }
    }
}