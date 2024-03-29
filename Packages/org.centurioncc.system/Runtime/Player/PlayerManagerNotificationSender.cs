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
        private TranslatableMessage changeToStaffTeamMessage;
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
        [SerializeField]
        private TranslatableMessage onFriendlyFireWarningMessage;
        [SerializeField]
        private TranslatableMessage onFriendlyFireModeChangedMessage;
        [SerializeField]
        private TranslatableMessage[] friendlyFireModes;

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
            else if (playerManager.IsStaffTeamId(player.TeamId))
                notification.ShowInfo(changeToStaffTeamMessage.Message);
            else
                notification.ShowInfo(string.Format(unknownTeamChangeNotificationMessage.Message, player.TeamId));
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

        public override void OnFriendlyFireModeChanged(FriendlyFireMode previousMode)
        {
            if ((int)playerManager.FriendlyFireMode >= 0 &&
                (int)playerManager.FriendlyFireMode < friendlyFireModes.Length)
            {
                notification.ShowInfo(
                    string.Format(onFriendlyFireModeChangedMessage.Message,
                        friendlyFireModes[(int)playerManager.FriendlyFireMode].Message,
                        5F,
                        978490789
                    )
                );
                return;
            }

            notification.ShowInfo(
                string.Format(onFriendlyFireModeChangedMessage.Message,
                    playerManager.FriendlyFireMode.ToEnumName()),
                5F,
                978490789 // string.GetHashCode() of "FRIENDLY_FIRE_MODE_CHANGE"
            );
        }

        public override void OnFriendlyFireWarning(PlayerBase victim, DamageData damageData, Vector3 contactPoint)
        {
            notification.ShowWarn(
                string.Format(onFriendlyFireWarningMessage.Message, playerManager.GetHumanFriendlyColoredName(victim)),
                5F,
                1325453321 + victim.PlayerId // string.GetHashCode() of "FRIENDLY_FIRE_WARNING" plus victim player id
            );
        }
    }
}