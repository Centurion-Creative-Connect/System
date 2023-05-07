using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WatchdogCommand : ActionCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private WatchdogProc watchdogProc;

        public override string Label => "Watchdog";
        public override string Usage => "<command> clear";
        public override string Description => "Clears Watchdog crash screen (not recommended!)";

        public override void OnActionCommand(NewbieConsole console, string label, ref string[] vars,
            ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
            {
                console.PrintUsage(this);
                return;
            }

            if (!console.IsSuperUser)
            {
                console.Println("You are not allowed to use this command\n" +
                                "If you're here from Crash Screen, Enjoy your secret developer console! - Newbie");
                return;
            }

            switch (vars[0].ToLower())
            {
                case "clear":
                {
                    watchdogProc.errorCallback.OnInitialized(null);
                    console.Println("<color=green>Successfully cleaned up exception window</color>");
                    return;
                }
                default:
                    console.PrintUsage(this);
                    return;
            }
        }
    }
}