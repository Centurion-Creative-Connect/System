using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.MassGun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunUpdaterCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunUpdater updater;

        public override string Label => "GunUpdater";
        public override string Usage => "<command> <list|maxSortStep|maxUpdateInstances> [value]";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
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
                case "maxupdateinstances":
                    if (vars.Length > 1)
                    {
                        var i = ConsoleParser.TryParseInt(vars[1]);
                        if (i < 1 || i > updater.ModelCount)
                        {
                            console.Println($"{i} exceeds capable range of 1 to {updater.ModelCount}!");
                            return ConsoleLiteral.GetNone();
                        }

                        updater.maxInstancesToUpdateFrequently = i;
                    }

                    console.Println($"Max Update Instances: {updater.maxInstancesToUpdateFrequently}");
                    return ConsoleLiteral.Of(updater.maxInstancesToUpdateFrequently);
                default:
                    console.PrintUsage(this);
                    return ConsoleLiteral.GetNone();
            }
        }
    }
}