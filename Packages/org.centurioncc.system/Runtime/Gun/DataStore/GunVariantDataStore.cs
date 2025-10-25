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
    [SelectionBase]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunVariantDataStore : UdonSharpBehaviour
    {
        [SerializeField] private byte uniqueId;

        [SerializeField] private string weaponName;

        [SerializeField] private int holsterSize = 100;

        [SerializeField] private float ergonomics = 3;

        [SerializeField] private FireMode[] availableFiringModes = { FireMode.SemiAuto };

        [SerializeField] private float maxRoundsPerSecond;

        [SerializeField] private Animator animator;

        [SerializeField] private string[] syncedAnimatorParameterNames;

        [SerializeField] private ProjectileDataProvider projectileData;

        [SerializeField] private GunAudioDataStore audioData;

        [SerializeField] private GunHapticDataStore hapticData;

        [SerializeField] private GunCameraDataStore cameraData;

        [SerializeField] private GunBehaviourBase[] behaviours;

        [SerializeField] [NewbieInject]
        private ProjectilePoolBase defaultProjectilePool;

        [SerializeField]
        [Tooltip("Override default ProjectilePool for this variant. leave this empty to use default ProjectilePool")]
        private ProjectilePoolBase projectilePoolOverride;

        [SerializeField] private bool isDoubleHanded;

        [SerializeField] private bool useRePickupDelayForMainHandle;

        [SerializeField] private bool useRePickupDelayForSubHandle;

        [SerializeField] private bool useWallCheck = true;

        [SerializeField] private bool useSafeZoneCheck = true;

        [SerializeField] private Transform shooterOffset;

        [SerializeField] private Transform mainHandleOffset;

        [SerializeField] private float mainHandlePitchOffset;

        [SerializeField] private Transform subHandleOffset;

        [Header("Messages")]
        [SerializeField] private TranslatableMessage desktopTooltip;

        [SerializeField] private TranslatableMessage vrTooltip;

        [Header("ObjectMarker Properties")]
        [SerializeField] private ObjectType objectType = ObjectType.Metallic;

        [SerializeField] private float objectWeight;

        [SerializeField] private string[] tags = { "NoFootstep" };

        [Header("Player Controller Properties")]
        [SerializeField] private MovementOption movementOption = MovementOption.Inherit;

        [SerializeField] private float walkSpeed = 1F;

        [SerializeField] private float sprintSpeed = 1F;

        [SerializeField] private float sprintThresholdMultiplier = 1F;

        [SerializeField] private CombatTagOption combatTagOption = CombatTagOption.Inherit;

        [SerializeField] private float combatTagSpeedMultiplier = 1F;

        [SerializeField] private float combatTagTime = 1F;

        public byte UniqueId => uniqueId;
        public string WeaponName => weaponName;
        public int HolsterSize => holsterSize;
        public FireMode[] AvailableFiringModes => availableFiringModes;
        public float RoundsPerSecond => maxRoundsPerSecond;
        public float SecondsPerRound => 1F / maxRoundsPerSecond;
        public float Ergonomics => ergonomics;
        public string[] SyncedAnimatorParameterNames => syncedAnimatorParameterNames;

        [CanBeNull] public Animator Animator => animator;

        [CanBeNull] public ProjectileDataProvider ProjectileData => projectileData;

        [CanBeNull] public GunAudioDataStore AudioData => audioData;

        [CanBeNull] public GunCameraDataStore CameraData => cameraData;

        [CanBeNull] public GunHapticDataStore HapticData => hapticData;

        public GunBehaviourBase[] Behaviours => behaviours;

        public ProjectilePoolBase ProjectilePool => projectilePoolOverride ? projectilePoolOverride : defaultProjectilePool;

        public bool IsDoubleHanded => isDoubleHanded;
        public bool UseWallCheck => useWallCheck;
        public bool UseSafeZoneCheck => useSafeZoneCheck;

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

        public string TooltipMessage =>
            Networking.LocalPlayer.IsUserInVR() ? _MessageOrEmpty(vrTooltip) : _MessageOrEmpty(desktopTooltip);

        public ObjectType ObjectType => objectType;
        public float ObjectWeight => objectWeight;
        public string[] Tags => tags;

        public MovementOption Movement => movementOption;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float SprintThresholdMultiplier => sprintThresholdMultiplier;
        public CombatTagOption CombatTag => combatTagOption;
        public float CombatTagSpeedMultiplier => combatTagSpeedMultiplier;
        public float CombatTagTime => combatTagTime;

        private static string _MessageOrEmpty(TranslatableMessage message)
        {
            return message != null ? message.Message : "";
        }
    }
}
