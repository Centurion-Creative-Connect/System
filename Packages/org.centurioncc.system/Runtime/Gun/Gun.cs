using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Gun : GunBase
    {
        private const string LeftHandTrigger = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        private const string RightHandTrigger = "Oculus_CrossPlatform_SecondaryIndexTrigger";
        private const float FreeHandPickupProximity = .1F;
        private const float SwapHandDisallowPickupProximity = 0.15F;
        private const float DisallowPickupFromBelowRange = -0.1F;

        [UdonSynced] [FieldChangeCallback(nameof(RawState))]
        private byte _currentState;

        private FireMode _fireMode;

        private bool _isLocal;

        private bool _isPickedUp;
        private bool _mainHandleIsPickedUp;

        private Vector3 _mainHandlePosOffset;
        private Quaternion _mainHandleRotOffset;
        private float _nextMainHandlePickupableTime;
        private float _nextSubHandlePickupableTime;

        protected GunHandle _pivotHandle;

        [UdonSynced]
        protected Vector3 _pivotPosOffset = Vector3.zero;
        [UdonSynced]
        protected Quaternion _pivotRotOffset = Quaternion.identity;

        [UdonSynced] [FieldChangeCallback(nameof(ShotCount))]
        protected int _shotCount = -1;


        private bool _subHandleIsPickedUp;

        private Vector3 _subHandlePosOffset;
        private Quaternion _subHandleRotOffset;
        private TriggerState _trigger;


        protected string Prefix => $"<color=olive>[{name}]</color> ";

        protected virtual void Start()
        {
            FireMode = AvailableFireModes.Length != 0 ? AvailableFireModes[0] : FireMode.Safety;
            Trigger = FireMode == FireMode.Safety ? TriggerState.Idle : TriggerState.Armed;

            if (Networking.IsMaster)
            {
                State = GunState.Idle;
                ShotCount = 0;
            }

            MainHandle.callback = this;
            MainHandle.handleType = HandleType.MainHandle;
            MainHandle.target = Target;
            var m = MainHandle.transform;
            _mainHandlePosOffset = m.localPosition;
            _mainHandleRotOffset = m.localRotation;

            SubHandle.callback = this;
            SubHandle.handleType = HandleType.SubHandle;
            SubHandle.target = Target;
            var s = SubHandle.transform;
            _subHandlePosOffset = s.localPosition;
            _subHandleRotOffset = s.localRotation;

            CustomHandle.callback = this;
            CustomHandle.handleType = HandleType.CustomHandle;
            CustomHandle.target = Target;
            CustomHandle.transform.localPosition = Vector3.down * 50;

            _pivotHandle = mainHandle;

            Internal_SetPivot(HandleType.MainHandle);
            Internal_UpdateHandlePickupable();

            MainHandle.Detach();
            SubHandle.Detach();

            UpdateManager.SubscribeUpdate(this);
            UpdateManager.SubscribeSlowUpdate(this);

            if (Behaviour != null)
                Behaviour.Setup(this);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            var otherName = other.name.ToLower();
            Logger.LogVerbose($"{Prefix}OnTriggerEnter: {otherName}");

            if (otherName.StartsWith("safezone"))
            {
                IsInSafeZone = true;
                return;
            }

            if (otherName.StartsWith("holster"))
            {
                if (!IsLocal || IsHolstered)
                    return;
                var holster = other.GetComponent<GunHolster>();
                if (holster == null || holster.HoldableSize < RequiredHolsterSize)
                    return;

                Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .5F, 1F, .1F);
                TargetHolster = holster;
                TargetHolster.IsHighlighting = true;
                return;
            }

            ++CollisionCount;

            OnProcessCollisionAudio(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            var otherName = other.name.ToLower();

            if (otherName.StartsWith("safezone"))
            {
                IsInSafeZone = false;
                return;
            }

            if (otherName.StartsWith("holster") && IsLocal)
            {
                if (TargetHolster != null)
                {
                    Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .5F, 1F, .1F);
                    TargetHolster.IsHighlighting = false;
                }

                TargetHolster = null;
                return;
            }

            --CollisionCount;
            // To make sure there arent negative values on it (might happen when turning off this collider)
            if (CollisionCount < 0)
                CollisionCount = 0;
        }

        public virtual void _Update()
        {
            if (QueuedShotCount > 0)
            {
                --QueuedShotCount;
                Internal_Shoot();
            }

            if (IsOptimized && !IsLocal)
                return;

            Internal_UpdateIsPickedUpState();
            UpdatePosition();

            if (!IsLocal)
                return;

            Internal_CheckForHandleDistance();

            if (TargetAnimator != null)
                TargetAnimator.SetFloat(GunUtility.TriggerProgressParameter(), GetMainTriggerPull());
            if (Behaviour != null)
                Behaviour.OnGunUpdate(this);

            if (Input.GetKeyDown(KeyCode.B))
                FireMode = GunUtility.CycleFireMode(FireMode, AvailableFireModes);

            if (IsInWall)
            {
                Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .2F, .02F, .1F);
                Networking.LocalPlayer.PlayHapticEventInHand(SubHandle.CurrentHand, .2F, .02F, .1F);
            }
        }

        public virtual void _SlowUpdate()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
                return;

            IsOptimized = Vector3.Distance(Target.position, Networking.LocalPlayer.GetPosition()) > OptimizationRange;

            if (!IsOptimized)
                return;

            Internal_UpdateIsPickedUpState();
            UpdatePosition();
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value || !IsLocal)
                return;

            FireMode = GunUtility.CycleFireMode(FireMode, AvailableFireModes);
        }

        [PublicAPI]
        public override ShotResult TryToShoot()
        {
            var canShoot = CanShoot();
            switch (canShoot)
            {
                case ShotResult.Paused:
                {
                    return ShotResult.Paused;
                }
                case ShotResult.Succeeded:
                case ShotResult.SucceededContinuously:
                {
                    Shoot();
                    ++BurstCount;
                    HasBulletInChamber = false;

                    if (!FireMode.HasFiredEnough(BurstCount))
                        return ShotResult.SucceededContinuously;

                    Trigger = TriggerState.Fired;
                    return ShotResult.Succeeded;
                }
                case ShotResult.Failed:
                {
                    EmptyShoot();

                    Trigger = TriggerState.Fired;
                    return ShotResult.Failed;
                }
                case ShotResult.Cancelled:
                default:
                {
                    Trigger = TriggerState.Fired;
                    return ShotResult.Cancelled;
                }
            }
        }

        [PublicAPI]
        public override void Shoot()
        {
            ++ShotCount;
            RequestSerialization();
        }

        [PublicAPI]
        public override void EmptyShoot()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_EmptyShoot));
        }

        [PublicAPI]
        public override void MoveTo(Vector3 position, Quaternion rotation)
        {
            Internal_SetRelatedObjectsOwner(Networking.LocalPlayer);
            MainHandle.ForceDrop();
            SubHandle.ForceDrop();
            Target.SetPositionAndRotation(position, rotation);

            MainHandle.MoveToLocalPosition(MainHandlePositionOffset, MainHandleRotationOffset);
            SubHandle.MoveToLocalPosition(SubHandlePositionOffset, SubHandleRotationOffset);

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdatePositionForSync));
        }

        [PublicAPI]
        public override void UpdatePosition()
        {
            Internal_UpdatePosition(
                IsDoubleHandedGun && MainHandle.IsPickedUp && SubHandle.IsPickedUp
                    ? PositionUpdateMethod.LookAt
                    : MainHandle.IsPickedUp
                        ? PositionUpdateMethod.MainHandle
                        : IsPickedUp
                            ? PositionUpdateMethod.PivotHandle
                            : PositionUpdateMethod.NotPickedUp
            );
        }

        [PublicAPI]
        public void UpdatePositionForSync()
        {
            SendCustomEventDelayedSeconds(nameof(UpdatePosition), 0.5F);
            SendCustomEventDelayedSeconds(nameof(UpdatePosition), 1F);
            SendCustomEventDelayedSeconds(nameof(UpdatePosition), 5F);
        }

        [PublicAPI]
        public void SetState(GunState state)
        {
            SetState(Convert.ToByte(state));
        }

        [PublicAPI]
        public void SetState(byte state)
        {
            if (RawState == state) return;
            RawState = state;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        /// <summary>
        /// Gets a LookAt rotation from <see cref="MainHandle"/> to <see cref="SubHandle"/>.
        /// </summary>
        /// <remarks>
        /// Exposed for modification of <see cref="UpdatePosition"/> method.
        /// </remarks>
        /// <returns>Rotation looking from <see cref="MainHandle"/> to <see cref="SubHandle"/> with Z forward Y up orientation</returns>
        [PublicAPI]
        public Quaternion GetLookAtRotation()
        {
            if (!IsDoubleHandedGun)
                return MainHandle.transform.rotation *
                       Quaternion.AngleAxis(CurrentMainHandlePitchOffset, Vector3.right);

            var mainTransform = MainHandle.transform;
            var subTransform = SubHandle.transform;

            var up = mainTransform.up;
            var mainHandlePos = mainTransform.position;
            var rawSubPos = subTransform.position;
            var subHandlePos = rawSubPos + Quaternion.LookRotation(rawSubPos - mainHandlePos, up) * LookAtTargetOffset;

            var look = subHandlePos - mainHandlePos;

            return Quaternion.LookRotation(look, up);
        }

        /// <summary>
        /// Gets holder's current <see cref="MainHandle"/>'s trigger input.
        /// </summary>
        /// <returns>0 if remote, 0-1 depending on how much current holder is pulling the trigger.</returns>
        [PublicAPI]
        public float GetMainTriggerPull()
        {
            if (!IsLocal || !MainHandle.IsPickedUp) return 0F;

            return MainHandle.CurrentHand == VRC_Pickup.PickupHand.Left
                ? Input.GetAxis(LeftHandTrigger)
                : Input.GetAxis(RightHandTrigger);
        }

        /// <summary>
        /// Gets holder's current <see cref="SubHandle"/>'s trigger input.
        /// </summary>
        /// <returns>0 if remote, 0-1 depending on how much current holder is pulling the trigger.</returns>
        [PublicAPI]
        public float GetSubTriggerPull()
        {
            if (!IsLocal || !SubHandle.IsPickedUp) return 0F;

            return SubHandle.CurrentHand == VRC_Pickup.PickupHand.Left
                ? Input.GetAxis(LeftHandTrigger)
                : Input.GetAxis(RightHandTrigger);
        }

        #region SerializeFields

        [SerializeField]
        protected string weaponName;

        [SerializeField]
        protected Transform target;
        [SerializeField]
        protected Transform shooter;
        [SerializeField]
        protected GunHandle mainHandle;
        [SerializeField]
        protected GunHandle subHandle;
        [SerializeField]
        protected GunHandle customHandle;
        [SerializeField]
        protected GunBulletHolder bulletHolder;
        [SerializeField]
        protected Animator animator;
        [SerializeField]
        protected GunBehaviourBase behaviour;
        [SerializeField]
        protected FireMode[] availableFireModes = { FireMode.SemiAuto };
        [SerializeField]
        protected ProjectileDataProvider projectileData;
        [SerializeField]
        protected GunAudioDataStore audioData;
        [SerializeField]
        protected GunHapticDataStore hapticData;
        [SerializeField]
        protected bool isDoubleHanded = true;
        [SerializeField]
        protected float roundsPerSecond = 4.5F;
        [SerializeField]
        protected int requiredHolsterSize = 100;
        [SerializeField]
        protected float mainHandlePitchOffset;
        [SerializeField]
        protected float mainHandleRePickupDelay;
        [SerializeField]
        protected float subHandleRePickupDelay;

        [Header("ObjectMarker Properties")]
        [SerializeField]
        protected ObjectType objectType = ObjectType.Metallic;
        [SerializeField]
        protected float objectWeight = 0F;
        [SerializeField]
        protected string[] tags = { "NoFootstep" };

        [SerializeField] [HideInInspector] [NewbieInject]
        private PrintableBase logger;
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerController playerController;

        #endregion

        #region OverridenProperties

        public override ObjectType ObjectType => objectType;
        public override float ObjectWeight => objectWeight;
        public override float WalkingSpeedMultiplier => 1;
        public override string[] Tags => tags;

        [PublicAPI]
        public override string WeaponName => weaponName;

        [PublicAPI]
        public override GunHandle MainHandle => mainHandle;
        [PublicAPI]
        public override GunHandle SubHandle => subHandle;
        [PublicAPI]
        public override GunHandle CustomHandle => customHandle;
        [PublicAPI]
        public override Transform Target => target ? target : transform;
        [PublicAPI] [CanBeNull]
        public override Animator TargetAnimator => animator;
        [PublicAPI] [CanBeNull]
        public override VRCPlayerApi CurrentHolder => MainHandle.CurrentPlayer ?? SubHandle.CurrentPlayer;
        [PublicAPI]
        public override GunState State
        {
            get
            {
                if (RawState > GunStateHelper.MaxValue)
                    return GunState.Unknown;
                return (GunState)RawState;
            }
            set => SetState(value);
        }
        [PublicAPI]
        public override TriggerState Trigger
        {
            get => _trigger;
            set
            {
                _trigger = value;
                if (value == TriggerState.Armed || value == TriggerState.Idle)
                    BurstCount = 0;
            }
        }

        [PublicAPI]
        public override Vector3 MainHandlePositionOffset => _mainHandlePosOffset;
        [PublicAPI]
        public override Quaternion MainHandleRotationOffset => _mainHandleRotOffset;

        [PublicAPI]
        public override Vector3 SubHandlePositionOffset => _subHandlePosOffset;
        [PublicAPI]
        public override Quaternion SubHandleRotationOffset => _subHandleRotOffset;

        [PublicAPI]
        public override bool IsPickedUp => _isPickedUp;
        [PublicAPI]
        public override bool IsLocal => _isLocal;

        #endregion

        #region SystemReferences

        [PublicAPI]
        public PrintableBase Logger => logger;
        [PublicAPI]
        public UpdateManager UpdateManager => updateManager;
        [PublicAPI]
        public AudioManager AudioManager => audioManager;
        [PublicAPI]
        public PlayerController PlayerController => playerController;

        #endregion

        #region GunProperties

        [PublicAPI]
        public virtual bool IsDoubleHandedGun => isDoubleHanded;
        [PublicAPI]
        public virtual float MainHandleRePickupDelay => mainHandleRePickupDelay;
        [PublicAPI]
        public virtual float MainHandlePitchOffset => mainHandlePitchOffset;
        [PublicAPI]
        public virtual float CurrentMainHandlePitchOffset { get; protected set; }

        [PublicAPI]
        public virtual float SubHandleRePickupDelay => subHandleRePickupDelay;
        [PublicAPI]
        public virtual Vector3 LookAtTargetOffset =>
            new Vector3(0, MainHandlePositionOffset.y - SubHandlePositionOffset.y, 0);


        /// <summary>
        /// Local position of where bullets shooting out from.
        /// </summary>
        [PublicAPI]
        public virtual Vector3 ShooterPositionOffset => shooter.localPosition;
        [PublicAPI]
        public virtual Quaternion ShooterRotationOffset => shooter.localRotation;

        /// <summary>
        /// World-based position of where bullets shooting out from.
        /// </summary>
        [PublicAPI]
        public virtual Vector3 ShooterPosition => shooter.position;
        /// <summary>
        /// World-based rotation of where bullets shooting out from.
        /// </summary>
        [PublicAPI]
        public virtual Quaternion ShooterRotation => shooter.rotation;

        [Obsolete("Use ProjectilePool instead")] [CanBeNull]
        public virtual ProjectilePool BulletHolder => ProjectilePool;
        [PublicAPI]
        public virtual ProjectilePool ProjectilePool => bulletHolder;
        [PublicAPI] [CanBeNull]
        public virtual GunBehaviourBase Behaviour => behaviour;
        [PublicAPI] [CanBeNull]
        public virtual ProjectileDataProvider ProjectileData => projectileData;
        [PublicAPI] [CanBeNull]
        public virtual GunAudioDataStore AudioData => audioData;
        [PublicAPI] [CanBeNull]
        public virtual GunHapticDataStore HapticData => hapticData;

        [PublicAPI]
        public virtual int RequiredHolsterSize => requiredHolsterSize;
        [PublicAPI]
        public virtual float OptimizationRange => 45F;
        [PublicAPI]
        public virtual int MaxQueuedShotCount => 10;
        /// <summary>
        /// Max firing rate of this Gun.
        /// </summary>
        /// <remarks>
        /// Uses cached <see cref="SecondsPerRound" /> property for <see cref="TryToShoot()" />.
        /// </remarks>
        /// <seealso cref="GunVariantDataStore.MaxRoundsPerSecond" />
        /// <seealso cref="SecondsPerRound" />
        [PublicAPI]
        public virtual float RoundsPerSecond => roundsPerSecond;
        /// <summary>
        /// Available <see cref="FireMode"/> set of this Gun.
        /// </summary>
        [PublicAPI]
        public virtual FireMode[] AvailableFireModes => availableFireModes;


        /// <summary>
        /// Last shot time of this Gun.
        /// </summary>
        [PublicAPI]
        public virtual DateTime LastShotTime { get; protected set; }
        /// <summary>
        /// Currently active <see cref="FireMode"/> of this Gun.
        /// </summary>
        [PublicAPI]
        public virtual FireMode FireMode
        {
            get => _fireMode;
            set
            {
                var lastFireMode = _fireMode;
                Internal_SetFireModeWithoutNotify(value);
                OnFireModeChanged(lastFireMode, value);
            }
        }
        [PublicAPI]
        public virtual byte RawState
        {
            get => _currentState;
            protected set
            {
                var lastState = _currentState;
                _currentState = value;
                if (TargetAnimator != null)
                    TargetAnimator.SetInteger(GunUtility.StateParameter(), value);
                if (lastState != value)
                    OnProcessStateChange(lastState, value);
            }
        }

        [PublicAPI]
        public virtual int ShotCount
        {
            get => _shotCount;
            protected set
            {
                if (value <= 0 || _shotCount == -1)
                {
                    Logger.LogVerbose($"{Prefix}Received new ShotCount: {value}");
                    _shotCount = value;
                    QueuedShotCount = 0;
                    return;
                }

                var diff = value - _shotCount;
                Logger.LogVerbose($"{Prefix}Received new ShotCount: {value}, Queueing {diff}");
                QueuedShotCount += diff;
                if (QueuedShotCount > MaxQueuedShotCount)
                {
                    var ignoredShots = QueuedShotCount - MaxQueuedShotCount;
                    Logger.LogWarn($"{Prefix}Queued shots are suffering! ignoring {ignoredShots} shots!");
                    QueuedShotCount = MaxQueuedShotCount;
                }

                if (QueuedShotCount < 0)
                {
                    Logger.LogWarn($"{Prefix}Queued shots underflow! resetting to 0!");
                    QueuedShotCount = 0;
                }

                _shotCount = value;
            }
        }
        [PublicAPI]
        public virtual int QueuedShotCount { get; protected set; }

        [PublicAPI] [CanBeNull]
        public virtual GunHolster TargetHolster { get; protected set; }
        [PublicAPI] [field: UdonSynced]
        public virtual bool IsHolstered { get; protected set; }
        [PublicAPI]
        public virtual bool IsOptimized { get; protected set; }

        /// <summary>
        /// Alias of 1 / <see cref="RoundsPerSecond" />.
        /// </summary>
        /// <seealso cref="RoundsPerSecond" />
        /// <seealso cref="TryToShoot()" />
        [PublicAPI]
        public float SecondsPerRound => 1 / RoundsPerSecond;

        /// <summary>
        /// Represents how many rounds were shot in one trigger.
        /// </summary>
        /// <remarks>
        /// Counts up on shoot.
        /// Resets when <see cref="GunBase.Trigger" /> is set to <see cref="TriggerState.Idle"/> or <see cref="TriggerState.Armed"/>.
        /// </remarks>
        /// <seealso cref="TryToShoot()" />
        [PublicAPI]
        public virtual int BurstCount { get; protected set; }

        /// <summary>
        /// Counter of currently colliding objects.
        /// </summary>
        /// <remarks>
        /// Counts up on OnTriggerEnter
        /// Counts down on OnTriggerExit
        /// </remarks>
        [PublicAPI]
        public virtual int CollisionCount { get; protected set; }

        /// <summary>
        /// Is this Gun inside of a wall?
        /// </summary>
        [PublicAPI]
        public virtual bool IsInWall => CollisionCount != 0;

        /// <summary>
        /// Is this Gun inside of safezone?
        /// </summary>
        [PublicAPI]
        public virtual bool IsInSafeZone { get; protected set; }

        #endregion

        #region Internals

        protected void Internal_Shoot()
        {
            var data = ProjectileData;
            if (data == null)
            {
                Logger.LogError($"{Prefix}ProjectileData not found.");
                return;
            }

            var pool = ProjectilePool;
            if (pool == null)
            {
                Logger.LogError($"{Prefix}ProjectilePool not found.");
                return;
            }

            if (!pool.HasInitialized)
            {
                Logger.LogError($"{Prefix}ProjectilePool not yet initialized.");
                return;
            }

            var playerId = -1;
            if (CurrentHolder != null && Utilities.IsValid(CurrentHolder))
                playerId = CurrentHolder.playerId;

            var dataIndexOffset = (ShotCount - QueuedShotCount) * data.ProjectileCount;
            var pos = ShooterPosition;
            var rot = ShooterRotation;

            for (int i = 0; i < data.ProjectileCount; i++)
            {
                data.Get
                (
                    dataIndexOffset + i,
                    out var posOffset, out var velocity,
                    out var rotOffset, out var torque,
                    out var drag,
                    out var trailTime, out var trailGradient
                );

                var tempRot = rot * rotOffset;
                var tempPos = pos + (tempRot * posOffset);

                var projectile = pool.Shoot
                (
                    tempPos,
                    tempRot,
                    velocity,
                    torque,
                    drag,
                    WeaponName, playerId,
                    trailTime, trailGradient
                );

                if (projectile == null)
                {
                    Logger.LogError($"{Prefix}Projectile not found!!!");
                    continue;
                }

                OnShoot(projectile, i != 0);
            }

            if (TargetAnimator != null)
                TargetAnimator.SetTrigger(GunUtility.IsShootingParameter());
            if (AudioData != null)
                Internal_PlayAudio(AudioData.Shooting);
            if (IsLocal && HapticData != null && HapticData.Shooting)
                HapticData.Shooting.PlayBothHand();

            LastShotTime = DateTime.UtcNow;
            HasCocked = false;
        }

        /// <summary>
        /// Exposed as public for networked event!
        /// </summary>
        public virtual void Internal_EmptyShoot()
        {
            OnEmptyShoot();
            if (AudioData != null)
                Internal_PlayAudio(AudioData.EmptyShooting);
        }

        protected void Internal_PlayAudio(AudioDataStore audioStore)
        {
            AudioManager.PlayAudioAtTransform(audioStore, Target);
        }

        protected void Internal_CheckForHandleDistance()
        {
            if (!SubHandle.IsPickedUp) return;

            var localSubHandlePos = Target.worldToLocalMatrix.MultiplyPoint3x4(SubHandle.transform.position);

            if (Vector3.Distance(localSubHandlePos, SubHandlePositionOffset) > .3F)
                SubHandle.ForceDrop();
        }

        protected void Internal_UpdateIsPickedUpState()
        {
            var mainHandleIsPickedUp = MainHandle.IsPickedUp;
            var subHandleIsPickedUp = SubHandle.IsPickedUp;
            var isPickedUp = mainHandleIsPickedUp || subHandleIsPickedUp;

            if (_isPickedUp != isPickedUp)
            {
                _isPickedUp = isPickedUp;

                if (isPickedUp)
                {
                    TargetHolster = null;
                    MainHandle.UnHolster();
                    IsHolstered = false;
                    Internal_SetPivot(mainHandleIsPickedUp ? HandleType.MainHandle : HandleType.SubHandle);
                    CurrentMainHandlePitchOffset = CurrentHolder != null && CurrentHolder.IsUserInVR()
                        ? MainHandlePitchOffset
                        : 0;
                }
            }

            if (_mainHandleIsPickedUp != mainHandleIsPickedUp)
            {
                _mainHandleIsPickedUp = mainHandleIsPickedUp;
                // if (!IsHolstered)
                //     MainHandle.SetAttached(!mainHandleIsPickedUp);
                if (!mainHandleIsPickedUp)
                {
                    if (subHandleIsPickedUp)
                    {
                        Internal_SetPivot(HandleType.SubHandle);
                    }
                    else
                    {
                        Internal_LimitSubHandlePos();
                    }

                    Internal_LimitMainHandlePos();
                }
            }

            if (_subHandleIsPickedUp != subHandleIsPickedUp)
            {
                _subHandleIsPickedUp = subHandleIsPickedUp;
                // SubHandle.SetAttached(!subHandleIsPickedUp);
                if (!subHandleIsPickedUp)
                {
                    if (mainHandleIsPickedUp)
                    {
                        Internal_SetPivot(HandleType.MainHandle);

                        // Get SubHandle owner back when remote dropped a handle!
                        if (IsLocal)
                            Internal_SetRelatedObjectsOwner(Networking.LocalPlayer);
                    }
                    else
                    {
                        Internal_LimitMainHandlePos();
                    }

                    Internal_LimitSubHandlePos();
                }
            }

            Internal_UpdateHandlePickupable();
        }

        protected void Internal_UpdateHandlePickupable()
        {
            // Only do complex update while picked up
            if (IsLocal && IsPickedUp)
            {
                var localPlayer = Networking.LocalPlayer;
                var inVR = localPlayer.IsUserInVR();
                var leftHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                var rightHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                var currentTime = Time.time;

                MainHandle.SetPickupable(currentTime > _nextMainHandlePickupableTime &&
                                         GetPickupProximity(MainHandle, leftHand, rightHand, inVR, false));
                SubHandle.SetPickupable(IsDoubleHandedGun && currentTime > _nextSubHandlePickupableTime &&
                                        GetPickupProximity(SubHandle, leftHand, rightHand, inVR, false));
                CustomHandle.SetPickupable(Behaviour != null &&
                                           Behaviour.RequireCustomHandle &&
                                           IsPickedUp &&
                                           localPlayer.IsUserInVR() &&
                                           GetPickupProximity(CustomHandle, leftHand, rightHand, inVR, true));
            }
            else
            {
                MainHandle.SetPickupable(true);
                SubHandle.SetPickupable(IsDoubleHandedGun);
                CustomHandle.SetPickupable(Behaviour != null && Behaviour.RequireCustomHandle && IsPickedUp &&
                                           Networking.LocalPlayer.IsUserInVR());
            }
        }

        protected bool GetPickupProximity(GunHandle handle, Vector3 leftHandPos, Vector3 rightHandPos, bool inVR,
            bool disallowFromBelow)
        {
            var handlePos = handle.transform.position;

            // We only want to interrupt proximity when user is in VR mode
            if (!inVR)
                return true;

            if (handle.IsPickedUp)
            {
                // If already picked up, check opposite hand's distance is not too close for being able to switch
                var freeHandDistance =
                    Vector3.Distance(
                        handle.CurrentHand == VRC_Pickup.PickupHand.Left ? rightHandPos : leftHandPos,
                        handlePos);
                return freeHandDistance > FreeHandPickupProximity;
            }

            // If not yet picked up, check closest hand's distance to ensure that handle is possible to pickup
            var lhDistance = Vector3.Distance(leftHandPos, handlePos);
            var rhDistance = Vector3.Distance(rightHandPos, handlePos);
            var closestHandDistance = Mathf.Min(lhDistance, rhDistance);

            var up = Target.up;
            var dot = Mathf.Max(Vector3.Dot(up, (leftHandPos - handlePos).normalized),
                Vector3.Dot(up, (rightHandPos - handlePos).normalized));

            return closestHandDistance < SwapHandDisallowPickupProximity &&
                   (!disallowFromBelow || dot > DisallowPickupFromBelowRange);
        }

        protected void Internal_LimitSubHandlePos()
        {
            const float allowedMoveRangeInZAxis = .05F;
            // Move subHandle match with it's gun's position respecting it's original Z axis movement
            // Max +-0.05 == 5cm movement in Z axis
            var subHandleOffsetZ = SubHandlePositionOffset.z;
            var newZ =
                Mathf.Clamp
                (
                    Target.worldToLocalMatrix.MultiplyPoint3x4(SubHandle.transform.position).z,
                    subHandleOffsetZ - allowedMoveRangeInZAxis,
                    subHandleOffsetZ + allowedMoveRangeInZAxis
                );

            var localToWorldMatrix = Target.localToWorldMatrix;

            SubHandle.transform.SetPositionAndRotation
            (
                localToWorldMatrix.MultiplyPoint3x4(
                    new Vector3
                    (
                        SubHandlePositionOffset.x,
                        SubHandlePositionOffset.y,
                        newZ
                    )),
                SubHandleRotationOffset * localToWorldMatrix.rotation
            );
        }

        protected void Internal_LimitMainHandlePos()
        {
            // Move handles match with it's gun's position

            MainHandle.transform.SetPositionAndRotation
            (
                Target.localToWorldMatrix.MultiplyPoint3x4(MainHandlePositionOffset),
                Target.rotation
            );
        }

        protected void Internal_SetPivot(HandleType handleType)
        {
            switch (handleType)
            {
                case HandleType.MainHandle:
                {
                    _pivotHandle = MainHandle;
                    _pivotRotOffset = MainHandleRotationOffset;
                    _pivotPosOffset = MainHandlePositionOffset;
                    break;
                }
                case HandleType.SubHandle:
                {
                    _pivotHandle = SubHandle;
                    // TODO: better look into this for more cleaner implementation
                    if (IsLocal)
                    {
                        Internal_UpdatePosition(PositionUpdateMethod.LookAt);
                        _pivotRotOffset = Quaternion.Inverse(SubHandle.transform.rotation) * GetLookAtRotation();
                        _pivotPosOffset = Target.InverseTransformPoint(SubHandle.transform.position);
                        Networking.SetOwner(Networking.LocalPlayer, gameObject);
                        RequestSerialization();
                    }

                    break;
                }
                case HandleType.None:
                case HandleType.CustomHandle:
                default:
                {
                    Logger.LogError($"{Prefix}MakePivot: invalid HandleType was provided: {handleType}");
                    break;
                }
            }
        }

        protected void Internal_UpdatePosition(PositionUpdateMethod method)
        {
            Vector3 position;
            Quaternion rotation;

            switch (method)
            {
                default:
                case PositionUpdateMethod.LookAt:
                case PositionUpdateMethod.NotPickedUp:
                    rotation = GetLookAtRotation();
                    position = MainHandle.transform.position + rotation * MainHandlePositionOffset * -1;
                    break;
                case PositionUpdateMethod.MainHandle:
                    var mainHandleTransform = MainHandle.transform;
                    rotation = mainHandleTransform.rotation *
                               Quaternion.AngleAxis(CurrentMainHandlePitchOffset, Vector3.right);
                    position = mainHandleTransform.position + rotation * MainHandlePositionOffset * -1;
                    break;
                case PositionUpdateMethod.PivotHandle:
                    if (_pivotHandle == null)
                        return;

                    var pivotTransform = _pivotHandle.transform;
                    rotation = pivotTransform.rotation * _pivotRotOffset;
                    position = pivotTransform.position + rotation * _pivotPosOffset * -1;
                    break;
            }

            Target.SetPositionAndRotation(position, rotation);

            if (!Networking.IsOwner(gameObject) || method == PositionUpdateMethod.NotPickedUp) return;

            if (!MainHandle.IsPickedUp)
                MainHandle.MoveToLocalPosition(MainHandlePositionOffset, MainHandleRotationOffset);
            if (!SubHandle.IsPickedUp)
                SubHandle.MoveToLocalPosition(SubHandlePositionOffset, SubHandleRotationOffset);
        }

        protected void Internal_SetRelatedObjectsOwner(VRCPlayerApi api)
        {
            Networking.SetOwner(api, gameObject);
            Networking.SetOwner(api, mainHandle.gameObject);
            Networking.SetOwner(api, subHandle.gameObject);
            Networking.SetOwner(api, customHandle.gameObject);
        }

        protected void Internal_SetFireModeWithoutNotify(FireMode fireMode)
        {
            _fireMode = fireMode;
            if (TargetAnimator != null)
                TargetAnimator.SetInteger(GunUtility.SelectorTypeParameter(), (int)fireMode);
        }

        #endregion

        #region CustomizableMethods

        protected virtual void OnGunPickup()
        {
        }

        protected virtual void OnGunHandlePickup(GunHandle instance)
        {
        }

        protected virtual void OnGunHandleUseDown(GunHandle instance)
        {
        }

        protected virtual void OnGunHandleUseUp(GunHandle instance)
        {
        }

        protected virtual void OnGunHandleDrop(GunHandle instance)
        {
        }

        protected virtual void OnGunDrop()
        {
        }

        protected virtual ShotResult CanShoot()
        {
            if (FireMode == FireMode.Safety)
            {
                Trigger = TriggerState.Idle;
                return ShotResult.Failed;
            }

            if (State != GunState.Idle)
            {
                Trigger = TriggerState.Fired;
                return ShotResult.Failed;
            }

            if (DateTime.UtcNow.Subtract(LastShotTime).TotalSeconds < SecondsPerRound)
            {
                return ShotResult.Paused;
            }

            return ShotResult.Succeeded;
        }

        protected virtual void OnShoot(ProjectileBase bullet, bool isPellet)
        {
        }

        protected virtual void OnEmptyShoot()
        {
        }

        protected virtual void OnFireModeChanged(FireMode previous, FireMode next)
        {
            if (previous == FireMode.Safety && next != FireMode.Safety)
            {
                Trigger = TriggerState.Armed;
                return;
            }

            if (previous != FireMode.Safety && next == FireMode.Safety)
            {
                Trigger = TriggerState.Idle;
                return;
            }
        }

        protected virtual void OnProcessStateChange(byte previous, byte next)
        {
            var previousState = GunState.Unknown;
            var nextState = GunState.Unknown;

            if (previous <= GunStateHelper.MaxValue)
                previousState = (GunState)previous;
            if (next <= GunStateHelper.MaxValue)
                nextState = (GunState)next;

            if (nextState == GunState.InCockingTwisting && previousState != GunState.InCockingTwisting)
            {
                if (AudioData != null) Internal_PlayAudio(AudioData.CockingTwist);

                if (TargetAnimator != null && !IsLocal)
                    TargetAnimator.SetFloat(GunUtility.CockingTwistParameter(), 1);
            }
            else if (nextState == GunState.InCockingPush && previousState == GunState.InCockingPull)
            {
                if (AudioData != null) Internal_PlayAudio(AudioData.CockingPull);

                if (TargetAnimator != null && !IsLocal)
                {
                    TargetAnimator.SetFloat(GunUtility.CockingProgressParameter(), 1);
                    TargetAnimator.SetFloat(GunUtility.CockingTwistParameter(), 1);
                }
            }
            else if (nextState == GunState.Idle && previousState != GunState.Idle)
            {
                if (AudioData != null) Internal_PlayAudio(AudioData.CockingRelease);

                if (TargetAnimator != null && !IsLocal)
                {
                    TargetAnimator.SetFloat(GunUtility.CockingProgressParameter(), 0);
                    TargetAnimator.SetFloat(GunUtility.CockingTwistParameter(), 0);
                }
            }
        }

        protected virtual void OnProcessCollisionAudio(Collider other)
        {
            if (AudioData == null || AudioData.Collision == null)
                return;

            if (CollisionCount < 1)
                return;

            var objMarker = other.GetComponent<ObjectMarkerBase>();
            if (objMarker != null && objMarker.Tags.ContainsString("NoCollisionAudio"))
                return;

            var otherName = other.name.ToLower();

            if (otherName.StartsWith("steel") || otherName.EndsWith("steel"))
            {
                Internal_PlayAudio(AudioData.Collision.IronAudio);
            }
            else if (otherName.StartsWith("wood") || otherName.EndsWith("wood"))
            {
                Internal_PlayAudio(AudioData.Collision.WoodAudio);
            }
            else if (otherName.StartsWith("sandbag") || otherName.EndsWith("sandbag"))
            {
                Internal_PlayAudio(AudioData.Collision.ClothAudio);
            }
            else
            {
                Internal_PlayAudio(AudioData.Collision.FallbackAudio);
            }
        }

        #endregion

        #region GunHandleCallback

        public override Vector3 GetHandleIdlePosition(GunHandle instance, HandleType handleType)
        {
            switch (handleType)
            {
                case HandleType.MainHandle:
                    return MainHandlePositionOffset;
                case HandleType.SubHandle:
                    return SubHandlePositionOffset;
                case HandleType.None:
                case HandleType.CustomHandle:
                default:
                    return Vector3.down * 50;
            }
        }

        public override Quaternion GetHandleIdleRotation(GunHandle instance, HandleType handleType)
        {
            switch (handleType)
            {
                case HandleType.MainHandle:
                    return MainHandleRotationOffset;
                case HandleType.SubHandle:
                    return SubHandleRotationOffset;
                case HandleType.None:
                case HandleType.CustomHandle:
                default:
                    return Quaternion.identity;
            }
        }

        public override void OnHandlePickup(GunHandle instance, HandleType handleType)
        {
            var b = Behaviour;
            if (b == null)
            {
                Logger.LogError($"{Prefix}OnHandlePickup: Behaviour is null!");
                instance.ForceDrop();
                return;
            }

            if (MainHandle.IsPickedUp ^
                (SubHandle != null && SubHandle.IsPickedUp) ^
                (CustomHandle != null && CustomHandle.IsPickedUp))
            {
                _isLocal = true;
                Internal_SetRelatedObjectsOwner(Networking.LocalPlayer);
                if (TargetAnimator != null)
                    TargetAnimator.SetBool(GunUtility.IsPickedUpLocallyParameter(), true);
                OnGunPickup();
                b.OnGunPickup(this);
                if (PlayerController != null)
                    PlayerController.AddHoldingObject(this);
            }

            OnGunHandlePickup(instance);
            b.OnHandlePickup(this, instance);
        }

        public override void OnHandleUseDown(GunHandle instance, HandleType handleType)
        {
            var b = Behaviour;
            if (b == null)
            {
                Logger.LogError($"{Prefix}OnHandleTriggerDown: Behaviour is null!");
                return;
            }

            OnGunHandleUseDown(instance);
            b.OnHandleUseDown(this, instance);

            switch (handleType)
            {
                case HandleType.MainHandle:
                {
                    Trigger = TriggerState.Firing;
                    b.OnTriggerDown(this);
                    break;
                }
                case HandleType.SubHandle:
                {
                    b.OnAction(this);
                    break;
                }
            }
        }

        public override void OnHandleUseUp(GunHandle instance, HandleType handleType)
        {
            var b = Behaviour;
            if (b == null)
            {
                Logger.LogError($"{Prefix}OnHandleTriggerUp: Behaviour is null!");
                return;
            }

            OnGunHandleUseUp(instance);
            b.OnHandleUseUp(this, instance);

            switch (handleType)
            {
                case HandleType.MainHandle:
                {
                    if (Trigger == TriggerState.Fired || FireMode.ShouldStopOnTriggerUp())
                        Trigger = TriggerState.Armed;
                    b.OnTriggerUp(this);
                    break;
                }
                case HandleType.SubHandle:
                {
                    b.OnAction(this);
                    break;
                }
            }
        }

        public override void OnHandleDrop(GunHandle instance, HandleType handleType)
        {
            var b = Behaviour;
            if (b == null)
            {
                Logger.LogError($"{Prefix}OnHandleDrop: Behaviour is null!");
                return;
            }

            OnGunHandleDrop(instance);
            b.OnHandleDrop(this, instance);


            switch (handleType)
            {
                case HandleType.MainHandle:
                    _nextMainHandlePickupableTime = Time.time + MainHandleRePickupDelay;
                    break;
                case HandleType.SubHandle:
                    _nextSubHandlePickupableTime = Time.time + SubHandleRePickupDelay;
                    break;
            }

            // When dropped entirely
            if (!(MainHandle.IsPickedUp ||
                  (SubHandle != null && SubHandle.IsPickedUp) ||
                  (CustomHandle != null && CustomHandle.IsPickedUp)))
            {
                OnGunDrop();
                b.OnGunDrop(this);
                if (TargetAnimator != null)
                    TargetAnimator.SetBool(GunUtility.IsPickedUpLocallyParameter(), false);

                if (TargetHolster != null)
                {
                    Internal_SetPivot(HandleType.MainHandle);
                    MainHandle.Holster(TargetHolster);
                    IsHolstered = true;
                    Internal_SetRelatedObjectsOwner(Networking.LocalPlayer);
                    RequestSerialization();
                }

                if (PlayerController != null)
                    PlayerController.RemoveHoldingObject(this);

                _isLocal = false;
            }

            Internal_UpdateIsPickedUpState();
        }

        #endregion
    }

    public enum PositionUpdateMethod
    {
        LookAt,
        MainHandle,
        PivotHandle,
        NotPickedUp
    }
}