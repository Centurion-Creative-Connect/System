using System;
using CenturionCC.System.Gun.Behaviour;
using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Gun.Rule;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using Random = UnityEngine.Random;

namespace CenturionCC.System.Gun
{
    [DefaultExecutionOrder(100)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunManager : UdonSharpBehaviour
    {
        private const string Prefix = "[GunManager] ";

        private const string MustBeMasterError =
            Prefix + "<color=red>You must be an master to execute this method</color>: {0}";

        [SerializeField] private GameObject variantRoot;

        [SerializeField] private GameObject managedGunRoot;

        [SerializeField] private ProjectilePool bulletHolder;

        [SerializeField] private GunVariantDataStore fallbackVariantData;

        [SerializeField] private DefaultGunBehaviour fallbackBehaviour;

        [SerializeField] [HideInInspector] [NewbieInject]
        // ReSharper disable once InconsistentNaming
        public PrintableBase Logger;

        [SerializeField] [HideInInspector] [NewbieInject]
        // ReSharper disable once InconsistentNaming
        public RicochetHandler RicochetHandler;

        public int allowedRicochetCount = 0;

        [Obsolete("No longer used")] public int maxQueuedShotCount = 10;

        [Obsolete("Use GunModel with GunUpdater instead")]
        public float optimizationRange = 30F;

        public float handleRePickupDelay = 0.5F;
        public float maxHoldDistance = 0.3F;

        public bool useDebugBulletTrail;
        public bool useBulletTrail = true;
        public bool useCollisionCheck = true;
        private WatchdogChildCallbackBase[] _childWdCallbacks;

        private int _eventCallbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[5];

        private bool _isDebugGunHandleVisible;


        private int _lastResetIndex = -1;

        [ItemCanBeNull] public ManagedGun[] ManagedGunInstances { get; private set; } = new ManagedGun[0];

        [ItemNotNull] public ManagedGun[] LocalHeldGuns { get; private set; } = new ManagedGun[0];

        [ItemCanBeNull]
        public GunVariantDataStore[] VariantDataInstances { get; private set; } = new GunVariantDataStore[0];

        public ProjectilePool BulletHolder => bulletHolder;
        public DefaultGunBehaviour FallbackBehaviour => fallbackBehaviour;
        public GunVariantDataStore FallbackVariantData => fallbackVariantData;

        public bool IsDebugGunHandleVisible
        {
            get => _isDebugGunHandleVisible;
            set
            {
                if (_isDebugGunHandleVisible == value)
                    return;

                foreach (var managedGun in ManagedGunInstances)
                {
                    if (managedGun == null) continue;
                    managedGun.MainHandle.IsVisible = value;
                    managedGun.SubHandle.IsVisible = value;
                    managedGun.CustomHandle.IsVisible = value;
                }

                _isDebugGunHandleVisible = value;
            }
        }

        public bool CanLocalShoot
        {
            get
            {
                foreach (var callback in _eventCallbacks)
                {
                    if (callback == null) continue;

                    if (!((GunManagerCallbackBase)callback).CanShoot())
                    {
                        Debug.Log($"{Prefix}CanShoot: {callback.name} returned false");
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsHoldingGun => LocalHeldGuns.Length != 0;
        public int OccupiedRemoteGunCount { get; private set; }

        public void Start()
        {
            // cant use get components in children yet so get transform child and get component each
            var variants = new GunVariantDataStore[variantRoot.transform.childCount];
            for (var i = 0; i < variants.Length; i++)
                variants[i] = variantRoot.transform.GetChild(i).GetComponent<GunVariantDataStore>();

            VariantDataInstances = variants;

            var managedGuns = new ManagedGun[managedGunRoot.transform.childCount];
            for (var i = 0; i < managedGuns.Length; i++)
                managedGuns[i] = managedGunRoot.transform.GetChild(i).GetComponent<ManagedGun>();

            ManagedGunInstances = managedGuns;

            LocalHeldGuns = new ManagedGun[0];

            if (VariantDataInstances == null || ManagedGunInstances == null)
            {
                Debug.LogError($"{Prefix}Required instances are not found. this will result in crash!");
                return;
            }

            if (fallbackVariantData == null)
                fallbackVariantData = VariantDataInstances[0];

            foreach (var managedGun in ManagedGunInstances)
                managedGun.Init(this);

            var watchdogCallbacks = new WatchdogChildCallbackBase[managedGuns.Length];
            for (var i = 0; i < managedGuns.Length; i++)
                watchdogCallbacks[i] =
                    (WatchdogChildCallbackBase)(UdonSharpBehaviour)ManagedGunInstances[i];
            _childWdCallbacks = watchdogCallbacks;
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return _childWdCallbacks;
        }

        public override void Interact()
        {
            Debug.Log("--- begin debug gun manager log ---");

            Debug.Log("-- Begin Variant Data --");

            const string variantFormat = "\n{0}: uniqueId = {1}, isDoubleHanded = {2}";
            foreach (var variant in VariantDataInstances)
                Debug.Log(variant != null
                    ? string.Format(variantFormat, variant.name, variant.UniqueId, variant.IsDoubleHanded)
                    : "null");

            Debug.Log("-- Begin Managed Gun Data --");

            const string managedGunFormat = "\n{0}: IsOccupied = {1}, VariantUniqueId = {2}";
            foreach (var managedGun in ManagedGunInstances)
                Debug.Log(managedGun != null
                    ? string.Format
                    (
                        managedGunFormat,
                        managedGun.name,
                        managedGun.IsOccupied,
                        managedGun.VariantDataUniqueId
                    )
                    : "null");

            Debug.Log("-- Begin Watchdog Callback Data --");

            foreach (var wdCallback in _childWdCallbacks)
                Debug.Log(wdCallback != null ? wdCallback.name : "null");
        }

        [CanBeNull]
        public ManagedGun MasterOnly_Spawn(byte variantId, Vector3 position, Quaternion rotation)
        {
            var variantData = GetVariantData(variantId);
            return MasterOnly_SpawnWithData(variantData, position, rotation);
        }

        [CanBeNull]
        public ManagedGun MasterOnly_SpawnWithData(GunVariantDataStore data, Vector3 position, Quaternion rotation)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError($"{Prefix}Could not spawn gun: You must be an master to spawn a gun!");
                return null;
            }

            var remote = GetAvailableManagedGun();
            if (remote == null)
            {
                Logger.LogError($"{Prefix}Could not spawn gun: Could not retrieve available managed gun!");
                return null;
            }

            remote.MasterOnly_Occupy();
            remote.MasterOnly_SetVariantData(data);
            remote.MoveTo(position, rotation);
            return remote;
        }

        public void MasterOnly_ResetRemoteGuns()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetRemoteGuns)));
                return;
            }

            Logger.Log($"{Prefix}Resetting ManagedGuns");
            foreach (var managedGun in ManagedGunInstances)
                if (managedGun != null)
                    managedGun.MasterOnly_Dispose();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Invoke_OnGunsReset));
            Logger.Log($"{Prefix}ManagedGuns reset complete");
        }

        public void MasterOnly_SlowlyResetRemoteGuns()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SlowlyResetRemoteGuns)));
                return;
            }

            const int chunk = 8;
            Logger.Log($"{Prefix}Resetting chunk {_lastResetIndex + 1} to {_lastResetIndex + 1 + chunk}");
            var managedGuns = ManagedGunInstances;
            for (var i = 0; i < chunk; i++)
            {
                if (Networking.IsClogged)
                {
                    Logger.LogWarn($"{Prefix}Reset interrupted because network is clogged!");
                    SendCustomEventDelayedSeconds(nameof(MasterOnly_SlowlyResetRemoteGuns), 2);
                    return;
                }

                ++_lastResetIndex;
                if (managedGuns.Length <= _lastResetIndex)
                    break;

                var managedGun = managedGuns[_lastResetIndex];
                if (managedGun != null)
                    managedGun.MasterOnly_Dispose();
            }

            if (managedGuns.Length <= _lastResetIndex + 1)
            {
                Logger.Log($"{Prefix}Reset complete");
                _lastResetIndex = -1;
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Invoke_OnGunsReset));
                return;
            }

            SendCustomEventDelayedSeconds(nameof(MasterOnly_SlowlyResetRemoteGuns), Random.Range(0F, 2F));
        }

        public void ReloadGuns()
        {
            foreach (var managedGun in ManagedGunInstances)
                if (managedGun != null && managedGun.IsOccupied)
                    managedGun.RefreshData(false);
        }

        [CanBeNull]
        public GunVariantDataStore GetVariantData(byte variantUniqueId)
        {
            foreach (var variant in VariantDataInstances)
                if (variant != null && variant.UniqueId == variantUniqueId)
                    return variant;

            Logger.LogError(
                $"{Prefix}GetVariantData: specified variant data not found. returning fallback data");
            return FallbackVariantData;
        }

        [CanBeNull]
        private ManagedGun GetAvailableManagedGun()
        {
            foreach (var managedGun in ManagedGunInstances)
                if (managedGun != null && !managedGun.IsOccupied)
                    return managedGun;
            Logger.Log($"{Prefix}available remote gun not found. returning index of 0");
            return ManagedGunInstances[0];
        }

        #region CallbackRegisterer

        public void SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.AddBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        public void UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.RemoveBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        #endregion

        #region ShootingRule

        private readonly DataDictionary _shootingRuleDict = new DataDictionary();
        private ShootingRule[] _shootingRules = new ShootingRule[0];

        [PublicAPI]
        public void AddShootingRule(ShootingRule rule)
        {
            _shootingRuleDict.Add(rule.RuleId, rule);
            _shootingRules = _shootingRules.AddAsList(rule);
        }

        [PublicAPI]
        public void RemoveShootingRule(ShootingRule rule)
        {
            _shootingRuleDict.Remove(rule.RuleId);
            _shootingRules = _shootingRules.RemoveItem(rule);
        }

        [PublicAPI]
        [CanBeNull]
        public TranslatableMessage GetCancelledMessageOf(int ruleId)
        {
            return _shootingRuleDict.TryGetValue(ruleId, TokenType.Reference, out var rule)
                ? ((ShootingRule)rule.Reference).CancelledMessage
                : null;
        }

        [PublicAPI]
        public bool CheckCanLocalShoot(GunBase instance, out int ruleId)
        {
            foreach (var rule in _shootingRules)
                if (rule != null && !rule.CanLocalShoot(instance))
                {
                    ruleId = rule.RuleId;
                    return false;
                }

            ruleId = 0;
            return true;
        }

        #endregion

        #region GunManagerEvents

        public void Invoke_OnGunsReset()
        {
            Logger.Log($"{Prefix}OnGunsReset");
            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnGunsReset();
            }
        }

        public void Invoke_OnOccupyChanged(ManagedGun instance)
        {
            if (instance == null) return;
            Logger.Log($"{Prefix}OnOccupyChanged: {instance.name}, {instance.IsOccupied}");

            if (instance.IsOccupied)
                ++OccupiedRemoteGunCount;
            else
                --OccupiedRemoteGunCount;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnOccupyChanged(instance);
            }
        }

        public void Invoke_OnVariantChanged(ManagedGun instance)
        {
            if (instance == null) return;

            Logger.Log($"{Prefix}OnVariantChanged: {instance.name}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnVariantChanged(instance);
            }
        }

        public void Invoke_OnPickedUpLocally(ManagedGun instance)
        {
            if (instance == null) return;

            Logger.Log($"{Prefix}OnPickedUpLocally: {instance.name}");
            LocalHeldGuns = LocalHeldGuns.AddAsSet(instance);

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnPickedUpLocally(instance);
            }
        }

        public void Invoke_OnDropLocally(ManagedGun instance)
        {
            if (instance == null) return;

            Logger.Log($"{Prefix}OnDropLocally: {instance.name}");
            LocalHeldGuns = LocalHeldGuns.RemoveItem(instance);

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnDropLocally(instance);
            }
        }

        public void Invoke_OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnShoot(instance, projectile);
            }
        }

        public void Invoke_OnEmptyShoot(ManagedGun instance)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnEmptyShoot(instance);
            }
        }

        public void Invoke_OnShootFailed(ManagedGun instance, int reasonId)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnShootFailed(instance, reasonId);
            }
        }

        public void Invoke_OnShootCancelled(ManagedGun instance, int reasonId)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnShootCancelled(instance, reasonId);
            }
        }

        public void Invoke_OnFireModeChanged(ManagedGun instance)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnFireModeChanged(instance);
            }
        }

        #endregion
    }
}