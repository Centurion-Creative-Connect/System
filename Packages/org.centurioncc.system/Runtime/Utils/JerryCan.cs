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
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notificationProvider;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField]
        private VRCPickup pickup;
        [SerializeField]
        private TranslatableMessage dropMessage;

        private void Start()
        {
            if (pickup == null)
                pickup = (VRCPickup)GetComponent(typeof(VRCPickup));

            playerManager.SubscribeCallback(this);
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (pickup.currentPlayer == hitPlayer.VrcPlayer && hitPlayer.IsLocal)
                Drop();
        }

        private void Drop()
        {
            pickup.Drop();
            if (notificationProvider != null && dropMessage != null)
                notificationProvider.ShowWarn(dropMessage.Message);
            SendCustomEventDelayedSeconds(nameof(MakePickupable), PickupCooldownTime);
        }

        public void MakePickupable()
        {
            pickup.pickupable = true;
        }
    }
}