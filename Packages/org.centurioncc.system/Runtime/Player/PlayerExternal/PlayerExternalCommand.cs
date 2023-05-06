using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player.PlayerExternal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerExternalCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private ExternalPlayerTagManager playerTagManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private ExternalHitDisplayManager hitDisplayManager;

        public override string Label => "PlayerExternal";
        public override string[] Aliases => new[] { "PlayerExt", "PExt" };
        public override string Description => "Manipulate Players external information";
        public override string Usage => "<command> <tag|hitDisplay> <option> [value]";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars.Length == 0)
                return console.PrintUsage(this);

            switch (vars[0].ToLower())
            {
                case "tag":
                    return HandleTagSubCommand(console, vars);
                case "hitdisplay":
                    return HandleHitDisplaySubCommand(console, vars);
            }

            return console.PrintUsage(this);
        }

        private string HandleTagSubCommand(NewbieConsole console, string[] vars)
        {
            const string optionsString = "Options: clear, reconstruct, update";

            if (vars.Length == 1)
            {
                console.Println(optionsString);
                return ConsoleLiteral.GetNone();
            }

            switch (vars[1].ToLower())
            {
                case "clear":
                {
                    playerTagManager.ClearTag();
                    console.Println("Cleared player tag");
                    return ConsoleLiteral.GetTrue();
                }
                case "reconstruct":
                {
                    playerTagManager.ReconstructTag();
                    console.Println("Reconstructed player tag");
                    return ConsoleLiteral.GetTrue();
                }
                case "update":
                {
                    playerTagManager.UpdateTag();
                    console.Println("Updated player tag");
                    return ConsoleLiteral.GetTrue();
                }
            }

            console.Println(optionsString);
            return ConsoleLiteral.GetNone();
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