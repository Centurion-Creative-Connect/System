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
using VRC.SDK3.UdonNetworkCalling;
namespace CenturionCC.System.Gun
{
    public abstract class GunManagerBase : UdonSharpBehaviour
    {
        protected const string Prefix = "[GunManager] ";

        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        private int _eventCallbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[0];
        private bool _isDebugGunHandleVisible;

        private GunBase[] _locallyHeldGuns = new GunBase[0];

        protected PrintableBase Logger => logger;

        [PublicAPI]
        public virtual bool IsHoldingGun => _locallyHeldGuns.Length > 0;
        [PublicAPI]
        public virtual bool UseCollisionCheck { get; set; }
        [PublicAPI]
        public virtual int AllowedRicochetCount { get; set; }
        [PublicAPI]
        public virtual bool UseDebugBulletTrail { get; set; }
        [PublicAPI]
        public virtual bool UseBulletTrail { get; set; }
        [PublicAPI]
        public virtual bool IsDebugGunHandleVisible
        {
            get => _isDebugGunHandleVisible;
            set
            {
                _isDebugGunHandleVisible = value;
                foreach (var gun in GetGunInstances())
                {
                    if (gun == null) continue;
                    gun._SetHandlesVisible(value);
                }
            }
        }

        public virtual GunVariantDataStore FallbackVariantData { get; protected set; }

        public abstract GunBase[] GetGunInstances();
        public abstract GunVariantDataStore[] GetVariantDataInstances();

        public abstract void _RequestSpawn(byte variantDataId, Vector3 position, Quaternion rotation);
        public abstract void _RequestReset(int index);
        public abstract void _RequestResetAll(GunManagerResetType resetType);
        public abstract void _RequestRefresh();

        public virtual GunBase[] GetLocallyHeldGunInstances()
        {
            return _locallyHeldGuns;
        }

        public virtual GunVariantDataStore GetVariantData(byte uniqueId)
        {
            foreach (var dataStore in GetVariantDataInstances())
            {
                if (dataStore == null || dataStore.UniqueId != uniqueId) continue;
                return dataStore;
            }

            return FallbackVariantData;
        }

        #region GunBaseCallbacks
        public virtual void OnGunPickedUpLocally(GunBase gun)
        {
            _locallyHeldGuns = _locallyHeldGuns.AddAsSet(gun);
            Invoke_OnPickedUpLocally(gun);
        }

        public virtual void OnGunDroppedLocally(GunBase gun)
        {
            _locallyHeldGuns = _locallyHeldGuns.RemoveItem(gun);
            Invoke_OnDropLocally(gun);
        }

        [PublicAPI]
        public virtual void OnGunVariantChanged(GunBase gun)
        {
            Invoke_OnVariantChanged(gun);
        }
        #endregion

        #region GunManagerEvents
        [PublicAPI]
        public void SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.AddBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        [PublicAPI]
        public void UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.RemoveBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        [NetworkCallable]
        public void Invoke_OnGunsReset(GunManagerResetType type)
        {
            Logger.Log($"{Prefix}OnGunsResetAll");
            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnGunsReset(type);
            }
        }

        private void Invoke_OnVariantChanged(GunBase instance)
        {
            if (instance == null) return;

            Logger.Log($"{Prefix}OnVariantChanged: {instance.name}");
            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnVariantChanged(instance);
            }
        }

        private void Invoke_OnPickedUpLocally(GunBase instance)
        {
            if (instance == null) return;

            Logger.Log($"{Prefix}OnPickedUpLocally: {instance.name}");
            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnPickedUpLocally(instance);
            }
        }

        private void Invoke_OnDropLocally(GunBase instance)
        {
            if (instance == null) return;

            Logger.Log($"{Prefix}OnDropLocally: {instance.name}");
            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnDropLocally(instance);
            }
        }

        public void Invoke_OnShoot(GunBase instance, ProjectileBase projectile)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnShoot(instance, projectile);
            }
        }

        public void Invoke_OnEmptyShoot(GunBase instance)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnEmptyShoot(instance);
            }
        }

        public void Invoke_OnShootFailed(GunBase instance, int reasonId)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnShootFailed(instance, reasonId);
            }
        }

        public void Invoke_OnShootCancelled(GunBase instance, int reasonId)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnShootCancelled(instance, reasonId);
            }
        }

        public void Invoke_OnFireModeChanged(GunBase instance)
        {
            if (instance == null) return;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((GunManagerCallbackBase)callback).OnFireModeChanged(instance);
            }
        }
        #endregion

        #region ShootingRule
        private readonly DataDictionary _shootingRuleDict = new DataDictionary();
        private ShootingRuleBase[] _shootingRules = new ShootingRuleBase[0];

        [PublicAPI]
        public void AddShootingRule(ShootingRuleBase rule)
        {
            _shootingRuleDict.Add(rule.RuleId, rule);
            _shootingRules = _shootingRules.AddAsList(rule);
        }

        [PublicAPI]
        public void RemoveShootingRule(ShootingRuleBase rule)
        {
            _shootingRuleDict.Remove(rule.RuleId);
            _shootingRules = _shootingRules.RemoveItem(rule);
        }

        [PublicAPI] [CanBeNull]
        public TranslatableMessage GetCancelledMessageOf(int ruleId)
        {
            return _shootingRuleDict.TryGetValue(ruleId, TokenType.Reference, out var rule)
                ? ((ShootingRuleBase)rule.Reference).CancelledMessage
                : null;
        }

        [PublicAPI]
        public virtual bool CanShoot(GunBase instance, out int ruleId)
        {
            foreach (var callback in _eventCallbacks)
            {
                if (callback == null || ((GunManagerCallbackBase)callback).CanShoot())
                    continue;
                ruleId = 200;
                return false;
            }

            foreach (var rule in _shootingRules)
            {
                if (rule == null || rule.CanLocalShoot(instance))
                    continue;
                ruleId = rule.RuleId;
                return false;
            }

            ruleId = 0;
            return true;
        }
        #endregion

        #region WatchdogCallbacks
        public virtual int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public virtual WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }
        #endregion
    }
}
