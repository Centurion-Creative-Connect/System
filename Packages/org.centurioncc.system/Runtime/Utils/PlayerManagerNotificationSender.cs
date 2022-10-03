using CenturionCC.System.Player;
using CenturionCC.System.UI;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Utils
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

        private NotificationUI _notification;
        private PlayerManager _playerManager;

        private void Start()
        {
            var gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            _notification = gameManager.notification;
            _playerManager = gameManager.players;
            _playerManager.SubscribeCallback(this);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            var vrcPlayer = player.VrcPlayer;
            if (vrcPlayer == null || !vrcPlayer.isLocal) return;

            if (player.TeamId >= 0 && player.TeamId < teamChangeNotificationMessages.Length)
                _notification.ShowInfo(teamChangeNotificationMessages[player.TeamId].Message);
            else
                _notification.ShowInfo(string.Format(unknownTeamChangeNotificationMessage.Message,
                    player.TeamId));
        }

        public override void OnPlayerTagChanged(TagType type, bool isOn)
        {
            switch (type)
            {
                case TagType.Debug:
                {
                    _notification.ShowInfo($"Debug player info is now {(isOn ? "shown" : "hidden")}.");
                    break;
                }
                case TagType.Team:
                {
                    _notification.ShowInfo(isOn
                        ? onTeamTagEnabledMessage.Message
                        : onTeamTagDisabledMessage.Message);
                    break;
                }
                case TagType.Master:
                case TagType.Owner:
                case TagType.Dev:
                case TagType.Staff:
                {
                    if (_playerManager.RoleManager.GetPlayerRole().HasPermission())
                        _notification.ShowInfo(isOn
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