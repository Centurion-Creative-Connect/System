using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] [RequireComponent(typeof(GunAnimationHelper), typeof(GunPositioningHelper))]
    public abstract class GunBase : GunHandleCallbackBase
    {
        #region Constants
        protected string Prefix => $"<color=olive>[{WeaponName}]</color> ";
        #endregion

        #region WatchdogProc
        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }
        #endregion

        #region SerializedFields
        [SerializeField] [NewbieInject]
        protected GunManagerBase gunManager;

        [SerializeField] [NewbieInject]
        protected AudioManager audioManager;

        [SerializeField] [NewbieInject]
        protected PrintableBase logger;

        [SerializeField] [NewbieInject]
        protected PlayerController playerController;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        protected GunAnimationHelper animationHelper;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        protected GunPositioningHelper positioningHelper;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private GunHandle mainHandle;

        [SerializeField]
        private GunHandle subHandle;
        #endregion

        #region PrivateFields
        [UdonSynced] [FieldChangeCallback(nameof(SyncedFireModeIndex))]
        private int _fireModeIndex;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedState))]
        private int _state;

        private TriggerState _trigger;
        private bool _isLocal;
        private bool _hasBulletInChamber;
        private bool _hasCocked;
        private int _burstCount;
        private int _collisionCount;
        #endregion

        #region Properties
        [PublicAPI] [CanBeNull] public abstract GunVariantDataStore VariantData { get; }

        [PublicAPI] [NotNull] public Transform Target => target;

        [PublicAPI] [NotNull] public GunHandle MainHandle => mainHandle;
        [PublicAPI] [CanBeNull] public GunHandle SubHandle => subHandle;
        [PublicAPI] public GunAnimationHelper AnimationHelper => animationHelper;
        [PublicAPI] public GunPositioningHelper PositioningHelper => positioningHelper;
        [PublicAPI] [field: UdonSynced] public bool IsHolstered { get; protected set; }

        /// <summary>
        /// Current holder of this Gun.
        /// </summary>
        [PublicAPI]
        public VRCPlayerApi CurrentHolder => MainHandle && Utilities.IsValid(MainHandle.CurrentPlayer)
            ? MainHandle.CurrentPlayer
            : SubHandle && Utilities.IsValid(SubHandle.CurrentPlayer)
                ? SubHandle.CurrentPlayer
                : null;

        public bool IsPickedUp => MainHandle.IsPickedUp || (SubHandle && SubHandle.IsPickedUp);

        [PublicAPI] public GunHolster ActiveHolster { get; protected set; }

        /// <summary>
        /// Is the <see cref="CurrentHolder"/> local?
        /// </summary>
        [PublicAPI]
        public bool IsLocal
        {
            get => _isLocal;
            protected set
            {
                _isLocal = value;
                animationHelper._SetLocal(value);
            }
        }

        /// <summary>
        /// Is the <see cref="CurrentHolder"/> in VR?
        /// </summary>
        [PublicAPI]
        public bool IsVR => Utilities.IsValid(CurrentHolder) && CurrentHolder.IsUserInVR();

        /// <summary>
        /// Currently active <see cref="FireMode"/> of this Gun.
        /// </summary>
        [PublicAPI] public FireMode FireMode => VariantData != null && VariantData.FireModeArray.Length > SyncedFireModeIndex ? VariantData.FireModeArray[SyncedFireModeIndex] : FireMode.Safety;

        [PublicAPI] public float SecondsPerRound => VariantData != null && VariantData.SecondsPerRoundArray.Length > SyncedFireModeIndex ? VariantData.SecondsPerRoundArray[SyncedFireModeIndex] : 0;

        [PublicAPI] public float PerBurstInterval => VariantData != null && VariantData.PerBurstIntervalArray.Length > SyncedFireModeIndex ? VariantData.PerBurstIntervalArray[SyncedFireModeIndex] : 0;

        [PublicAPI] public int CurrentFireModeIndex
        {
            get => SyncedFireModeIndex;
            set
            {
                if (VariantData == null)
                {
                    logger.LogError($"{Prefix}set_CurrentFireModeIndex: Setting index to 0 because VariantData is null.");
                    SyncedFireModeIndex = 0;
                    return;
                }

                if (value >= VariantData.FireModeArray.Length || value < 0)
                {
                    logger.Log($"{Prefix}set_CurrentFireModeIndex: Setting index to 0 because {value} is out of bounds");
                    SyncedFireModeIndex = 0;
                }
                else
                {
                    SyncedFireModeIndex = value;
                }
            }
        }

        private int SyncedFireModeIndex
        {
            get => _fireModeIndex;
            set
            {
                var prev = FireMode;
                _fireModeIndex = value;
                Trigger = FireMode == FireMode.Safety ? TriggerState.Idle : TriggerState.Armed;
                animationHelper._SetSelectorType((int)FireMode);

                if (gunManager && IsLocal && prev != FireMode)
                    gunManager.Invoke_OnFireModeChanged(this);
            }
        }

        [PublicAPI] public TriggerState Trigger
        {
            get => _trigger;
            set
            {
                _trigger = value;

                if (value == TriggerState.Armed || value == TriggerState.Idle)
                    BurstCount = 0;

                animationHelper._SetTriggerState((int)value);
            }
        }

        [PublicAPI] public GunState State
        {
            get => (GunState)_state;
            set => SyncedState = (int)value;
        }

        [PublicAPI] public int SyncedState
        {
            get => _state;
            set
            {
                var prev = State;
                _state = value;
                animationHelper._SetState(_state);

                ProcessStateChange(prev, State);
            }
        }

        /// <summary>
        /// Has the gun bullet in a chamber?
        /// </summary>
        /// <remarks>
        /// This property should be set to <c>false</c> after a bullet was shot by <see cref="_Shoot"/>, <see cref="_TryToShoot"/>.
        /// Load the bullet from a chamber using <see cref="_LoadBullet"/>.
        /// Eject the bullet from a chamber using <see cref="_EjectBullet"/>.
        /// </remarks>
        [PublicAPI]
        public virtual bool HasBulletInChamber
        {
            get => _hasBulletInChamber;
            protected set
            {
                _hasBulletInChamber = value;
                animationHelper._SetHasBullet(value);
            }
        }

        /// <summary>
        /// Has the gun cocked?
        /// </summary>
        /// <remarks>
        /// This property is set to `false` after <see cref="_Shoot"/>, <see cref="_TryToShoot"/>, and <see cref="_EmptyShoot"/>.
        /// Manipulate within <see cref="CenturionCC.System.Gun.Behaviour.GunBehaviourBase"/> 
        /// </remarks>
        [PublicAPI]
        public virtual bool HasCocked
        {
            get => _hasCocked;
            set
            {
                _hasCocked = value;
                animationHelper._SetHasCocked(value);
            }
        }

        public virtual DateTime LastShotTime { get; protected set; }

        public virtual DateTime LastBurstEndedTime { get; protected set; }

        [PublicAPI] [field: UdonSynced]
        public int ShotCount { get; protected set; }

        /// <summary>
        /// Represents how many rounds were shot in one trigger.
        /// </summary>
        /// <remarks>
        /// Counts up on shoot.
        /// Resets when <see cref="GunBase.Trigger" /> is set to <see cref="TriggerState.Idle"/> or <see cref="TriggerState.Armed"/>.
        /// </remarks>
        /// <seealso cref="_TryToShoot" />
        [PublicAPI]
        public int BurstCount { get; protected set; }

        /// <summary>
        /// Counter of currently colliding objects.
        /// </summary>
        /// <remarks>
        /// Counts up on OnTriggerEnter
        /// Counts down on OnTriggerExit
        /// </remarks>
        [PublicAPI]
        public int CollisionCount
        {
            get => _collisionCount;
            protected set
            {
                _collisionCount = value;
                animationHelper._SetIsInWall(IsInWall);
            }
        }

        /// <summary>
        /// Is this Gun inside a wall?
        /// </summary>
        [PublicAPI]
        public virtual bool IsInWall => CollisionCount != 0;
        #endregion

        #region OverridenProperties
        public override ObjectType ObjectType => VariantData ? VariantData.ObjectType : ObjectType.Prototype;

        public override float ObjectWeight => VariantData ? VariantData.ObjectWeight : 0;

        public override string[] Tags => VariantData ? VariantData.Tags : new string[0];

        public override float WalkingSpeedMultiplier => 1;
        #endregion

        #region Aliases
        [PublicAPI] public string WeaponName => VariantData && !string.IsNullOrEmpty(VariantData.WeaponName)
            ? VariantData.WeaponName
            : name;

        [PublicAPI] public ProjectileDataProvider ProjectileData => VariantData ? VariantData.ProjectileData : null;
        [PublicAPI] public ProjectilePoolBase ProjectilePool => VariantData ? VariantData.ProjectilePool : null;
        [PublicAPI] public GunAudioDataStore AudioData => VariantData ? VariantData.AudioData : null;
        [PublicAPI] public GunHapticDataStore HapticData => VariantData ? VariantData.HapticData : null;
        [PublicAPI] public GunCameraDataStore CameraData => VariantData ? VariantData.CameraData : null;

        [PublicAPI] public GunBehaviourBase[] Behaviours =>
            VariantData ? VariantData.Behaviours : new GunBehaviourBase[0];

        [PublicAPI] public Vector3 FiringOffsetPosition =>
            VariantData ? VariantData.FiringPositionOffset : Vector3.zero;

        [PublicAPI] public Quaternion FiringOffsetRotation =>
            VariantData ? VariantData.FiringRotationOffset : Quaternion.identity;

        [PublicAPI] public int HolsterSize => VariantData ? VariantData.HolsterSize : 0;
        [PublicAPI] public bool CanBeTwoHanded => VariantData && VariantData.IsDoubleHanded;
        #endregion

        #region UnityEvents
        protected virtual void Start()
        {
            _Setup();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            var otherName = other.name.ToLower();

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.LogVerbose($"{Prefix}OnTriggerEnter: {otherName}");
#endif

            if (otherName.StartsWith("holster"))
            {
                if (!IsLocal || IsHolstered)
                    return;
                var holster = other.GetComponent<GunHolster>();
                if (holster == null || holster.HoldableSize < HolsterSize)
                    return;

                Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .5F, 1F, .1F);
                ActiveHolster = holster;
                ActiveHolster.IsHighlighting = true;
                return;
            }

            ++CollisionCount;

            ProcessCollisionAudio(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            var otherName = other.name.ToLower();

            if (otherName.StartsWith("holster") && IsLocal)
            {
                if (ActiveHolster != null)
                {
                    Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .5F, 1F, .1F);
                    ActiveHolster.IsHighlighting = false;
                }

                ActiveHolster = null;
                return;
            }

            --CollisionCount;
            // To make sure there aren't negative values on it (might happen when turning off this collider)
            if (CollisionCount < 0)
                CollisionCount = 0;
        }
        #endregion

        #region PublicAPIs
        [PublicAPI]
        public void _GetFiringPositionAndRotation(out Vector3 firingPosition, out Quaternion firingRotation)
        {
            var ltw = Target.localToWorldMatrix;
            firingPosition = ltw.MultiplyPoint3x4(FiringOffsetPosition);
            firingRotation = ltw.rotation * FiringOffsetRotation;
        }

        /// <summary>
        /// Shoots a bullet without any checks.
        /// </summary>
        [PublicAPI]
        public void _Shoot()
        {
            _GetFiringPositionAndRotation(out var firingPos, out var firingRot);
            SendCustomNetworkEvent(
                NetworkEventTarget.All,
                nameof(Internal_NetworkedShoot),
                firingPos,
                firingRot,
                ShotCount++,
                Guid.NewGuid().ToByteArray()
            );
        }

        /// <summary>
        /// If the Gun was able to shoot, will shoot the bullet.
        /// </summary>
        /// <returns><see cref="ShotResult.Succeeded"/> if succeed to shoot, <see cref="ShotResult.Paused"/> if paused, <see cref="ShotResult.Failed"/> if checks were failed.</returns>
        [PublicAPI]
        public ShotResult _TryToShoot()
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
                    _Shoot();
                    ++BurstCount;
                    HasBulletInChamber = false;

                    if (!FireMode.HasFiredEnough(BurstCount))
                        return ShotResult.SucceededContinuously;

                    Trigger = TriggerState.Fired;
                    LastBurstEndedTime = Networking.GetNetworkDateTime();
                    return ShotResult.Succeeded;
                }
                case ShotResult.Failed:
                {
                    _EmptyShoot();

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

        /// <summary>
        /// Plays *click* sound for a failed shot.
        /// </summary>
        [PublicAPI]
        public void _EmptyShoot()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_NetworkedEmptyShoot));
        }

        /// <summary>
        /// Moves it's Gun's position and rotation to a specified location.
        /// </summary>
        /// <param name="position">New world position for this Gun.</param>
        /// <param name="rotation">New world rotation for this Gun.</param>
        [PublicAPI] [NetworkCallable]
        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            _SetOwner(Networking.LocalPlayer);
            MainHandle.ForceDrop();
            if (SubHandle)
                SubHandle.ForceDrop();

            positioningHelper.MoveTo(position, rotation);
        }

        /// <summary>
        /// Does the gun have a next bullet to load into the chamber? 
        /// </summary>
        /// <returns>`true` if the magazine or gun itself can provide the next bullet to shoot, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool _HasNextBullet()
        {
            return true;
        }

        /// <summary>
        /// Tries to load bullet from current magazine.
        /// </summary>
        /// <returns>`true` if successfully loaded bullet on a gun, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool _LoadBullet()
        {
            if (HasBulletInChamber)
                _EjectBullet();
            HasBulletInChamber = true;
            RequestSerialization();
            return HasBulletInChamber;
        }

        /// <summary>
        /// Tries to eject a bullet from the chamber.
        /// </summary>
        [PublicAPI]
        public virtual void _EjectBullet()
        {
            HasBulletInChamber = false;
            RequestSerialization();
        }

        public virtual void _SetHandlesVisible(bool isVisible)
        {
            if (MainHandle)
                MainHandle.IsVisible = isVisible;
            if (SubHandle)
                SubHandle.IsVisible = isVisible;

            // not gonna cache this - this is only used for debugging purposes!
            var gunHandles = GetComponentsInChildren<GunHandle>();
            foreach (var gunHandle in gunHandles)
            {
                gunHandle.IsVisible = isVisible;
            }
        }
        #endregion

        #region Internals
        [PublicAPI]
        public void _ShootLocally(ProjectileDataProvider data, ProjectilePoolBase pool,
                                  Vector3 worldPosition, Quaternion worldRotation,
                                  int shotCount, Guid shotGuid)
        {
            if (data == null)
            {
                logger.LogError($"{Prefix}ProjectileData not found.");
                return;
            }

            if (pool == null)
            {
                logger.LogError($"{Prefix}ProjectilePool not found.");
                return;
            }

            if (!pool.HasInitialized)
            {
                logger.LogError($"{Prefix}ProjectilePool not yet initialized.");
                return;
            }

            LastShotTime = Networking.GetNetworkDateTime();

            var playerId = -1;
            if (NetworkCalling.InNetworkCall)
                playerId = NetworkCalling.CallingPlayer.playerId;
            else if (CurrentHolder != null && Utilities.IsValid(CurrentHolder))
                playerId = CurrentHolder.playerId;

            var dataIndexOffset = shotCount * data.ProjectileCount;

            for (var i = 0; i < data.ProjectileCount; i++)
            {
                data.Get(
                    dataIndexOffset + i,
                    out var posOffset,
                    out var velocity,
                    out var rotOffset,
                    out var torque,
                    out var drag,
                    out var damageAmount,
                    out var trailTime,
                    out var trailGradient,
                    out var lifeTimeInSeconds
                );

                var tempRot = worldRotation * rotOffset;
                var tempPos = worldPosition + (tempRot * posOffset);

                var projectile = pool.Shoot(
                    shotGuid,
                    tempPos,
                    tempRot,
                    velocity,
                    torque,
                    drag,
                    WeaponName,
                    damageAmount,
                    LastShotTime,
                    playerId,
                    trailTime,
                    trailGradient,
                    lifeTimeInSeconds
                );

                if (!projectile)
                {
                    logger.LogError($"{Prefix}Projectile not found!!!");
                    continue;
                }

                if (gunManager)
                    gunManager.Invoke_OnShoot(this, projectile);
            }

            data.GetRecoil(out var recoilPos, out var recoilRot);
            positioningHelper.AddRecoil(recoilPos, recoilRot);

            animationHelper._SetShooting();
            if (AudioData)
                _PlayAudio(AudioData.Shooting, AudioData.ShootingOffset);
            if (IsLocal && HapticData && HapticData.Shooting)
                HapticData.Shooting.PlayBothHand();

            HasCocked = false;
        }

        [NetworkCallable(100)]
        public void Internal_NetworkedShoot(Vector3 worldPosition, Quaternion worldRotation,
                                            int shotCount, byte[] shotId)
        {
            var shotGuid = shotId != null && shotId.Length == 16 ? new Guid(shotId) : Guid.Empty;

            _ShootLocally(
                ProjectileData,
                ProjectilePool,
                worldPosition,
                worldRotation,
                shotCount,
                shotGuid
            );
        }

        [NetworkCallable(100)]
        public void Internal_NetworkedEmptyShoot()
        {
            animationHelper._SetEmptyShooting();
            if (AudioData)
                _PlayAudio(AudioData.EmptyShooting, AudioData.EmptyShootingOffset);
            if (gunManager)
                gunManager.Invoke_OnEmptyShoot(this);
        }

        protected void _PlayAudio(AudioDataStore dataStore, Vector3 offset)
        {
            audioManager.PlayAudioAtTransform(dataStore, transform, offset);
        }

        protected void _SetOwner(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(player, gameObject);
            if (MainHandle && !Networking.IsOwner(MainHandle.gameObject))
                Networking.SetOwner(player, MainHandle.gameObject);
            if (SubHandle && !Networking.IsOwner(SubHandle.gameObject))
                Networking.SetOwner(player, SubHandle.gameObject);
        }

        protected void _UpdatePositioningHelper()
        {
            if (!SubHandle)
            {
                positioningHelper.SetControlAndPivot(ControlType.OneHanded, PivotType.Primary);
                return;
            }

            positioningHelper.SetControlAndPivot(
                MainHandle.IsPickedUp && SubHandle.IsPickedUp ? ControlType.TwoHanded : ControlType.OneHanded,
                !MainHandle.IsPickedUp && SubHandle.IsPickedUp ? PivotType.Secondary : PivotType.Primary
            );
        }

        protected void _Setup()
        {
            // init AnimationHelper
            {
                animationHelper.TargetAnimator = VariantData ? VariantData.Animator : null;
                animationHelper.SyncedParameterNames = VariantData ? VariantData.SyncedAnimatorParameterNames : new string[0];
            }

            // init GunHandle & PositioningHelper
            {
                var isUserInVR = Networking.LocalPlayer.IsUserInVR();
                MainHandle.callback = this;
                MainHandle.handleType = HandleType.MainHandle;
                MainHandle.Detach();
                MainHandle.AdjustScaleForDesktop(isUserInVR);
                if (SubHandle)
                {
                    SubHandle.callback = this;
                    SubHandle.handleType = HandleType.SubHandle;
                    SubHandle.SetPickupable(CanBeTwoHanded && isUserInVR);
                    SubHandle.Detach();
                }


                if (VariantData)
                {
                    MainHandle.UseText = VariantData.TooltipMessage;
                    positioningHelper.SetOffsets(
                        Matrix4x4.TRS(VariantData.MainHandlePositionOffset, VariantData.MainHandleRotationOffset, Vector3.one),
                        Matrix4x4.TRS(VariantData.SubHandlePositionOffset, VariantData.SubHandleRotationOffset, Vector3.one)
                    );

                    positioningHelper.SetRecoilErgonomics(VariantData.Ergonomics);
                }
            }

            SyncedFireModeIndex = 0;
            Trigger = FireMode == FireMode.Safety ? TriggerState.Idle : TriggerState.Armed;
            State = GunState.Idle;
            HasBulletInChamber = false;
            HasCocked = false;
            CollisionCount = 0;

            foreach (var behaviour in Behaviours)
            {
                behaviour.Setup(this);
            }
        }
        #endregion

        #region Callbacks
        public override void OnHandlePickup(GunHandle instance, HandleType handleType)
        {
            var behaviours = Behaviours;
            if (behaviours == null)
            {
                logger.LogError($"{Prefix}OnHandlePickup: Behaviour is null!");
                instance.ForceDrop();
                return;
            }

            if (behaviours.Length == 0)
            {
                logger.LogError($"{Prefix}OnHandlePickup: Behaviour is empty!");
            }

            // OnHandlePickup is only called locally, thus setting isLocal here is appropriate
            IsLocal = true;

            animationHelper._SetPickedUpLocally(true);
            animationHelper._SetTriggerProgress(0);

            // When only one handle is picked up at this time
            if ((MainHandle != null && MainHandle.IsPickedUpLocally) ^
                (SubHandle != null && SubHandle.IsPickedUpLocally))
            {
                if (playerController != null)
                {
                    playerController.AddHoldingObject(this);
                }

                if (gunManager)
                {
                    gunManager.OnGunPickedUpLocally(this);
                }

                animationHelper.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(animationHelper.SetPickedUpGlobally), true);
                animationHelper.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(animationHelper.SetVR), IsVR);

                foreach (var behaviour in behaviours)
                {
                    behaviour.OnGunPickup(this);
                }
            }

            foreach (var behaviour in behaviours)
            {
                behaviour.OnHandlePickup(this, instance);
            }

            _SetOwner(Networking.LocalPlayer);

            _UpdatePositioningHelper();
            RequestSerialization();
        }

        public override void OnHandleUseDown(GunHandle instance, HandleType handleType)
        {
            var behaviours = Behaviours;
            if (behaviours == null)
            {
                logger.LogError($"{Prefix}OnHandleUseDown: Behaviour is null!");
                return;
            }

            if (behaviours.Length == 0)
            {
                logger.LogError($"{Prefix}OnHandleUseDown: Behaviour is empty!");
            }

            foreach (var behaviour in behaviours)
            {
                behaviour.OnHandleUseDown(this, instance);
            }

            switch (handleType)
            {
                case HandleType.MainHandle:
                {
                    Trigger = TriggerState.Firing;
                    foreach (var behaviour in behaviours)
                    {
                        behaviour.OnTriggerDown(this);
                    }

                    break;
                }
                case HandleType.SubHandle:
                {
                    foreach (var behaviour in behaviours)
                    {
                        behaviour.OnAction(this);
                    }

                    break;
                }
            }
        }

        public override void OnHandleUseUp(GunHandle instance, HandleType handleType)
        {
            var behaviours = Behaviours;
            if (behaviours == null)
            {
                logger.LogError($"{Prefix}OnHandleTriggerUp: Behaviour is null!");
                return;
            }

            if (behaviours.Length == 0)
            {
                logger.LogError($"{Prefix}OnHandleTriggerUp: Behaviour is empty!");
            }

            foreach (var behaviour in behaviours)
            {
                behaviour.OnHandleUseUp(this, instance);
            }

            switch (handleType)
            {
                case HandleType.MainHandle:
                {
                    if (Trigger == TriggerState.Fired)
                    {
                        Trigger = TriggerState.Armed;
                    }

                    if (FireMode.ShouldStopOnTriggerUp())
                    {
                        if (BurstCount != 0)
                        {
                            LastBurstEndedTime = Networking.GetNetworkDateTime();
                        }

                        Trigger = TriggerState.Armed;
                    }

                    foreach (var behaviour in behaviours)
                    {
                        behaviour.OnTriggerUp(this);
                    }

                    break;
                }
            }
        }

        public override void OnHandleDrop(GunHandle instance, HandleType handleType)
        {
            var behaviours = Behaviours;
            if (behaviours == null)
            {
                logger.LogError($"{Prefix}OnHandleDrop: Behaviour is null!");
                return;
            }

            if (behaviours.Length == 0)
            {
                logger.LogError($"{Prefix}OnHandleDrop: Behaviour is empty!");
            }

            foreach (var behaviour in behaviours)
            {
                behaviour.OnHandleDrop(this, instance);
            }

            // When dropped entirely for local player
            var mainDroppedLocally = MainHandle == null || !MainHandle.IsPickedUpLocally;
            var subDroppedLocally = SubHandle == null || !SubHandle.IsPickedUpLocally;
            var droppedLocally = mainDroppedLocally && subDroppedLocally;
            if (droppedLocally)
            {
                IsLocal = false;

                animationHelper._SetPickedUpLocally(false);
                animationHelper._SetTriggerProgress(0);

                // When dropped entirely
                var mainDropped = MainHandle == null || !MainHandle.IsPickedUp;
                var subDropped = SubHandle == null || !SubHandle.IsPickedUp;
                var dropped = mainDropped && subDropped;
                if (dropped)
                {
                    animationHelper.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(animationHelper.SetPickedUpGlobally), false);
                }

                if (playerController != null)
                {
                    playerController.RemoveHoldingObject(this);
                }

                if (gunManager)
                    gunManager.OnGunDroppedLocally(this);

                foreach (var behaviour in behaviours)
                {
                    behaviour.OnGunDrop(this);
                }

                if (ActiveHolster != null)
                {
                    MainHandle.Holster(ActiveHolster);
                    IsHolstered = true;
                }
            }

            _SetOwner(Networking.LocalPlayer);
            _UpdatePositioningHelper();
            RequestSerialization();
        }

        protected ShotResult CanShoot()
        {
            if (!VariantData)
            {
                if (gunManager)
                    gunManager.Invoke_OnShootCancelled(this, 1);
                return ShotResult.Cancelled;
            }

            if (FireMode == FireMode.Safety)
            {
                if (gunManager)
                    gunManager.Invoke_OnShootFailed(this, 12);
                return ShotResult.Failed;
            }

            if (IsInWall && VariantData.UseWallCheck && (!gunManager || gunManager.UseCollisionCheck))
            {
                if (gunManager)
                    gunManager.Invoke_OnShootFailed(this, 100);
                return ShotResult.Failed;
            }

            if (gunManager && !gunManager.CanShoot(this, out var reasonId))
            {
                gunManager.Invoke_OnShootCancelled(this, reasonId);
                return ShotResult.Cancelled;
            }

            if (State != GunState.Idle)
            {
                Trigger = TriggerState.Fired;
                return ShotResult.Failed;
            }

            var now = Networking.GetNetworkDateTime();
            if (now.Subtract(LastShotTime).TotalSeconds < SecondsPerRound ||
                now.Subtract(LastBurstEndedTime).TotalSeconds < PerBurstInterval)
            {
                return ShotResult.Paused;
            }

            return ShotResult.Succeeded;
        }

        private void ProcessStateChange(GunState previousState, GunState nextState)
        {
            if (nextState == GunState.InCockingTwisting && previousState != GunState.InCockingTwisting)
            {
                if (AudioData)
                {
                    if (!AudioData.UseSecondTwistAudio)
                    {
                        _PlayAudio(AudioData.CockingTwist, AudioData.CockingTwistOffset);
                    }
                    else
                    {
                        if (previousState == GunState.InCockingPush || previousState == GunState.InCockingPull)
                        {
                            _PlayAudio(AudioData.CockingSecondTwist, AudioData.CockingSecondTwistOffset);
                        }
                        else
                        {
                            _PlayAudio(AudioData.CockingTwist, AudioData.CockingTwistOffset);
                        }
                    }
                }

                if (!IsLocal)
                    animationHelper._SetTwistingProgress(1);
            }
            else if (nextState == GunState.InCockingPush && previousState == GunState.InCockingPull)
            {
                if (AudioData) _PlayAudio(AudioData.CockingPull, AudioData.CockingPullOffset);

                if (!IsLocal)
                {
                    animationHelper._SetCockingProgress(1);
                    animationHelper._SetTwistingProgress(1);
                }
            }
            else if (nextState == GunState.Idle && previousState != GunState.Idle)
            {
                if (AudioData) _PlayAudio(AudioData.CockingRelease, AudioData.CockingReleaseOffset);

                if (!IsLocal)
                {
                    animationHelper._SetCockingProgress(0);
                    animationHelper._SetTwistingProgress(0);
                }
            }
        }

        private void ProcessCollisionAudio(Collider other)
        {
            if (!AudioData || !AudioData.Collision)
                return;

            if (CollisionCount > 1)
                return;

            var objMarker = other.GetComponent<ObjectMarkerBase>();
            if (objMarker == null || objMarker.Tags.ContainsString("NoCollisionAudio"))
                return;

            _PlayAudio(AudioData.Collision.Get(objMarker.ObjectType), Vector3.zero);
        }
        #endregion
    }
}
