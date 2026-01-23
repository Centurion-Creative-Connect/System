using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Command.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GetGamePlayerCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        public override string Label => "GetGamePlayer";
        public override string Description => "Gets game player information.";
        public override string Usage => "<command> [-remote=playerId] <team|kill|death>";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            var playerId = Networking.LocalPlayer.playerId;
            foreach (var s in vars)
            {
                if (!s.StartsWith("-remote="))
                    continue;
                playerId = ConsoleParser.TryParseInt(s.Substring(8));
                vars = vars.RemoveItem(s);
                // Break after replacing vars to avoid out-of-index access in foreach
                break;
            }

            var playerBase = playerManager.GetPlayerById(playerId);
            if (playerBase == null)
            {
                console.Println("Such player does not exist in game");
                return ConsoleLiteral.GetNone();
            }

            if (vars.Length == 0)
            {
                return console.PrintUsage(this);
            }

            switch (vars[0])
            {
                case "index":
                    return "no-op";
                case "team":
                    return ConsoleLiteral.Of(playerBase.TeamId);
                case "kill":
                    return ConsoleLiteral.Of(playerBase.Kills);
                case "death":
                    return ConsoleLiteral.Of(playerBase.Deaths);
            }

            return console.PrintUsage(this);
        }
    }
}
