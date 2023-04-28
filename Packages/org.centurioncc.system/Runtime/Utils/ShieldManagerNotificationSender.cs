using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ShieldManagerNotificationSender : ShieldManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private ShieldManager shieldManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleManager roleManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;

        [SerializeField]
        private TranslatableMessage shieldDroppedBecauseHit;
        [SerializeField]
        private TranslatableMessage shieldDroppedBecauseAlreadyPickedUp;
        [SerializeField]
        private TranslatableMessage dropShieldOnHitEnabled;
        [SerializeField]
        private TranslatableMessage dropShieldOnHitDisabled;

        private void Start()
        {
            shieldManager.SubscribeCallback(this);
        }

        public override void OnShieldDrop(Shield shield, DropContext context)
        {
            switch (context)
            {
                case DropContext.Hit:
                {
                    notification.ShowWarn(shieldDroppedBecauseHit.Message);
                    break;
                }
            }
        }

        public override void OnShieldPickupCancelled(Shield shield, PickupCancelContext context)
        {
            switch (context)
            {
                case PickupCancelContext.AlreadyPickedUp:
                {
                    notification.ShowWarn(shieldDroppedBecauseAlreadyPickedUp.Message);
                    break;
                }
            }
        }

        public override void OnDropShieldSettingChanged(bool nextDropShieldOnHit)
        {
            if (roleManager.GetPlayerRole().IsGameStaff())
            {
                notification.ShowInfo(nextDropShieldOnHit
                    ? dropShieldOnHitEnabled.Message
                    : dropShieldOnHitDisabled.Message
                );
            }
        }
    }
}