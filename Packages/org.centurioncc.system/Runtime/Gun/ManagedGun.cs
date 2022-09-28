using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Gun.GunCamera;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
#endif

namespace CenturionCC.System.Gun
{
    [DefaultExecutionOrder(110)] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ManagedGun : Gun
    {
        private Animator _animator;
        private bool _hasInit;
        [UdonSynced] [FieldChangeCallback(nameof(IsOccupied))]
        private bool _isOccupied;

        [UdonSynced] [FieldChangeCallback(nameof(VariantDataUniqueId))]
        private byte _variantDataUniqueId = 0xFF;

        [PublicAPI]
        public GunManager ParentManager { get; protected set; }

        [PublicAPI] [CanBeNull]
        public GameObject Model { get; protected set; }

        [PublicAPI]
        public BoxCollider Collider { get; protected set; }

        [PublicAPI] [CanBeNull]
        public GunVariantDataStore VariantData { get; protected set; }

        [PublicAPI] [CanBeNull]
        public GunCameraDataStore CameraData => VariantData != null ? VariantData.CameraData : null;

        /// <summary>
        /// Current Gun Variant ID
        /// <seealso cref="GunManager.GetVariantData"/>
        /// </summary>
        /// <remarks>
        /// Should only be changed by instance master.
        /// </remarks>
        public byte VariantDataUniqueId
        {
            get => _variantDataUniqueId;
            private set
            {
                _variantDataUniqueId = value;
                if (_variantDataUniqueId == 0xFF)
                    Internal_ResetVariantData();
                else
                    RefreshData(true);

                UpdatePositionForSync();
            }
        }

        public bool IsOccupied
        {
            get => _isOccupied;
            private set
            {
                var lastOccupied = _isOccupied;
                _isOccupied = value;
                if (!value)
                {
                    Destroy(Model);
                    Model = null;
                }

                if (value)
                {
                    UpdatePositionForSync();
                }

                if (lastOccupied != value)
                    ParentManager.Invoke_OnOccupyChanged(this);
            }
        }

        protected override void Start()
        {
            Logger = GameManagerHelper.GetLogger();
            AudioManager = GameManagerHelper.GetAudioManager();
            UpdateManager = GameManagerHelper.GetUpdateManager();

            target = transform;

            Collider = GetComponent<BoxCollider>();

            MainHandle.callback = this;
            MainHandle.handleType = HandleType.MainHandle;

            SubHandle.callback = this;
            SubHandle.handleType = HandleType.SubHandle;

            customHandle.callback = this;
            customHandle.handleType = HandleType.CustomHandle;

            Internal_SetPivot(HandleType.MainHandle);

            UpdateManager.SubscribeUpdate(this);
            UpdateManager.SubscribeSlowUpdate(this);
        }

        public void Init(GunManager parentManager)
        {
            if (_hasInit)
            {
                Logger.LogError($"{Prefix}I'm already initialized!");
                return;
            }

            ParentManager = parentManager;

            _hasInit = true;
        }

        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public void RefreshData(bool refreshHandleOffset)
        {
            if (ParentManager == null)
            {
                Logger.LogError($"{Prefix}ParentManager is null but trying to refresh data!: {VariantDataUniqueId}");
                return;
            }

            if (VariantDataUniqueId == 0xFF)
            {
                Logger.LogError($"{Prefix}Will not refresh it's data because data is not specified.");
                return;
            }

            var data = ParentManager.GetVariantData(VariantDataUniqueId);
            Internal_SetVariantData(data, refreshHandleOffset);
        }

        private void Internal_SetVariantData(GunVariantDataStore data, bool refreshHandleOffset)
        {
            Logger.LogVerbose($"{Prefix}Internal_SetVariantData: {data.UniqueId}");
            // Set unique id
            _variantDataUniqueId = data.UniqueId;

            // Set new model and move to current position
            _animator = null;
            if (Model)
                Destroy(Model);

            Model = Instantiate(data.Model);
            if (Model != null)
            {
                var mt = Model.transform;
                mt.SetParent(Target);
                mt.localPosition = data.ModelPositionOffset;
                mt.localRotation = data.ModelRotationOffset;
                _animator = Model.GetComponentInChildren<Animator>();
            }

            VariantData = data;

            // Set shooter's position and rotation
            shooter.localPosition = data.FiringPositionOffset;
            shooter.localRotation = data.FiringRotationOffset;

            // Update handle's actual position
            if (refreshHandleOffset)
            {
                Internal_SetPivot(HandleType.MainHandle);
                MainHandle.MoveToLocalPosition(MainHandlePositionOffset, MainHandleRotationOffset);
                SubHandle.MoveToLocalPosition(SubHandlePositionOffset, SubHandleRotationOffset);
            }

            Internal_UpdateHandlePickupable();

            // Update handle's tooltip
            MainHandle.UseText = data.TooltipMessage;

            // Apply colliders setting
            if (data.HasColliderSetting)
            {
                Collider.enabled = true;
                Collider.center = data.ColliderCenter;
                Collider.size = data.ColliderSize;
            }

            CollisionCount = 0;

            // Update behaviour related properties
            FireMode = AvailableFireModes[0];

            // Finally call setup for behaviour
            if (Behaviour != null)
                Behaviour.Setup(this);

            ParentManager.Invoke_OnVariantChanged(this);
        }

        private void Internal_ResetVariantData()
        {
            Logger.LogVerbose($"{Prefix}ResetVariantData");
            if (Behaviour != null)
                Behaviour.Dispose(this);

            _variantDataUniqueId = 0xFF;
            _animator = null;
            if (Model)
                Destroy(Model);

            VariantData = null;

            MainHandle.UnHolster(); // TODO: check this wont hurt something by syncing across players
            FireMode = 0;
            ShotCount = 0;
            QueuedShotCount = 0;

            Trigger = TriggerState.Idle;
            IsInSafeZone = false;

            Collider.enabled = false;
            Collider.center = Vector3.zero;
            Collider.size = Vector3.zero;
            CollisionCount = 0;

            CustomHandle.ForceDrop();
            CustomHandle.SetPickupable(false);

            Internal_SetPivot(HandleType.MainHandle);

            IsOccupied = false;
        }

        public void RequestDisposeToMaster()
        {
            if (!Networking.IsMaster)
                return;

            MasterOnly_Dispose();
        }

        public bool MasterOnly_Occupy()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError($"{Prefix}You must be a master to occupy a ManagedGun!");
                return false;
            }

            var lastOccupied = IsOccupied;
            IsOccupied = true;

            Internal_SetRelatedObjectsOwner(Networking.LocalPlayer);
            RequestSerialization();

            if (lastOccupied != IsOccupied)
                Logger.Log($"{Prefix}{name} has been <color=green>occupied</color>");
            return true;
        }

        public void MasterOnly_SetVariantData(GunVariantDataStore data)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError($"{Prefix}You must be a master to execute MasterOnly_SetVariantData!");
                return;
            }

            if (data == null) return;

            Internal_SetVariantData(data, true);

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public bool MasterOnly_Dispose()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError($"{Prefix}You must be a master to dispose a ManagedGun!");
                return false;
            }

            MainHandle.ForceDrop();
            SubHandle.ForceDrop();

            Internal_ResetVariantData();

            Internal_SetRelatedObjectsOwner(Networking.LocalPlayer);
            RequestSerialization();

            MoveTo(Vector3.down * 5, Quaternion.identity);

            Logger.Log($"{Prefix}{name} has been <color=red>disposed</color>");
            return true;
        }

        #region OverridenProperties

        public override string WeaponName => VariantData != null ? VariantData.WeaponName : null;
        public override Animator TargetAnimator => _animator;
        public override ProjectilePool BulletHolder =>
            ParentManager != null ? ParentManager.BulletHolder : null;
        public override GunBehaviourBase Behaviour =>
            VariantData != null ? VariantData.Behaviour : null;
        [PublicAPI]
        public override ProjectileDataProvider ProjectileData =>
            VariantData != null ? VariantData.ProjectileData : null;
        [PublicAPI]
        public override GunAudioDataStore AudioData =>
            VariantData != null ? VariantData.AudioData : null;
        [PublicAPI]
        public override GunHapticDataStore HapticData =>
            VariantData != null ? VariantData.HapticData : null;


        [PublicAPI]
        public override Vector3 MainHandlePositionOffset =>
            VariantData != null ? VariantData.MainHandlePositionOffset : Vector3.zero;
        [PublicAPI]
        public override Quaternion MainHandleRotationOffset =>
            VariantData != null ? VariantData.MainHandleRotationOffset : Quaternion.identity;


        [PublicAPI]
        public override Vector3 SubHandlePositionOffset =>
            VariantData != null ? VariantData.SubHandlePositionOffset : Vector3.forward;
        [PublicAPI]
        public override Quaternion SubHandleRotationOffset =>
            VariantData != null ? VariantData.SubHandleRotationOffset : Quaternion.identity;


        [PublicAPI]
        public override float MainHandlePitchOffset => VariantData != null ? VariantData.MainHandlePitchOffset : 0F;


        public override bool IsDoubleHandedGun => VariantData != null && VariantData.IsDoubleHanded;

        [PublicAPI]
        public override int RequiredHolsterSize => VariantData != null ? VariantData.HolsterSize : 0;

        [PublicAPI]
        public override float MainHandleRePickupDelay =>
            VariantData != null && ParentManager != null && VariantData.UseRePickupDelayForMainHandle
                ? ParentManager.HandleRePickupDelay
                : 0F;
        [PublicAPI]
        public override float SubHandleRePickupDelay =>
            VariantData != null && ParentManager != null && VariantData.UseRePickupDelayForSubHandle
                ? ParentManager.HandleRePickupDelay
                : 0F;
        [PublicAPI]
        public override float OptimizationRange => ParentManager != null ? ParentManager.OptimizationRange : 0F;

        [PublicAPI]
        public override int MaxQueuedShotCount => ParentManager != null ? ParentManager.MaxQueuedShotCount : 10;

        public override float RoundsPerSecond =>
            VariantData != null ? VariantData.MaxRoundsPerSecond : float.PositiveInfinity;

        [PublicAPI]
        public override FireMode[] AvailableFireModes =>
            VariantData != null ? VariantData.AvailableFiringModes : new[] { FireMode.Safety };

        #endregion

        #region OverridenMethod

        protected override void OnTriggerEnter(Collider other)
        {
            if (other.name.ToLower().StartsWith("eraser"))
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RequestDisposeToMaster));
                return;
            }

            base.OnTriggerEnter(other);
        }

        protected override ShotResult CanShoot()
        {
            var result = base.CanShoot();

            if (FireMode == FireMode.Safety)
            {
                ParentManager.Invoke_OnShootFailed(this, 12);
                result = ShotResult.Failed;
            }

            if (ParentManager.UseCollisionCheck)
            {
                if (IsInWall)
                {
                    ParentManager.Invoke_OnShootFailed(this, 100);
                    result = ShotResult.Failed;
                }

                if (IsInSafeZone)
                {
                    ParentManager.Invoke_OnShootCancelled(this, 101);
                    result = ShotResult.Cancelled;
                }
            }

            if (ParentManager.CanLocalShoot == false)
            {
                ParentManager.Invoke_OnShootCancelled(this, 200);
                result = ShotResult.Cancelled;
            }

            return result;
        }

        protected override void OnShoot(ProjectileBase bullet, bool isPellet)
        {
            ParentManager.Invoke_OnShoot(this, bullet);
        }

        protected override void OnEmptyShoot()
        {
            ParentManager.Invoke_OnEmptyShoot(this);
        }

        protected override void OnFireModeChanged(FireMode previous, FireMode next)
        {
            if (!IsLocal || previous == next)
                return;
            ParentManager.Invoke_OnFireModeChanged(this);
        }

        protected override void OnGunPickup()
        {
            ParentManager.Invoke_OnPickedUpLocally(this);
        }

        protected override void OnGunDrop()
        {
            ParentManager.Invoke_OnDropLocally(this);
        }

        #endregion
    }
}