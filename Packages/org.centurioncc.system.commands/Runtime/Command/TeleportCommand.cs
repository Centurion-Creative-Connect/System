using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TeleportCommand : ActionCommandHandler
    {
        private const string PlayerNotFoundMessage = "<color=red>Player {0} does not exist.</color>";
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;
        [SerializeField]
        private TranslatableMessage onTeleportSuccessfulMessage;

        private NewbieConsole _console;

        private int _lastRequestVersion = -1;

        [UdonSynced]
        private int _opponentPlayerId;
        [UdonSynced]
        private int _requestVersion = -1;
        [UdonSynced]
        private int _toPlayerId;

        public override string Label => "Teleport";
        public override string[] Aliases => new[] { "Tp" };
        public override string Usage => "<command> <toPlayer> or <command> <opponentPlayer> <toPlayer>";
        public override string Description => "Teleports player to specified player.";

        private void Start()
        {
            _requestVersion = 0;
            _lastRequestVersion = 0;
        }

        public override void OnDeserialization()
        {
            DoTeleport();
        }

        public override void OnPreSerialization()
        {
            DoTeleport();
        }

        private void DoTeleport()
        {
            if (_console == null)
                return;

            if (_lastRequestVersion == -1)
            {
                _lastRequestVersion = _requestVersion;
                return;
            }

            var toPlayer = VRCPlayerApi.GetPlayerById(_toPlayerId);
            if (toPlayer == null || !toPlayer.IsValid())
            {
                _console.Println(
                    $"[TeleportCommand] Received invalid teleport command for {NewbieUtils.GetPlayerName(_opponentPlayerId)} to {NewbieUtils.GetPlayerName(_toPlayerId)}");
                return;
            }

            if (_opponentPlayerId != Networking.LocalPlayer.playerId)
            {
                _console.Println(
                    $"[TeleportCommand] Received teleport command for {NewbieUtils.GetPlayerName(_opponentPlayerId)} to {NewbieUtils.GetPlayerName(_toPlayerId)}.");
                return;
            }

            // LocalPlayer is opponent
            Networking.LocalPlayer.TeleportTo(
                toPlayer.GetPosition(),
                toPlayer.GetRotation(),
                VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                false);

            notification.ShowInfo(
                string.Format(onTeleportSuccessfulMessage.Message,
                    toPlayer.displayName));
            _console.Println(
                $"[TeleportCommand] Successfully teleported {NewbieUtils.GetPlayerName(Networking.LocalPlayer)} to {NewbieUtils.GetPlayerName(toPlayer)}!");
        }

        public override void OnActionCommand(NewbieConsole console, string label,
            ref string[] vars, ref string[] envVars)
        {
            // tp
            if (vars == null || vars.Length <= 0)
            {
                console.PrintUsage(this);
                return;
            }

            VRCPlayerApi opponentPlayer;
            VRCPlayerApi toPlayer;

            // tp <toPlayer>
            if (vars.Length == 1)
            {
                opponentPlayer = Networking.LocalPlayer;
                toPlayer = ConsoleParser.TryParsePlayer(vars[0]);

                if (toPlayer == null || !toPlayer.IsValid())
                {
                    console.Println(string.Format(PlayerNotFoundMessage, vars[0]));
                    return;
                }
            }
            else // ModOnly: tp <opponentPlayer> <toPlayer>
            {
                if (!console.IsSuperUser)
                {
                    console.Println("Cannot specify opponent player unless you're super user.");
                    return;
                }

                opponentPlayer = ConsoleParser.TryParsePlayer(vars[0]);
                toPlayer = ConsoleParser.TryParsePlayer(vars[1]);

                if (opponentPlayer == null || !opponentPlayer.IsValid())
                {
                    console.Println(string.Format(PlayerNotFoundMessage, vars[0]));
                    return;
                }

                if (toPlayer == null || !toPlayer.IsValid())
                {
                    console.Println(string.Format(PlayerNotFoundMessage, vars[1]));
                    return;
                }
            }

            _opponentPlayerId = opponentPlayer.playerId;
            _toPlayerId = toPlayer.playerId;
            ++_requestVersion;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();

            console.Println(
                $"Teleporting {NewbieUtils.GetPlayerName(opponentPlayer)} to {NewbieUtils.GetPlayerName(toPlayer)}...");
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
        }
    }
}