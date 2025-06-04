using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerManagerNotificationSender : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private NotificationProvider notification;

        [SerializeField] [NewbieInject]
        private RoleProvider roleProvider;


        [Header("Team Change Messages")] [SerializeField]
        private TranslatableMessage[] teamChangeNotificationMessages;

        [SerializeField] private TranslatableMessage changeToStaffTeamMessage;
        [SerializeField] private TranslatableMessage unknownTeamChangeNotificationMessage;

        [Header("Staff Tag Messages")] [FormerlySerializedAs("onDisguiseEnabledMessage")] [SerializeField]
        private TranslatableMessage onStaffTagEnabledMessage;

        [FormerlySerializedAs("onDisguiseDisabledMessage")] [SerializeField]
        private TranslatableMessage onStaffTagDisabledMessage;

        [Header("Team Tag Messages")] [SerializeField]
        private TranslatableMessage onTeamTagEnabledMessage;

        [SerializeField] private TranslatableMessage onTeamTagDisabledMessage;

        [Header("Friendly Fire Messages")] [SerializeField]
        private TranslatableMessage onFriendlyFireWarningMessage;

        [SerializeField] private TranslatableMessage onFriendlyFireOccurredMessage;
        [SerializeField] private TranslatableMessage onReverseFriendlyFireOccurredMessage;

        [Header("Friendly Fire Mode Changes Messages")] [SerializeField]
        private TranslatableMessage onFriendlyFireModeChangedMessage;

        [SerializeField] private TranslatableMessage[] friendlyFireModes;

        private void Start()
        {
            playerManager.Subscribe(this);
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
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
                    if (roleProvider.GetPlayerRoles().HasPermission())
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

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            string fmt;
            PlayerBase target;
            switch (type)
            {
                case KillType.Default:
                default:
                    return;
                case KillType.FriendlyFire:
                {
                    if (!attacker.IsLocal || onFriendlyFireOccurredMessage == null ||
                        playerManager.FriendlyFireMode == FriendlyFireMode.Both) return;

                    fmt = onFriendlyFireOccurredMessage.Message;
                    target = victim;
                    break;
                }
                case KillType.ReverseFriendlyFire:
                {
                    if (!attacker.IsLocal || onReverseFriendlyFireOccurredMessage == null) return;

                    fmt = onReverseFriendlyFireOccurredMessage.Message;
                    target = victim;
                    break;
                }
            }

            notification.ShowError(
                string.Format(fmt, target.DisplayName),
                5F,
                1325453321 + victim.PlayerId // string.GetHashCode() of "FRIENDLY_FIRE_WARNING" plus victim player id
            );
        }

        public override void OnPlayerFriendlyFireWarning(PlayerBase victim, DamageInfo damageInfo)
        {
            if (onFriendlyFireWarningMessage == null) return;

            notification.ShowWarn(
                string.Format(onFriendlyFireWarningMessage.Message, victim.DisplayName),
                5F,
                1325453321 + victim.PlayerId // string.GetHashCode() of "FRIENDLY_FIRE_WARNING" plus victim player id
            );
        }
    }
}