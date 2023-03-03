using CenturionCC.System.UI;
using DerpyNewbie.Logger;
using UdonSharp;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NotificationCommand : ActionCommandHandler
    {
        private NotificationUI _notification;

        public override string Label => "Notification";
        public override string[] Aliases => new[] { "Msg", "Pop" };
        public override string Usage => "<command> [info|warn|err] <msg>";
        public override string Description => "Show notification with custom message.";

        private void Start()
        {
            _notification = CenturionSystemReference.GetNotificationUI();
        }

        private static string MakeString(int begin, string[] args)
        {
            var result = "";
            for (var i = begin; i < args.Length; i++)
            {
                result += args[i];
                result += " ";
            }

            return result.Trim();
        }

        public override void OnActionCommand(NewbieConsole console, string label,
            ref string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
                return;

            switch (vars[0].Trim().ToLower())
            {
                case "i":
                case "info":
                    _notification.ShowInfo(MakeString(1, vars));
                    break;
                case "w":
                case "warn":
                case "warning":
                    _notification.ShowWarn(MakeString(1, vars));
                    break;
                case "e":
                case "err":
                case "error":
                    _notification.ShowError(MakeString(1, vars));
                    break;
                default:
                    _notification.ShowInfo(MakeString(0, vars));
                    break;
            }
        }
    }
}