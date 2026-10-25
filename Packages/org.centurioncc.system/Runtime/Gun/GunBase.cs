using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.PlayerLocomotion;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] [RequireComponent(typeof(GunAnimationHelper), typeof(GunPositioningHelper), typeof(GunReloadHelper))]
    public abstract class GunBase : GunHandleCallbackBase
    {
        #region Constants
        protected string Prefix => $"[<color=orange>Gun-{WeaponName}</color>] ";
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

        [SerializeField] [NewbieInject(SearchScope.Self)]
        protected GunReloadHelper reloadHelper;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private GunHandle mainHandle;

        [SerializeField]
        private GunHandle subHandle;
        #endregion

        #region PrivateFields
        [UdonSynced] [FieldChangeCallback(nameof(SyncedState))]
        private int _state;

        [UdonSynced] [FieldChangeCallback(nameof(BulletsInMagazine))]
        private int _bulletsInMagazine;

        private TriggerState _trigger;
        private bool _hasBulletInChamber;
        private bool _hasCocked;
        private bool _isLocal;
        private bool _isLocalUserInVR;
        private int _burstCount;
        private int _collisionCount;
        private int _fireModeIndex;
        private bool _isHeldLocally;
        private bool _positionedMainHeld;
        private bool _positionedSubHeld;
        private readonly DataList _highlightingHolster = new DataList();
        #endregion

        #region Properties
        [PublicAPI] [CanBeNull] public abstract GunVariantDataStore VariantData { get; }

        [PublicAPI] [NotNull] public Transform Target => target;

        [PublicAPI] [NotNull] public GunHandle MainHandle => mainHandle;
        [PublicAPI] [CanBeNull] public GunHandle SubHandle => subHandle;
        [PublicAPI] public GunAnimationHelper AnimationHelper => animationHelper;
        [PublicAPI] public GunPositioningHelper PositioningHelper => positioningHelper;
        [PublicAPI] public GunReloadHelper ReloadHelper => reloadHelper;
        [PublicAPI] [field: UdonSynced] public bool IsHolstered { get; protected set; }

        /// <summary>
        /// Current holder of this Gun.
        /// </summary>
        [PublicAPI]
        public VRCPlayerApi CurrentHolder => MainHandle && Utilities.IsValid(MainHandle.CurrentPlayer)
            ? MainHandle.CurrentPlayer
            : SubHandle != null && Utilities.IsValid(SubHandle.CurrentPlayer)
                ? SubHandle.CurrentPlayer
                : null;

        public bool IsPickedUp => MainHandle.IsPickedUp || (SubHandle != null && SubHandle.IsPickedUp);

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
        /// <remarks>
        /// Local holder is always the local player, so skip resolving <see cref="CurrentHolder"/> in the per-frame local paths.
        /// </remarks>
        [PublicAPI]
        public bool IsVR => IsLocal ? _isLocalUserInVR : Utilities.IsValid(CurrentHolder) && CurrentHolder.IsUserInVR();

        /// <summary>
        /// Currently active <see cref="FireMode"/> of this Gun.
        /// </summary>
        [PublicAPI] public FireMode FireMode => VariantData != null && VariantData.FireModeArray.Length > FireModeIndex ? VariantData.FireModeArray[FireModeIndex] : FireMode.Safety;

        [PublicAPI] public float SecondsPerRound => VariantData != null && VariantData.SecondsPerRoundArray.Length > FireModeIndex ? VariantData.SecondsPerRoundArray[FireModeIndex] : 0;

        [PublicAPI] public float PerBurstInterval => VariantData != null && VariantData.PerBurstIntervalArray.Length > FireModeIndex ? VariantData.PerBurstIntervalArray[FireModeIndex] : 0;

        [PublicAPI] public int CurrentFireModeIndex
        {
            get => FireModeIndex;
            set
            {
                if (VariantData == null)
                {
                    logger.LogError($"{Prefix}set_CurrentFireModeIndex: Setting index to 0 because VariantData is null.");
                    FireModeIndex = 0;
                    return;
                }

                if (value >= VariantData.FireModeArray.Length || value < 0)
                {
                    logger.Log($"{Prefix}set_CurrentFireModeIndex: Setting index to 0 because {value} is out of bounds");
                    FireModeIndex = 0;
                }
                else
                {
                    FireModeIndex = value;
                }
            }
        }

        private int FireModeIndex
        {
            get => _fireModeIndex;
            set
            {
                var prev = FireMode;
                _fireModeIndex = value;
                Trigger = FireMode == FireMode.Safety ? TriggerState.Idle : TriggerState.Armed;
                animationHelper._SetSelectorType((int)FireMode);

                if (gunManager && IsLocal && prev != FireMode)
                    gunManager.Event.Invoke_OnFireModeChanged(this);
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
        public bool HasCocked
        {
            get => _hasCocked;
            set
            {
                _hasCocked = value;
                animationHelper._SetHasCocked(value);
            }
        }

        [PublicAPI]
        public int BulletsInMagazine
        {
            get => ReloadType == ReloadType.None ? 1 : _bulletsInMagazine;
            set
            {
                _bulletsInMagazine = value;
                animationHelper._SetBulletsInMagazine(value);
            }
        }

        public DateTime LastShotTime { get; protected set; }

        public DateTime LastBurstEndedTime { get; protected set; }

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
        // checked
        // ReSharper disable PossibleNullReferenceException
        public override ObjectType ObjectType => VariantData ? VariantData.ObjectType : ObjectType.Prototype;

        public override float ObjectWeight => VariantData ? VariantData.ObjectWeight : 0;

        public override string[] Tags => VariantData ? VariantData.Tags : new string[0];
        // ReSharper restore PossibleNullReferenceException

        public override float WalkingSpeedMultiplier => 1;
        #endregion

        #region Aliases
        // checked
        // ReSharper disable PossibleNullReferenceException
        [PublicAPI] public string WeaponName => VariantData && !string.IsNullOrEmpty(VariantData.WeaponName)
            ? VariantData.WeaponName
            : name;

        [PublicAPI] [CanBeNull] public ProjectileDataProvider ProjectileData => VariantData ? VariantData.ProjectileData : null;
        [PublicAPI] [CanBeNull] public ProjectilePoolBase ProjectilePool => VariantData ? VariantData.ProjectilePool : null;
        [PublicAPI] [CanBeNull] public GunAudioDataStore AudioData => VariantData ? VariantData.AudioData : null;
        [PublicAPI] [CanBeNull] public GunHapticDataStore HapticData => VariantData ? VariantData.HapticData : null;
        [PublicAPI] [CanBeNull] public GunCameraDataStore CameraData => VariantData ? VariantData.CameraData : null;

        [PublicAPI] [ItemCanBeNull] public GunBehaviourBase[] Behaviours =>
            VariantData ? VariantData.Behaviours : new GunBehaviourBase[0];

        [PublicAPI] public Vector3 FiringOffsetPosition =>
            VariantData ? VariantData.FiringPositionOffset : Vector3.zero;

        [PublicAPI] public Quaternion FiringOffsetRotation =>
            VariantData ? VariantData.FiringRotationOffset : Quaternion.identity;

        [PublicAPI] public int HolsterSize => VariantData ? VariantData.HolsterSize : 0;
        [PublicAPI] public bool CanBeTwoHanded => VariantData && VariantData.IsDoubleHanded;
        [PublicAPI] public ReloadType ReloadType => VariantData ? VariantData.ReloadType : ReloadType.None;
        [PublicAPI] public float ReloadTimeInSeconds => VariantData ? VariantData.ReloadTimeInSeconds : 0;
        [PublicAPI] public int DefaultMagazineSize => VariantData ? VariantData.DefaultMagazineSize : 0;
        // ReSharper restore PossibleNullReferenceException
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
                // do not add holsters as collision count
                if (!IsLocal || IsHolstered) return;

                var holster = other.GetComponent<GunHolster>();
                if (holster == null) return;

                Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .5F, 1F, .1F);
                _AddHighlightingHolster(holster);
                return;
            }

            ++CollisionCount;

            ProcessCollisionAudio(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            var otherName = other.name.ToLower();

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.LogVerbose($"{Prefix}OnTriggerExit: {otherName}");
#endif

            if (otherName.StartsWith("holster"))
            {
                // do not add holsters as collision count
                if (!IsLocal) return;

                var holster = other.GetComponent<GunHolster>();
                if (holster == null) return;

                _RemoveHighlightingHolster(holster);

                if (_GetActiveHolsters().Length == 0)
                {
                    Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .5F, 1F, .1F);
                }
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

        [PublicAPI("1.1.0")]
        public GunHolster[] _GetActiveHolsters()
        {
            var result = new GunHolster[_highlightingHolster.Count];
            var tokenArr = _highlightingHolster.ToArray();
            for (var i = 0; i < tokenArr.Length; i++)
            {
                result[i] = (GunHolster)tokenArr[i].Reference;
            }

            return result;
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
            if (!VariantData)
                return _CancelShot(ShotReasonId.NoVariantData);

            if (FireMode == FireMode.Safety)
                return _CancelShot(ShotReasonId.SelectorSafety);

            if (!HasBulletInChamber)
                return HasCocked ? _FailShot(ShotReasonId.NoBulletInChamber) : _CancelShot(ShotReasonId.NoBulletInChamber);

            // ReSharper disable once PossibleNullReferenceException
            if (IsInWall && VariantData.UseWallCheck && (!gunManager || gunManager.UseCollisionCheck))
                return _CancelShot(ShotReasonId.InWall);

            if (gunManager && !gunManager.CanShoot(this, out var reasonId))
                return _CancelShot(reasonId);

            if (State != GunState.Idle)
                return _FailShot(ShotReasonId.StateNotIdle);

            if (!ReloadHelper.HasMagazine && !VariantData.CanShootWithoutMagazine)
                return HasCocked ? _FailShot(ShotReasonId.NoMagazine) : _CancelShot(ShotReasonId.NoMagazine);

            var now = Networking.GetNetworkDateTime();
            if (now.Subtract(LastShotTime).TotalSeconds < SecondsPerRound ||
                now.Subtract(LastBurstEndedTime).TotalSeconds < PerBurstInterval)
            {
                return ShotResult.Paused;
            }

            _Shoot();
            ++BurstCount;

            if (!FireMode.HasFiredEnough(BurstCount))
            {
                return ShotResult.SucceededContinuously;
            }

            Trigger = TriggerState.Fired;
            LastBurstEndedTime = Networking.GetNetworkDateTime();
            return ShotResult.Succeeded;
        }

        /// <summary>
        /// Plays *click* sound for a failed shot.
        /// </summary>
        [PublicAPI]
        public void _EmptyShoot()
        {
            if (!HasCocked)
            {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
                Debug.Log($"{Prefix}_EmptyShoot: will not invoke because its not cocked");
#endif
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_NetworkedEmptyShoot));
        }

        /// <summary>
        /// Ends the trigger pull without any sound.
        /// </summary>
        private ShotResult _CancelShot(int reasonId)
        {
            Trigger = TriggerState.Fired;

            if (gunManager) gunManager.Event.Invoke_OnShootCancelled(this, reasonId);
            return ShotResult.Cancelled;
        }

        /// <summary>
        /// Ends the trigger pull with a *click*.
        /// </summary>
        private ShotResult _FailShot(int reasonId)
        {
            Trigger = TriggerState.Fired;

            _EmptyShoot();
            if (gunManager) gunManager.Event.Invoke_OnShootFailed(this, reasonId);
            return ShotResult.Failed;
        }

        /// <summary>
        /// Moves it's Gun's position and rotation to a specified location.
        /// </summary>
        /// <param name="position">New world position for this Gun.</param>
        /// <param name="rotation">New world rotation for this Gun.</param>
        [PublicAPI]
        [NetworkCallable]
        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            _SetOwner(Networking.LocalPlayer);
            MainHandle.ForceDrop();
            if (SubHandle != null)
                SubHandle.ForceDrop();

            positioningHelper.MoveTo(position, rotation);
        }

        /// <summary>
        /// Calls custom event on all behaviours. Network callable.
        /// </summary>
        /// <param name="eventName"></param>
        [PublicAPI]
        [NetworkCallable(100)]
        public void SendGunBehaviourEvent(string eventName)
        {
            foreach (var behaviour in Behaviours)
            {
                if (behaviour != null)
                {
                    behaviour.SendCustomEvent(eventName);
                }
            }
        }

        [PublicAPI]
        public void _SendNetworkGunBehaviourEvent(NetworkEventTarget networkEventTarget, string eventName)
        {
            SendCustomNetworkEvent(networkEventTarget, nameof(SendGunBehaviourEvent), eventName);
        }

        /// <summary>
        /// Does the gun have a next bullet to load into the chamber? 
        /// </summary>
        /// <returns>`true` if the magazine or gun itself can provide the next bullet to shoot, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool _HasNextBullet()
        {
            return ReloadType == ReloadType.None || BulletsInMagazine > 0;
        }

        /// <summary>
        /// Tries to load bullet from current magazine.
        /// </summary>
        /// <returns>`true` if successfully loaded bullet on a gun, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool _LoadBullet()
        {
            if (_HasNextBullet())
            {
                if (ReloadType == ReloadType.None)
                {
                    HasBulletInChamber = true;
                }
                else
                {
                    --BulletsInMagazine;
                    HasBulletInChamber = BulletsInMagazine >= 0;
                    if (!HasBulletInChamber) BulletsInMagazine = 0;
                }
            }

            return HasBulletInChamber;
        }

        /// <summary>
        /// Tries to eject a bullet from the chamber.
        /// </summary>
        [PublicAPI]
        public virtual void _EjectBullet()
        {
            HasBulletInChamber = false;
        }

        public virtual void _SetHandlesVisible(bool isVisible)
        {
            MainHandle.IsVisible = isVisible;
            if (SubHandle != null)
                SubHandle.IsVisible = isVisible;

            // not gonna cache this - this is only used for debugging purposes!
            var gunHandles = GetComponentsInChildren<GunHandle>();
            foreach (var gunHandle in gunHandles)
            {
                gunHandle.IsVisible = isVisible;
            }
        }

        [PublicAPI]
        public void _RequestSync()
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log($"[GunBase-{name}] _RequestSync");
#endif
            _SetOwner(Networking.LocalPlayer);
            RequestSerialization();
        }
        #endregion

        #region Internals
        [PublicAPI]
        public void _ShootLocally(ProjectileDataProvider data, ProjectilePoolBase pool,
                                  Vector3 worldPosition, Quaternion worldRotation,
                                  int shotCount, Guid shotGuid, bool consumeBullet = true)
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
                    CenturionDiagnostic.LogWarning($"{Prefix}Projectile not found!!!");
                    continue;
                }

                projectile.SetProjectileDataProvider(data);

                if (gunManager)
                {
                    gunManager.Event.Invoke_OnShoot(this, projectile);
                }
            }

            data.GetRecoil(out var recoilPos, out var recoilRot);
            positioningHelper.AddRecoil(recoilPos, recoilRot);

            animationHelper._SetShooting();
            if (AudioData)
                _PlayAudio(AudioData.Shooting, AudioData.ShootingOffset);
            if (IsLocal && HapticData && HapticData.Shooting)
                HapticData.Shooting.PlayBothHand();

            HasCocked = false;
            _EjectBullet();
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

            HasCocked = false;

            if (gunManager)
                gunManager.Event.Invoke_OnEmptyShoot(this);
        }

        public void _PlayAudio(AudioDataStore dataStore, Vector3 offset)
        {
            audioManager.PlayAudioAtTransform(dataStore, transform, offset);
        }

        protected void _SetOwner(VRCPlayerApi player)
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(player, gameObject);
            // Taking ownership of a handle someone else is holding desyncs their hand from the handle
            if (!Networking.IsOwner(MainHandle.gameObject) && !MainHandle.IsPickedUpByOthers)
                Networking.SetOwner(player, MainHandle.gameObject);
            if (SubHandle != null && !Networking.IsOwner(SubHandle.gameObject) && !SubHandle.IsPickedUpByOthers)
                Networking.SetOwner(player, SubHandle.gameObject);
        }

        protected void _UpdatePositioningHelper()
        {
            var mainHeld = _IsHandleHeld(MainHandle);
            var subHeld = _IsHandleHeld(SubHandle);
            _positionedMainHeld = mainHeld;
            _positionedSubHeld = subHeld;

            if (SubHandle == null)
            {
                positioningHelper.SetControlAndPivot(ControlType.OneHanded, PivotType.Primary);
                return;
            }

            positioningHelper.SetControlAndPivot(
                mainHeld && subHeld ? ControlType.TwoHanded : mainHeld || subHeld || IsHolstered ? ControlType.OneHanded : ControlType.None,
                !mainHeld && subHeld ? PivotType.Secondary : PivotType.Primary
            );
        }

        protected void _Setup()
        {
            // init AnimationHelper
            {
                animationHelper.TargetAnimator = VariantData != null ? VariantData.Animator : null;
                animationHelper.SyncedParameterNames = VariantData != null ? VariantData.SyncedAnimatorParameterNames : new string[0];
            }

            // init GunHandle & PositioningHelper
            {
                var isUserInVR = Networking.LocalPlayer.IsUserInVR();
                _isLocalUserInVR = isUserInVR;
                MainHandle.callback = this;
                MainHandle.handleType = HandleType.MainHandle;
                MainHandle.Detach();
                MainHandle.AdjustScaleForDesktop(isUserInVR);

                if (SubHandle != null)
                {
                    SubHandle.callback = this;
                    SubHandle.handleType = HandleType.SubHandle;
                    SubHandle.SetPickupable(CanBeTwoHanded);
                    SubHandle.Detach();
                }


                if (VariantData != null)
                {
                    MainHandle.UseText = VariantData.TooltipMessage;
                    positioningHelper.SetOffsets(
                        Matrix4x4.TRS(VariantData.MainHandlePositionOffset, VariantData.MainHandleRotationOffset, Vector3.one),
                        Matrix4x4.TRS(VariantData.SubHandlePositionOffset, VariantData.SubHandleRotationOffset, Vector3.one)
                    );

                    positioningHelper.SetRecoilErgonomics(VariantData.Ergonomics);
                    positioningHelper.SetGravity(VariantData.UseGravity);
                }
            }

            FireModeIndex = 0;
            Trigger = FireMode == FireMode.Safety ? TriggerState.Idle : TriggerState.Armed;
            State = GunState.Idle;
            HasBulletInChamber = false;
            HasCocked = false;
            BulletsInMagazine = 0;
            CollisionCount = 0;

            _NotifySetup(Behaviours);
        }

        protected bool _AddHighlightingHolster(GunHolster holster)
        {
            if (holster == null) return false;

            if (holster.HoldableSize < HolsterSize) return false;

            holster.AddHighlightingObject(this);

            _highlightingHolster.Add(holster);
            return true;
        }

        protected bool _RemoveHighlightingHolster(GunHolster holster)
        {
            if (holster == null) return false;

            holster.RemoveHighlightingObject(this);

            return _highlightingHolster.RemoveAll(holster);
        }

        protected void _MakeHolstered(GunHolster holster)
        {
            if (holster == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}_MakeHolstered: target holster is null");
                return;
            }

            if (!IsLocal)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}_MakeHolstered: tried to make non-local holstered");
                return;
            }

            if (IsHolstered)
            {
                _MakeUnholstered();
            }

            holster.AddHolsteredObject(this);
            _RemoveHighlightingHolster(holster);

            MainHandle.Holster(holster);
            ActiveHolster = holster;
            IsHolstered = true;

            _UpdatePositioningHelper();
            _RequestSync();
        }

        protected void _MakeUnholstered()
        {
            if (!IsHolstered) return;

            if (!IsLocal)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}_MakeUnholstered: Non-local tried to make gun unholstered");
                return;
            }

            _ClearLocalHolsterState();
            IsHolstered = false;

            _UpdatePositioningHelper();
            _RequestSync();
        }

        /// <summary>
        /// Local player is in control of this gun when holding MainHandle, or holding SubHandle while no one else holds MainHandle.
        /// Holding only SubHandle while someone else holds MainHandle makes local player a supporting holder.
        /// </summary>
        protected bool ShouldBeLocalHolder => MainHandle.IsPickedUpLocally ||
                                              (SubHandle != null && SubHandle.IsPickedUpLocally && !MainHandle.IsPickedUpByOthers);

        protected bool IsHeldByOthers => MainHandle.IsPickedUpByOthers || (SubHandle != null && SubHandle.IsPickedUpByOthers);

        private bool IsHeldLocally => MainHandle.IsPickedUpLocally || (SubHandle != null && SubHandle.IsPickedUpLocally);

        /// <summary>
        /// Brings local holder state in line with who is currently holding which handle. Safe to call any number of times.
        /// </summary>
        /// <param name="forceSync">
        /// Reposition and sync even when nothing changed, e.g. after a local handle event.
        /// Otherwise positioning is only refreshed when held handles changed, and sync only on local holder change.
        /// </param>
        protected void _RefreshHolders(bool forceSync)
        {
            var shouldBeLocal = ShouldBeLocalHolder;
            var hasLocalHolderChanged = shouldBeLocal != IsLocal;
            if (hasLocalHolderChanged)
            {
                if (shouldBeLocal) _BecomeLocalHolder();
                else _ResignLocalHolder();
            }

            // Local holder owns positioning and syncing. After letting go, sync the final state once,
            // unless someone else still holds a handle, in which case they are in control now
            var isInControl = IsLocal || (hasLocalHolderChanged && !IsHeldByOthers);
            if (isInControl)
            {
                if (hasLocalHolderChanged || forceSync)
                {
                    _UpdatePositioningHelper();
                    _RequestSync();
                }
                else if (_HasHeldStateChanged())
                {
                    _UpdatePositioningHelper();
                }
            }

            _RefreshHeldLocally();
        }

        private void _BecomeLocalHolder()
        {
            IsLocal = true;

            _MakeUnholstered();

            animationHelper._SetPickedUpLocally(true);
            animationHelper._SetTriggerProgress(0);

            if (playerController != null)
            {
                playerController.AddMarker(this);
            }

            animationHelper.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(animationHelper.SetPickedUpGlobally), true);
            animationHelper.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(animationHelper.SetVR), IsVR);

            var behaviours = Behaviours;
            _NotifyGunPickup(behaviours);

            // Interleaved per behaviour on purpose, so each behaviour sees MainHandle before SubHandle
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null) continue;
                if (MainHandle.IsPickedUpLocally) behaviour.OnHandlePickup(this, MainHandle);
                if (SubHandle != null && SubHandle.IsPickedUpLocally) behaviour.OnHandlePickup(this, SubHandle);
            }
        }

        private void _ResignLocalHolder()
        {
            var isHeldByOthers = IsHeldByOthers;

            // Do not holster a gun someone else is still holding
            if (!isHeldByOthers)
            {
                var activeHolsters = _GetActiveHolsters();
                if (activeHolsters.Length != 0)
                {
                    _MakeHolstered(activeHolsters[activeHolsters.Length - 1]);
                }
            }

            IsLocal = false;

            animationHelper._SetPickedUpLocally(false);
            animationHelper._SetTriggerProgress(0);

            if (!isHeldByOthers)
            {
                animationHelper.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(animationHelper.SetPickedUpGlobally), false);
            }

            if (playerController != null)
            {
                playerController.RemoveMarker(this);
            }

            _NotifyGunDrop(Behaviours);
        }

        /// <summary>
        /// Notifies GunManager when local player starts or stops holding any handle, including as a supporting holder.
        /// </summary>
        private void _RefreshHeldLocally()
        {
            var isHeldLocally = IsHeldLocally;
            if (isHeldLocally == _isHeldLocally) return;

            _isHeldLocally = isHeldLocally;
            if (!gunManager) return;

            if (isHeldLocally) gunManager.OnGunPickedUpLocally(this);
            else gunManager.OnGunDroppedLocally(this);
        }

        /// <summary>
        /// Remote players picking up or dropping handles are not notified locally, so poll for it while holding this gun.
        /// </summary>
        /// <remarks>Called every frame by <see cref="GunController"/> while held locally.</remarks>
        public void _PollHolders()
        {
            _RefreshHolders(false);
        }

        /// <summary>
        /// Has any handle been picked up or dropped since positioning was last applied?
        /// </summary>
        private bool _HasHeldStateChanged()
        {
            return _positionedMainHeld != _IsHandleHeld(MainHandle) || _positionedSubHeld != _IsHandleHeld(SubHandle);
        }

        /// <summary>
        /// Is the handle held by anyone?
        /// </summary>
        /// <remarks>
        /// <see cref="GunHandle.IsPickedUp"/> may still be true for the handle being dropped during OnDrop, thus check holder explicitly.
        /// </remarks>
        private static bool _IsHandleHeld(GunHandle handle)
        {
            return handle != null && (handle.IsPickedUpLocally || handle.IsPickedUpByOthers);
        }
        #endregion

        #region BehaviourDispatch
        // Udon has no delegates, so each GunBehaviourBase event gets its own null-skipping loop
        private void _NotifySetup(GunBehaviourBase[] behaviours)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.Setup(this);
        }

        private void _NotifyGunPickup(GunBehaviourBase[] behaviours)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnGunPickup(this);
        }

        private void _NotifyGunDrop(GunBehaviourBase[] behaviours)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnGunDrop(this);
        }

        private void _NotifyHandlePickup(GunBehaviourBase[] behaviours, GunHandle handle)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnHandlePickup(this, handle);
        }

        private void _NotifyHandleDrop(GunBehaviourBase[] behaviours, GunHandle handle)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnHandleDrop(this, handle);
        }

        private void _NotifyHandleUseDown(GunBehaviourBase[] behaviours, GunHandle handle)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnHandleUseDown(this, handle);
        }

        private void _NotifyHandleUseUp(GunBehaviourBase[] behaviours, GunHandle handle)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnHandleUseUp(this, handle);
        }

        private void _NotifyTriggerDown(GunBehaviourBase[] behaviours)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnTriggerDown(this);
        }

        private void _NotifyTriggerUp(GunBehaviourBase[] behaviours)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnTriggerUp(this);
        }

        private void _NotifyAction(GunBehaviourBase[] behaviours)
        {
            foreach (var behaviour in behaviours)
                if (behaviour != null)
                    behaviour.OnAction(this);
        }
        #endregion

        #region Callbacks
        public override void OnHandlePickup(GunHandle instance, HandleType handleType)
        {
            var behaviours = Behaviours;
            if (behaviours == null || behaviours.Length == 0)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}OnHandlePickup: Behaviour is null or empty!");
                instance.ForceDrop();
                return;
            }

            // Only the local holder may grab custom handles (e.g. cocking handle)
            if (handleType == HandleType.CustomHandle && !IsLocal)
            {
                instance.ForceDrop();
                return;
            }

            // When not yet local, becoming local holder calls OnHandlePickup for every locally held handle, including this one
            if (IsLocal) _NotifyHandlePickup(behaviours, instance);

            _RefreshHolders(true);
        }

        public override void OnHandleUseDown(GunHandle instance, HandleType handleType)
        {
            // Supporting holder cannot operate the gun
            if (!IsLocal) return;

            var behaviours = Behaviours;
            if (behaviours == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}OnHandleUseDown: Behaviour is null!");
                return;
            }

            if (behaviours.Length == 0)
            {
                logger.LogError($"{Prefix}OnHandleUseDown: Behaviour is empty!");
            }

            _NotifyHandleUseDown(behaviours, instance);

            switch (handleType)
            {
                case HandleType.MainHandle:
                    Trigger = TriggerState.Firing;
                    _NotifyTriggerDown(behaviours);
                    break;
                case HandleType.SubHandle:
                    _NotifyAction(behaviours);
                    break;
            }
        }

        public override void OnHandleUseUp(GunHandle instance, HandleType handleType)
        {
            if (!IsLocal) return;

            var behaviours = Behaviours;
            if (behaviours == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}OnHandleTriggerUp: Behaviour is null!");
                return;
            }

            if (behaviours.Length == 0)
            {
                logger.LogError($"{Prefix}OnHandleTriggerUp: Behaviour is empty!");
            }

            _NotifyHandleUseUp(behaviours, instance);

            if (handleType != HandleType.MainHandle) return;

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

            _NotifyTriggerUp(behaviours);
        }

        public override void OnHandleDrop(GunHandle instance, HandleType handleType)
        {
            var behaviours = Behaviours;
            if (behaviours == null || behaviours.Length == 0)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}OnHandleDrop: Behaviour is null or empty!");
                return;
            }

            // Supporting holder never received OnHandlePickup, so there is nothing to clean up
            if (IsLocal) _NotifyHandleDrop(behaviours, instance);

            _RefreshHolders(true);
        }

        /// <summary>
        /// <see cref="GunHolsterCallback.OnHolsterDeactivated"/>
        /// </summary>
        /// <param name="holster"></param>
        public void OnHolsterDeactivated(GunHolster holster)
        {
            if (ActiveHolster == holster)
            {
                _MakeUnholstered();
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            // Local holder must stay owner, e.g. after both players grabbed a free gun at the same time
            if (!IsLocal || Networking.IsOwner(gameObject)) return;

            _UpdatePositioningHelper();
            _RequestSync();
        }

        public override void OnDeserialization()
        {
            // Holstering is applied locally only. When someone else takes the gun out of our holster,
            // only the synced IsHolstered flips, so clean up the leftover local holster state here.
            if (!IsHolstered && (ActiveHolster != null || MainHandle.IsHolstered))
            {
                _ClearLocalHolsterState();
            }
        }

        private void _ClearLocalHolsterState()
        {
            if (ActiveHolster) ActiveHolster.RemoveHolsteredObject(this);
            MainHandle.UnHolster();
            ActiveHolster = null;
        }

        private void ProcessStateChange(GunState previousState, GunState nextState)
        {
            if (previousState == nextState)
            {
                return;
            }

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log($"[GunBase-{name}] State changed from {previousState.GetStateString()} to {nextState.GetStateString()}.");
#endif

            switch (nextState)
            {
                case GunState.Idle:
                {
                    if (!IsLocal)
                    {
                        animationHelper._SetCockingProgress(0);
                        animationHelper._SetTwistingProgress(0);
                    }

                    if (!AudioData)
                    {
                        break;
                    }

                    _PlayAudio(AudioData.CockingRelease, AudioData.CockingReleaseOffset);
                    break;
                }
                case GunState.InCockingTwisting:
                {
                    if (!IsLocal)
                    {
                        animationHelper._SetTwistingProgress(1);
                    }

                    if (!AudioData)
                    {
                        break;
                    }

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

                    break;
                }
                case GunState.InCockingPush:
                {

                    if (!IsLocal)
                    {
                        animationHelper._SetCockingProgress(1);
                        animationHelper._SetTwistingProgress(1);
                    }

                    if (!AudioData)
                    {
                        break;
                    }

                    _PlayAudio(AudioData.CockingPull, AudioData.CockingPullOffset);
                    break;
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
