using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ShieldManager : UdonSharpBehaviour
    {
        private const string Prefix = "[ShieldManager] ";

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieLogger logger;

        private Shield _currentlyHeldShield;

        [UdonSynced] [FieldChangeCallback(nameof(DropShieldOnHit))]
        private bool _dropShieldOnHit;

        private int _eventCallbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[0];

        public bool DropShieldOnHit
        {
            get => _dropShieldOnHit;
            set
            {
                if (_dropShieldOnHit != value)
                    Invoke_OnDropShieldSettingChanged(value);
                _dropShieldOnHit = value;
            }
        }

        private void Start()
        {
            playerManager.SubscribeCallback(this);
            gunManager.SubscribeCallback(this);
        }

        public void SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.AddBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        public void UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.RemoveBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        #region ShieldManagerEventInvokers

        public bool Invoke_OnShieldPickup(Shield shield)
        {
            logger.Log($"{Prefix}Invoke_OnShieldPickup: {shield.name}");

            if (_currentlyHeldShield != null)
            {
                Invoke_OnShieldPickupCancelled(shield, PickupCancelContext.AlreadyPickedUp);
                return false;
            }

            _currentlyHeldShield = shield;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                if (((ShieldManagerCallbackBase)callback).OnShieldPickup(shield)) continue;

                Invoke_OnShieldPickupCancelled(shield, PickupCancelContext.Callback);
                return false;
            }

            return true;
        }

        private void Invoke_OnShieldPickupCancelled(Shield shield, PickupCancelContext context)
        {
            logger.Log($"{Prefix}Invoke_OnShieldPickupCancelled: {shield.name}, {context}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((ShieldManagerCallbackBase)callback).OnShieldPickupCancelled(shield, context);
            }
        }

        public void Invoke_OnShieldDrop(Shield shield, DropContext context)
        {
            logger.Log($"{Prefix}Invoke_OnShieldDrop: {shield.name}, {context}");

            if (_currentlyHeldShield == shield)
                _currentlyHeldShield = null;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((ShieldManagerCallbackBase)callback).OnShieldDrop(shield, context);
            }
        }

        public void Invoke_OnDropShieldSettingChanged(bool value)
        {
            logger.Log($"{Prefix}Invoke_OnDropShieldSettingChanged: {value}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((ShieldManagerCallbackBase)callback).OnDropShieldSettingChanged(value);
            }
        }

        #endregion

        #region PlayerManagerEvents

        public void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
        }

        public void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
        }

        public void OnFriendlyFire(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
        }

        public void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
        }

        public void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (!hitPlayer.IsLocal || _currentlyHeldShield == null)
                return;

            if (DropShieldOnHit && _currentlyHeldShield.DropShieldOnHit)
                _currentlyHeldShield.DropByHit();
        }

        public void OnTeamChanged(PlayerBase player, int oldTeam)
        {
        }

        public void OnPlayerTagChanged(TagType type, bool isOn)
        {
        }

        public void OnResetAllPlayerStats()
        {
        }

        public void OnResetPlayerStats(PlayerBase player)
        {
        }

        #endregion

        #region GunManagerEvents

        public bool CanShoot()
        {
            return _currentlyHeldShield == null || _currentlyHeldShield.CanShootWhileCarrying;
        }


        public void OnGunsReset()
        {
        }

        public void OnOccupyChanged(ManagedGun instance)
        {
        }

        public void OnVariantChanged(ManagedGun instance)
        {
        }

        public void OnPickedUpLocally(ManagedGun instance)
        {
        }

        public void OnDropLocally(ManagedGun instance)
        {
        }

        public void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
        }

        public void OnEmptyShoot(ManagedGun instance)
        {
        }

        public void OnShootFailed(ManagedGun instance, int reasonId)
        {
        }

        public void OnShootCancelled(ManagedGun instance, int reasonId)
        {
        }

        public void OnFireModeChanged(ManagedGun instance)
        {
        }

        #endregion
    }

    public abstract class ShieldManagerCallbackBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Called when shield was picked up.
        /// </summary>
        /// <param name="shield">shield which was picked up</param>
        /// <returns><c>true</c> to allow pickup, <c>false</c> to cancel pickup</returns>
        public virtual bool OnShieldPickup(Shield shield)
        {
            return true;
        }

        /// <summary>
        /// Called when shield pickup was cancelled.
        /// </summary>
        /// <param name="shield">shield which was cancelled pickup</param>
        /// <param name="context">context of cancellation</param>
        public virtual void OnShieldPickupCancelled(Shield shield, PickupCancelContext context)
        {
        }

        /// <summary>
        /// Called when shield was dropped.
        /// </summary>
        /// <param name="shield">shield which was dropped</param>
        /// <param name="context">context of dropping</param>
        public virtual void OnShieldDrop(Shield shield, DropContext context)
        {
        }

        /// <summary>
        /// Called when <see cref="ShieldManager.DropShieldOnHit"/> was changed.
        /// </summary>
        /// <param name="nextDropShieldOnHit">updated value of <see cref="ShieldManager.DropShieldOnHit"/></param>
        public virtual void OnDropShieldSettingChanged(bool nextDropShieldOnHit)
        {
        }
    }

    public enum PickupCancelContext
    {
        AlreadyPickedUp,
        Callback,
    }

    public enum DropContext
    {
        UserInput,
        PickupCancelled,
        Hit
    }
}