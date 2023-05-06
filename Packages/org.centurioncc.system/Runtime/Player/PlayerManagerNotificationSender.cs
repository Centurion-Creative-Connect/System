using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerManagerNotificationSender : PlayerManagerCallbackBase
    {
        [Header("Notification Messages")]
        [SerializeField]
        private TranslatableMessage[] teamChangeNotificationMessages;
        [SerializeField]
        private TranslatableMessage unknownTeamChangeNotificationMessage;
        [FormerlySerializedAs("onDisguiseEnabledMessage")]
        [SerializeField]
        private TranslatableMessage onStaffTagEnabledMessage;
        [FormerlySerializedAs("onDisguiseDisabledMessage")]
        [SerializeField]
        private TranslatableMessage onStaffTagDisabledMessage;
        [SerializeField]
        private TranslatableMessage onTeamTagEnabledMessage;
        [SerializeField]
        private TranslatableMessage onTeamTagDisabledMessage;

        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private void Start()
        {
            playerManager.SubscribeCallback(this);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            var vrcPlayer = player.VrcPlayer;
            if (vrcPlayer == null || !vrcPlayer.isLocal) return;

            if (player.TeamId >= 0 && player.TeamId < teamChangeNotificationMessages.Length)
                notification.ShowInfo(teamChangeNotificationMessages[player.TeamId].Message);
            else
                notification.ShowInfo(string.Format(unknownTeamChangeNotificationMessage.Message,
                    player.TeamId));
        }

        public override void OnPlayerTagChanged(TagType type, bool isOn)
        {
            switch (type)
            {
                case TagType.Debug:
                {
                    notification.ShowInfo($"Debug player info is now {(isOn ? "shown" : "hidden")}.");
                    break;
                }
                case TagType.Team:
                {
                    notification.ShowInfo(isOn
                        ? onTeamTagEnabledMessage.Message
                        : onTeamTagDisabledMessage.Message);
                    break;
                }
                case TagType.Master:
                case TagType.Owner:
                case TagType.Dev:
                case TagType.Staff:
                {
                    if (playerManager.RoleManager.GetPlayerRole().HasPermission())
                        notification.ShowInfo(isOn
                            ? onStaffTagEnabledMessage.Message
                            : onStaffTagDisabledMessage.Message);
                    break;
                }
                default:
                {
                    Debug.Log($"[PMNotificationSender] Unknown tag type was provided: {type}");
                    break;
                }
            }
        }
    }
}