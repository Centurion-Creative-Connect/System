using CenturionCC.System.Player.HitDisplay;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Command.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerExternalCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [NewbieInject]
        private ExternalHitDisplayManager hitDisplayManager;

        public override string Label => "PlayerExternal";
        public override string[] Aliases => new[] { "PlayerExt", "PExt" };
        public override string Description => "Manipulate Players external information";
        public override string Usage => "<command> <hitDisplay> <option> [value]";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars.Length == 0)
                return console.PrintUsage(this);

            switch (vars[0].ToLower())
            {
                case "tag":
                    return "no-op";
                case "hitdisplay":
                    return HandleHitDisplaySubCommand(console, vars);
            }

            return console.PrintUsage(this);
        }

        private string HandleHitDisplaySubCommand(NewbieConsole console, string[] vars)
        {
            const string optionsString = "Options: clear";

            if (vars.Length == 1)
            {
                console.Println(optionsString);
                return ConsoleLiteral.GetNone();
            }

            switch (vars[1].ToLower())
            {
                case "clear":
                {
                    hitDisplayManager.Clear();
                    console.Println("Cleared hit display");
                    return ConsoleLiteral.GetTrue();
                }
            }

            console.Println(optionsString);
            return ConsoleLiteral.GetNone();
        }
    }
}
