using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.Rule
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoShootingInAirRule : ShootingRule
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField]
        private TranslatableMessage cancelledMessage;

        public override int RuleId => 1000;
        public override TranslatableMessage CancelledMessage => cancelledMessage;

        private void Start()
        {
            gunManager.AddShootingRule(this);
        }

        public override bool CanLocalShoot(GunBase instance)
        {
            return Networking.LocalPlayer.IsPlayerGrounded();
        }
    }
}