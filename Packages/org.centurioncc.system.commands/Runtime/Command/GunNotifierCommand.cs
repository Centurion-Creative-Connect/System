using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunNotifierCommand : BoolCommandHandler
    {
        [SerializeField] [NewbieInject] [HideInInspector]
        private GunManagerNotificationSender notificationSender;

        public override string Label => "GunNotification";
        public override string Usage => "<command> [true|false]";
        public override string Description => "Sets on/off notification for GunManager.";

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (vars != null && vars.Length >= 1)
                notificationSender.notifyCancelled =
                    ConsoleParser.TryParseBoolean(vars[0], notificationSender.notifyCancelled);

            console.Println($"{label}: {notificationSender.notifyCancelled}");
            return notificationSender.notifyCancelled;
        }
    }
}