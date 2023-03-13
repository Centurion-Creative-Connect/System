using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
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
        private RoleManager roleManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieLogger logger;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationUI notification;

        [SerializeField]
        private TranslatableMessage shieldDroppedBecauseHit;
        [SerializeField]
        private TranslatableMessage dropShieldOnHitEnabled;
        [SerializeField]
        private TranslatableMessage dropShieldOnHitDisabled;

        private Shield _currentlyHeldShield;

        [UdonSynced] [FieldChangeCallback(nameof(DropShieldOnHit))]
        private bool _dropShieldOnHit;
        private int _eventCallbackCount;

        private UdonSharpBehaviour[] _eventCallbacks;

        public bool DropShieldOnHit
        {
            get => _dropShieldOnHit;
            set
            {
                _dropShieldOnHit = value;
                if (roleManager.GetPlayerRole().HasPermission())
                    notification.ShowInfo(value ? dropShieldOnHitEnabled.Message : dropShieldOnHitDisabled.Message);
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
                Invoke_OnShieldPickupCancelled(shield, 1);
                return false;
            }

            _currentlyHeldShield = shield;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                if (((ShieldManagerCallbackBase)callback).OnShieldPickup(shield)) continue;

                Invoke_OnShieldPickupCancelled(shield, 2);
                return false;
            }

            return true;
        }

        private void Invoke_OnShieldPickupCancelled(Shield shield, int reasonId)
        {
            logger.Log($"{Prefix}Invoke_OnShieldPickupCancelled: {shield.name}, {reasonId}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((ShieldManagerCallbackBase)callback).OnShieldPickupCancelled(shield, reasonId);
            }
        }

        public void Invoke_OnShieldDrop(Shield shield)
        {
            logger.Log($"{Prefix}Invoke_OnShieldDrop: {shield.name}");

            if (_currentlyHeldShield == shield)
                _currentlyHeldShield = null;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((ShieldManagerCallbackBase)callback).OnShieldDrop(shield);
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
            if (!hitPlayer.IsLocal)
                return;

            if (_currentlyHeldShield == null)
                return;

            if (DropShieldOnHit && _currentlyHeldShield.DropShieldOnHit)
            {
                _currentlyHeldShield.Drop();
                notification.ShowWarn(shieldDroppedBecauseHit.Message);
            }
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
        /// <param name="reasonId">numeric id of the reason</param>
        /// <remarks>
        /// Currently set cancel reasons are:
        /// 1 = Another shield was currently picked up.
        /// 2 = Callback cancelled shield pickup.
        /// </remarks>
        public virtual void OnShieldPickupCancelled(Shield shield, int reasonId)
        {
        }

        /// <summary>
        /// Called when shield was dropped.
        /// </summary>
        /// <param name="shield">shield which was dropped</param>
        public virtual void OnShieldDrop(Shield shield)
        {
        }
    }
}