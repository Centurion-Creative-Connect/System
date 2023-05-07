using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerUpdaterCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerUpdater updater;

        public override string Label => "PlayerUpdater";
        public override string Usage => "<command> <list|maxSortStep> [value]";

        public override string OnCommand(NewbieConsole console, string label, string[] vars,
            ref string[] envVars)
        {
            if (vars.Length == 0)
            {
                console.PrintUsage(this);
                return ConsoleLiteral.GetNone();
            }

            switch (vars[0].ToLower())
            {
                case "list":
                    var listStr = updater.GetOrder();
                    console.Print(listStr);
                    return listStr;
                case "maxsortstep":
                    if (vars.Length > 1)
                    {
                        var i = ConsoleParser.TryParseInt(vars[1]);
                        if (i < 1 || i > updater.ModelCount - 2)
                        {
                            console.Println($"{i} exceeds capable range of 1 to {updater.ModelCount - 2}!");
                            return ConsoleLiteral.GetNone();
                        }

                        updater.sortStepCount = i;
                    }

                    console.Println($"Max Sort Step: {updater.sortStepCount}");
                    return ConsoleLiteral.Of(updater.sortStepCount);
                default:
                    console.PrintUsage(this);
                    return ConsoleLiteral.GetNone();
            }
        }
    }
}