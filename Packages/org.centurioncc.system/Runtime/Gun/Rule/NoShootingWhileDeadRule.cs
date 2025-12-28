using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.Rule
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoShootingWhileDeadRule : ShootingRuleBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField]
        private TranslatableMessage cancelledMessage;

        public override int RuleId => 1001;
        public override TranslatableMessage CancelledMessage => cancelledMessage;

        private void Start()
        {
            gunManager.AddShootingRule(this);
        }

        public override bool CanLocalShoot(GunBase instance)
        {
            var localPlayer = playerManager.GetLocalPlayer();
            return localPlayer == null || !localPlayer.IsDead;
        }
    }
}
