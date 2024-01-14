using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeamPlayerCountText : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField]
        private Text text;
        [SerializeField] [TextArea]
        private string format = "<b>Players In Team</b>\n" +
                                "<color=grey>None</color>  : %non%\n" +
                                "<color=red>Red</color>   : %red%\n" +
                                "<color=yellow>Yellow</color>: %yel%\n";

        private void Start()
        {
            playerManager.SubscribeCallback(this);
            UpdateText();
            SendCustomEventDelayedSeconds(nameof(UpdateText), 10F);
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            UpdateText();
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            text.text = GetFormattedText(playerManager, format);
        }

        private static string GetFormattedText(PlayerManager playerManager, string format)
        {
            return format
                .Replace("%total%", playerManager.PlayerCount.ToString())
                .Replace("%non%", playerManager.NoneTeamPlayerCount.ToString())
                .Replace("%red%", playerManager.RedTeamPlayerCount.ToString())
                .Replace("%yel%", playerManager.YellowTeamPlayerCount.ToString())
                .Replace("%gre%", playerManager.GreenTeamPlayerCount.ToString())
                .Replace("%blu%", playerManager.BlueTeamPlayerCount.ToString());
        }
    }
}