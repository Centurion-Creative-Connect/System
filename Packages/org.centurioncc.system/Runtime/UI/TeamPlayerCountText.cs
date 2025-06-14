﻿using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeamPlayerCountText : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField]
        private Text text;

        [SerializeField] [TextArea]
        private string format = "<b>Players In Team</b>\n" +
                                "<color=grey>None</color>  : %non%\n" +
                                "<color=red>Red</color>   : %red%\n" +
                                "<color=yellow>Yellow</color>: %yel%\n";

        private void Start()
        {
            playerManager.Subscribe(this);
            UpdateText();
            SendCustomEventDelayedSeconds(nameof(UpdateText), 10F);
        }

        public override void OnPlayerAdded(PlayerBase player)
        {
            UpdateText();
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            text.text = GetFormattedText(playerManager, format);
        }

        private static string GetFormattedText(PlayerManagerBase playerManager, string format)
        {
            return format
                .Replace("%total%", VRCPlayerApi.GetPlayerCount().ToString())
                .Replace("%non%", playerManager.GetPlayersInTeam(0).Length.ToString())
                .Replace("%red%", playerManager.GetPlayersInTeam(1).Length.ToString())
                .Replace("%yel%", playerManager.GetPlayersInTeam(2).Length.ToString())
                .Replace("%gre%", playerManager.GetPlayersInTeam(3).Length.ToString())
                .Replace("%blu%", playerManager.GetPlayersInTeam(4).Length.ToString());
        }
    }
}