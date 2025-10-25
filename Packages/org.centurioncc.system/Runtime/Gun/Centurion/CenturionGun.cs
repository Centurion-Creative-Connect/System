using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gun.Centurion
{
    [DefaultExecutionOrder(110)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionGun : GunBase
    {
        [PublicAPI] [CanBeNull]
        public GameObject model;

        private bool _hasInit;

        [UdonSynced] [FieldChangeCallback(nameof(IsOccupied))]
        private bool _isOccupied;

        private GunVariantDataStore _variantData;

        [UdonSynced] [FieldChangeCallback(nameof(VariantDataUniqueId))]
        private byte _variantDataUniqueId = 0xFF;

        public override GunVariantDataStore VariantData => _variantData;

        /// <summary>
        /// Current Gun Variant ID
        /// <seealso cref="CenturionGunManager.GetVariantData"/>
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
                    RefreshData();
            }
        }

        public bool IsOccupied
        {
            get => _isOccupied;
            private set
            {
                _isOccupied = value;
                if (!value)
                {
                    Destroy(model);
                    model = null;
                }
            }
        }

        #region OverridenProperties
        public override bool IsInWall => VariantData && gunManager
            ? gunManager.UseCollisionCheck && VariantData.UseWallCheck && CollisionCount != 0
            : CollisionCount != 0;
        #endregion

        #region OverridenMethod
        protected override void OnTriggerEnter(Collider other)
        {
            if (IsLocal && other.name.ToLower().StartsWith("eraser"))
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RequestDisposeToMaster));
                return;
            }

            base.OnTriggerEnter(other);
        }
        #endregion

        public void RefreshData()
        {
            if (gunManager == null)
            {
                logger.LogError($"{Prefix}ParentManager is null but trying to refresh data!: {VariantDataUniqueId}");
                return;
            }

            if (VariantDataUniqueId == 0xFF)
            {
                logger.LogError($"{Prefix}Will not refresh it's data because data is not specified.");
                return;
            }

            var data = gunManager.GetVariantData(VariantDataUniqueId);
            Internal_SetVariantData(data);
        }

        private void Internal_SetVariantData(GunVariantDataStore data)
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.LogVerbose($"{Prefix}Internal_SetVariantData: {data.UniqueId}");
#endif
            // Destroy old model
            if (model)
            {
                Destroy(model);
            }

            // Set unique id - do not update the property `VariantDataUniqueId` because it'll call this function back!
            _variantDataUniqueId = data.UniqueId;

            model = Instantiate(data.gameObject, transform, false);
            if (model == null)
            {
                logger.LogError($"{Prefix}Internal_SetVariantData: Could not clone GunVariantDataStore!");
                return;
            }

            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _variantData = model.GetComponent<GunVariantDataStore>();
            if (_variantData == null)
            {
                logger.LogError($"{Prefix}Internal_SetVariantData: Could not retrieve cloned GunVariantDataStore!");
                return;
            }

            _Setup();
            gunManager.OnGunVariantChanged(this);
        }

        private void Internal_ResetVariantData()
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.LogVerbose($"{Prefix}ResetVariantData");
#endif

            foreach (var behaviour in Behaviours)
                behaviour.Dispose(this);

            // Set unique id - do not update the property `VariantDataUniqueId` because it'll call this function back!
            _variantDataUniqueId = 0xFF;
            animationHelper.TargetAnimator = null;
            if (model)
                Destroy(model);

            _variantData = null;

            MainHandle.UnHolster();
            FireMode = 0;
            ShotCount = 0;

            Trigger = TriggerState.Idle;
            State = GunState.Idle;
            HasBulletInChamber = false;
            HasCocked = false;

            CollisionCount = 0;

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
                logger.LogError($"{Prefix}You must be a master to occupy a ManagedGun!");
                return false;
            }

            var lastOccupied = IsOccupied;
            IsOccupied = true;

            _SetOwner(Networking.LocalPlayer);
            RequestSerialization();

            if (lastOccupied != IsOccupied)
                logger.Log($"{Prefix}{name} has been <color=green>occupied</color>");
            return true;
        }

        public void MasterOnly_SetVariantData(GunVariantDataStore data)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError($"{Prefix}You must be a master to execute MasterOnly_SetVariantData!");
                return;
            }

            if (data == null) return;

            Internal_SetVariantData(data);

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public bool MasterOnly_Dispose()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError($"{Prefix}You must be a master to dispose a ManagedGun!");
                return false;
            }

            MainHandle.ForceDrop();
            if (SubHandle)
                SubHandle.ForceDrop();

            Internal_ResetVariantData();

            _SetOwner(Networking.LocalPlayer);
            RequestSerialization();

            MoveTo(Vector3.down * 5, Quaternion.identity);

            logger.Log($"{Prefix}{name} has been <color=red>disposed</color>");
            return true;
        }
    }
}
