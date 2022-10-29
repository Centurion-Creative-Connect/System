using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using VRC.SDKBase;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ForceVoiceResetCommand : ActionCommandHandler
    {
        private NotificationUI _notification;

        public override string Label => "ForceVoiceReset";
        public override string Usage => "<command>";
        public override string Description => "Resets voice settings for users currently in instance";

        private void Start()
        {
            _notification = CenturionSystemReference.GetNotificationUI();
        }

        public override void OnActionCommand(NewbieConsole console, string label, ref string[] vars,
            ref string[] envVars)
        {
            console.Println("<color=red>Force resetting player voices!</color>");
            var playerApis = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            playerApis = VRCPlayerApi.GetPlayers(playerApis);

            foreach (var api in playerApis)
            {
                if (api == null || !api.IsValid()) continue;
                NewbieUtils.ResetVoice(api);
            }

            console.Println("Successfully cleared customized player voices");
            if (_notification)
                _notification.ShowInfo("All player voices has been reset.");
        }
    }
}