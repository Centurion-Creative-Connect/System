using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.Rule
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [RequireComponent(typeof(TranslatableMessage))]
    public class NoShootingInSafeZoneRule : ShootingRule
    {
        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private TranslatableMessage cancelledMessage;

        public override int RuleId => 101;
        public override TranslatableMessage CancelledMessage => cancelledMessage;

        private void Start()
        {
            gunManager.AddShootingRule(this);
        }

        public override bool CanLocalShoot(GunBase instance)
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (!localPlayer) return true;

            return !localPlayer.IsInSafeZone;
        }
    }
}