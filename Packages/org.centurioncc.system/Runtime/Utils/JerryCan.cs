using CenturionCC.System.Player;
using CenturionCC.System.UI;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class JerryCan : PlayerManagerCallbackBase
    {
        private const float PickupCooldownTime = 10F;
        [SerializeField]
        private VRCPickup pickup;
        [SerializeField]
        private TranslatableMessage dropMessage;

        private NotificationUI _notificationUI;

        private void Start()
        {
            if (pickup == null)
                pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            if (_notificationUI == null)
                _notificationUI = CenturionSystemReference.GetNotificationUI();

            CenturionSystemReference.GetPlayerManager().SubscribeCallback(this);
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (pickup.currentPlayer == hitPlayer.VrcPlayer && hitPlayer.IsLocal)
                Drop();
        }

        private void Drop()
        {
            pickup.Drop();
            if (_notificationUI != null && dropMessage != null)
                _notificationUI.ShowWarn(dropMessage.Message);
            SendCustomEventDelayedSeconds(nameof(MakePickupable), PickupCooldownTime);
        }

        public void MakePickupable()
        {
            pickup.pickupable = true;
        }
    }
}