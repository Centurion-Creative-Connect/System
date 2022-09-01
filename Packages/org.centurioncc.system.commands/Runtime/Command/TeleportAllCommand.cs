using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TeleportAllCommand : ActionCommandHandler
    {
        [SerializeField]
        private TranslatableMessage willBeTeleportedMessage;
        [SerializeField]
        private TranslatableMessage teleportSuccessfulMessage;
        [SerializeField]
        private TranslatableMessage teleportDestinationNotFoundMessage;

        [UdonSynced]
        private int _toPlayerId;

        private NewbieConsole _console;
        private NotificationUI _notification;

        private string PlayerNotFoundMessage => "<color=red>Player {0} does not exist.</color>";

        private void Start()
        {
            _notification = GameManagerHelper.GetNotificationUI();
        }

        public void SendBeforeTeleportNotification()
        {
            _notification.ShowInfo(willBeTeleportedMessage.Message);
        }

        public void TeleportAll()
        {
            _console.Println("[TeleportAllCommand] Teleporting!");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(DoTeleport));
        }

        public void DoTeleport()
        {
            var toPlayer = VRCPlayerApi.GetPlayerById(_toPlayerId);
            if (toPlayer == null || !toPlayer.IsValid())
            {
                _notification.ShowError(teleportDestinationNotFoundMessage.Message);
                _console.Println($"[TeleportAllCommand] Teleport destination player {_toPlayerId} not found!");
                return;
            }

            Networking.LocalPlayer.TeleportTo(
                toPlayer.GetPosition(),
                toPlayer.GetRotation(),
                VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                false);
            _notification.ShowInfo(string.Format(teleportSuccessfulMessage.Message, toPlayer.displayName));
            _console.Println(
                $"[TeleportAllCommand] Successfully teleported you to {NewbieUtils.GetPlayerName(toPlayer)}");
        }

        public override string Label => "TeleportAll";
        public override string[] Aliases => new[] { "TpAll" };
        public override string Usage => "<command> [toPlayer]";
        public override string Description => "Teleports all player to self or specified player";

        public override void OnActionCommand(NewbieConsole console, string label, ref string[] vars,
            ref string[] envVars)
        {
            if (!console.IsSuperUser)
            {
                console.Println("You are not allowed to execute this command");
                return;
            }

            var toPlayer = Networking.LocalPlayer;
            if (vars.Length > 0)
            {
                toPlayer = ConsoleParser.TryParsePlayer(vars[0]);
                if (toPlayer == null || !toPlayer.IsValid())
                {
                    console.Println(string.Format(PlayerNotFoundMessage, vars[0]));
                    return;
                }
            }

            _toPlayerId = toPlayer.playerId;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            console.Println($"Teleporting everyone to {NewbieUtils.GetPlayerName(_toPlayerId)} in 5 seconds...");
            SendCustomEventDelayedSeconds(nameof(TeleportAll), 5F);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SendBeforeTeleportNotification));
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
        }
    }
}