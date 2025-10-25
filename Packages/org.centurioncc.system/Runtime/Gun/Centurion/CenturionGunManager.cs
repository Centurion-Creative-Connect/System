using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Gun.Centurion
{
    [DefaultExecutionOrder(100)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionGunManager : GunManagerBase
    {
        [Header("Root Transforms")]
        [SerializeField]
        private Transform gunsRoot;
        [SerializeField]
        private Transform variantsRoot;
        [Header("Options")]
        [SerializeField]
        private bool useDebugBulletTrail;
        [SerializeField]
        private bool useBulletTrail = true;
        [SerializeField]
        private bool useCollisionCheck = true;
        [SerializeField]
        private GunVariantDataStore fallbackVariantData;

        private WatchdogChildCallbackBase[] _childWdCallbacks;
        private int _lastFoundGunIndex;

        [ItemCanBeNull] private CenturionGun[] GunInstances { get; set; } = new CenturionGun[0];
        [ItemCanBeNull] private GunVariantDataStore[] VariantInstances { get; set; } = new GunVariantDataStore[0];

        public void Start()
        {
            // cant use get components in children yet so get transform child and get component each
            var variants = new GunVariantDataStore[variantsRoot.childCount];
            for (var i = 0; i < variants.Length; i++)
                variants[i] = variantsRoot.GetChild(i).GetComponent<GunVariantDataStore>();

            VariantInstances = variants;

            var managedGuns = new CenturionGun[gunsRoot.childCount];
            for (var i = 0; i < managedGuns.Length; i++)
                managedGuns[i] = gunsRoot.GetChild(i).GetComponent<CenturionGun>();

            GunInstances = managedGuns;

            if (VariantInstances == null || GunInstances == null)
            {
                Debug.LogError($"{Prefix}Required instances are not found. this will result in crash!");
                return;
            }

            FallbackVariantData = fallbackVariantData == null && VariantInstances.Length != 0 ? VariantInstances[0] : fallbackVariantData;
            UseCollisionCheck = useCollisionCheck;
            UseBulletTrail = useBulletTrail;
            UseDebugBulletTrail = useDebugBulletTrail;

            var watchdogCallbacks = new WatchdogChildCallbackBase[GunInstances.Length];
            for (var i = 0; i < GunInstances.Length; i++)
                // ReSharper disable once SuspiciousTypeConversion.Global
                watchdogCallbacks[i] = (WatchdogChildCallbackBase)(UdonSharpBehaviour)GunInstances[i]; // Udon allows such conversion... 
            _childWdCallbacks = watchdogCallbacks;
        }

        private void _MasterOnly_SpawnByData(GunVariantDataStore data, Vector3 position, Quaternion rotation)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError($"{Prefix}Could not spawn gun: You must be an master to spawn a gun!");
                return;
            }

            var gun = FindAvailableGun();
            if (gun == null)
            {
                Logger.LogError($"{Prefix}Could not spawn gun: Could not retrieve available gun!");
                return;
            }

            gun.MasterOnly_Occupy();
            gun.MasterOnly_SetVariantData(data);
            gun.MoveTo(position, rotation);
        }

        [NetworkCallable]
        public void Internal_MasterOnly_SpawnById(byte variantId, Vector3 position, Quaternion rotation)
        {
            if (!Networking.IsMaster) return;

            var variantData = GetVariantData(variantId);
            _MasterOnly_SpawnByData(variantData, position, rotation);
        }

        [NetworkCallable]
        public void Internal_MasterOnly_ReturnByIndex(int index)
        {
            if (!Networking.IsMaster) return;

            if (GunInstances.Length <= index)
            {
                Logger.LogError($"{Prefix}Internal_MasterOnly_ReturnByIndex: Index is out of range!");
                return;
            }

            var gun = GunInstances[index];
            if (gun == null)
            {
                Logger.LogError($"{Prefix}Internal_MasterOnly_ReturnByIndex: Target gun is null!");
                return;
            }

            gun.MasterOnly_Dispose();
        }

        [NetworkCallable]
        public void Internal_MasterOnly_ResetAll(GunManagerResetType type)
        {
            if (!Networking.IsMaster) return;

            switch (type)
            {
                case GunManagerResetType.All:
                {
                    foreach (var gun in GunInstances)
                    {
                        if (gun == null) continue;
                        gun.MasterOnly_Dispose();
                    }
                    break;
                }
                case GunManagerResetType.Unused:
                {
                    foreach (var gun in GunInstances)
                    {
                        if (gun == null || gun.IsPickedUp || gun.IsHolstered) continue;
                        gun.MasterOnly_Dispose();
                    }
                    break;
                }
                default:
                {
                    Logger.LogError($"{Prefix}Internal_MasterOnly_ResetAll: Unknown reset type: {type}");
                    return;
                }
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Invoke_OnGunsReset), (int)type);
        }

        [NetworkCallable]
        public void Internal_Refresh()
        {
            foreach (var managedGun in GunInstances)
                if (managedGun != null && managedGun.IsOccupied)
                    managedGun.RefreshData();
        }

        [CanBeNull]
        private CenturionGun FindAvailableGun()
        {
            for (var i = 0; i < GunInstances.Length; i++)
            {
                var gun = GunInstances[i];
                if (gun == null || gun.IsOccupied)
                    continue;

                _lastFoundGunIndex = i;
                return gun;
            }

            Logger.Log($"{Prefix}Free guns could not be found. Searching for non-held guns...");
            for (var i = 1; i < GunInstances.Length; i++)
            {
                var idx = (i + _lastFoundGunIndex) % GunInstances.Length;
                var gun = GunInstances[idx];
                if (gun == null || gun.IsPickedUp || gun.IsHolstered)
                    continue;

                _lastFoundGunIndex = idx;
                return gun;
            }

            if (GunInstances.Length == 0)
            {
                Logger.LogError($"{Prefix}No guns found!");
                return null;
            }

            Logger.Log($"{Prefix}Could not find available guns. returning first instance...");
            return GunInstances[0];
        }

        #region OverridenMethods
        public override WatchdogChildCallbackBase[] GetChildren()
        {
            return _childWdCallbacks;
        }

        public override GunBase[] GetGunInstances()
        {
            // ReSharper disable once CoVariantArrayConversion
            return GunInstances;
        }
        public override GunVariantDataStore[] GetVariantDataInstances()
        {
            return VariantInstances;
        }

        public override void _RequestSpawn(byte variantDataId, Vector3 position, Quaternion rotation)
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(Internal_MasterOnly_SpawnById), variantDataId, position, rotation);
        }

        public override void _RequestReset(int index)
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(Internal_MasterOnly_ReturnByIndex), index);
        }

        public override void _RequestResetAll(GunManagerResetType resetType)
        {
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(Internal_MasterOnly_ResetAll), (int)resetType);
        }

        public override void _RequestRefresh()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_Refresh));
        }
        #endregion
    }
}
