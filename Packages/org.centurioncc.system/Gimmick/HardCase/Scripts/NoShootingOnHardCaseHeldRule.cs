using CenturionCC.System.Gun;
using CenturionCC.System.Gun.Rule;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.HardCase
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoShootingOnHardCaseHeldRule : ShootingRuleBase
    {
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [NewbieInject(SearchScope.Scene)]
        private HardCaseKeyboard[] keyboards;

        [SerializeField]
        private TranslatableMessage cancelledMessage;

        private bool _isHoldingHardCase;
        public override int RuleId => 67658369;
        public override TranslatableMessage CancelledMessage => cancelledMessage;

        private void Start()
        {
            gunManager.AddShootingRule(this);

            foreach (var keyboard in keyboards) keyboard.Subscribe(this);
        }

        public override bool CanLocalShoot(GunBase instance)
        {
            return !_isHoldingHardCase;
        }

        public void OnKeyboardUseDown()
        {
            _isHoldingHardCase = true;
        }

        public void OnKeyboardUseUp()
        {
            _isHoldingHardCase = false;
        }
    }
}
