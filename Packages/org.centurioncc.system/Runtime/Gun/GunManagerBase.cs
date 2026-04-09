using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Gun.Rule;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
namespace CenturionCC.System.Gun
{
    [RequireComponent(typeof(GunManagerEventHelper))]
    public abstract class GunManagerBase : UdonSharpBehaviour
    {
        protected const string Prefix = "[<color=olive>GunManager</color>] ";

        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        [SerializeField] [NewbieInject]
        private GunManagerEventHelper eventHelper;

        private bool _isDebugGunHandleVisible;

        private GunBase[] _locallyHeldGuns = new GunBase[0];

        protected PrintableBase Logger => logger;

        [PublicAPI]
        public GunManagerEventHelper Event => eventHelper;

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
        public abstract void _RequestSync();

        public GunBase[] GetLocallyHeldGunInstances()
        {
            return _locallyHeldGuns;
        }

        #region GunBaseCallbacks
        public virtual void OnGunPickedUpLocally(GunBase gun)
        {
            _locallyHeldGuns = _locallyHeldGuns.AddAsSet(gun);
            Event.Invoke_OnPickedUpLocally(gun);
        }

        public virtual void OnGunDroppedLocally(GunBase gun)
        {
            _locallyHeldGuns = _locallyHeldGuns.RemoveItem(gun);
            Event.Invoke_OnDropLocally(gun);
        }

        [PublicAPI]
        public virtual void OnGunVariantChanged(GunBase gun)
        {
            Event.Invoke_OnVariantChanged(gun);
        }
        #endregion

        #region GunManagerEvents
        [PublicAPI]
        public bool Subscribe(UdonSharpBehaviour behaviour)
        {
            return Event.Subscribe(behaviour);
        }

        [PublicAPI]
        public bool Unsubscribe(UdonSharpBehaviour behaviour)
        {
            return Event.Unsubscribe(behaviour);
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
            if (!Event.Invoke_CanShoot())
            {
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
