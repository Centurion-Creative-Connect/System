using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Utils;
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

        [UdonSynced] [FieldChangeCallback(nameof(IsOccupied))]
        private bool _isOccupied;

        private bool _isUpdatingVariantData;

        private GunVariantDataStore _variantData;

        [UdonSynced] [FieldChangeCallback(nameof(VariantDataUniqueId))]
        private byte _variantDataUniqueId = 0xFF;

        public override GunVariantDataStore VariantData => _variantData;

        /// <summary>
        /// Current Gun Variant ID
        /// <seealso cref="GunManagerBase.GetVariantDataById"/>
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

                // recurse guard
                if (_isUpdatingVariantData)
                {
                    Debug.LogWarning($"{Prefix}VariantDataUniqueId is being updated recursively!");
                    return;
                }
                _isUpdatingVariantData = true;

                if (_variantDataUniqueId == 0xFF)
                    Internal_ResetVariantData();
                else
                    RefreshData();

                _isUpdatingVariantData = false;
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
                CenturionDiagnostic.LogError($"{Prefix}ParentManager is null but trying to refresh data!: {VariantDataUniqueId}");
                return;
            }

            if (VariantDataUniqueId == 0xFF)
            {
                logger.LogError($"{Prefix}Will not refresh it's data because data is not specified.");
                return;
            }

            var data = gunManager.GetVariantDataById(VariantDataUniqueId);
            if (data == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}Will not refresh it's data because data is not found. maybe GunManager hasn't been initialized. Refreshing in 10 seconds!: {VariantDataUniqueId}");
                SendCustomEventDelayedSeconds(nameof(RefreshData), 10f);
                return;
            }

            Internal_SetVariantData(data);
        }

        private void Internal_SetVariantData(GunVariantDataStore data)
        {
            if (data == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}Internal_SetVariantData: data is null!");
                return;
            }

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.LogVerbose($"{Prefix}Internal_SetVariantData: {data.UniqueId}");
#endif
            // Destroy old model
            if (model)
            {
                Destroy(model);
            }

            VariantDataUniqueId = data.UniqueId;

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

        public void Internal_ResetVariantData()
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.LogVerbose($"{Prefix}ResetVariantData");
#endif

            foreach (var behaviour in Behaviours)
            {
                if (behaviour == null) continue;
                behaviour.Dispose(this);
            }

            VariantDataUniqueId = 0xFF;
            positioningHelper.SetPrimaryXAngleOffset(0);
            animationHelper.TargetAnimator = null;

            MainHandle.ForceDrop();
            MainHandle.UnHolster();

            if (model)
            {
                Destroy(model);
                model = null;
            }

            _variantData = null;
            IsOccupied = false;
        }

        public void RequestDisposeToMaster()
        {
            MainHandle.ForceDrop();
            if (SubHandle)
                SubHandle.ForceDrop();

            if (!Networking.IsMaster)
                return;

            MasterOnly_Dispose();
        }

        public bool MasterOnly_Occupy()
        {
            if (!Networking.IsMaster)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}You must be a master to occupy a ManagedGun!");
                return false;
            }

            var lastOccupied = IsOccupied;
            IsOccupied = true;

            if (lastOccupied != IsOccupied)
            {
                logger.Log($"{Prefix}{name} has been <color=green>occupied</color>");
            }

            return true;
        }

        public void MasterOnly_SetVariantData(GunVariantDataStore data)
        {
            if (!Networking.IsMaster)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}You must be a master to execute MasterOnly_SetVariantData!");
                return;
            }

            if (data == null)
            {
                logger.LogError($"{Prefix}MasterOnly_SetVariantData: data is null!");
                VariantDataUniqueId = 0xFF;
            }
            else
            {
                VariantDataUniqueId = data.UniqueId;
            }

            _RequestSync();
        }

        public bool MasterOnly_Dispose()
        {
            if (!IsOccupied)
            {
                // Already disposed
                return false;
            }

            if (!Networking.IsMaster)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}You must be a master to dispose a ManagedGun!");
                return false;
            }

            MainHandle.ForceDrop();
            if (SubHandle)
                SubHandle.ForceDrop();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ResetVariantData));
            _RequestSync();

            MoveTo(Vector3.down * 5, Quaternion.identity);

            logger.Log($"{Prefix}{name} has been <color=red>disposed</color>");
            return true;
        }
    }
}
