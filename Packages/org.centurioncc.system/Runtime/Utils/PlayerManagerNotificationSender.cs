using CenturionCC.System.Player;
using CenturionCC.System.UI;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

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
        [SerializeField]
        private TranslatableMessage onDisguiseEnabledMessage;
        [SerializeField]
        private TranslatableMessage onDisguiseDisabledMessage;
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

        public override void OnTeamChanged(ShooterPlayer player, int oldTeam)
        {
            var vrcPlayer = player.VrcPlayer;
            if (vrcPlayer == null || !vrcPlayer.isLocal) return;

            if (player.Team >= 0 && player.Team < teamChangeNotificationMessages.Length)
                _notification.ShowInfo(teamChangeNotificationMessages[player.Team].Message);
            else
                _notification.ShowInfo(string.Format(unknownTeamChangeNotificationMessage.Message,
                    player.Team));
        }

        public override void OnPlayerTagChanged(ShooterPlayer player, TagType type, bool isOn)
        {
            var vrcPlayer = player.VrcPlayer;
            if (vrcPlayer == null || !vrcPlayer.isLocal) return;

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
                case TagType.Staff:
                {
                    _notification.ShowInfo(isOn
                        ? onDisguiseEnabledMessage.Message
                        : onDisguiseDisabledMessage.Message);
                    break;
                }
            }
        }
    }
}