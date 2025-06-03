using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleTeamSelectUI : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieConsole console;

        [SerializeField]
        private int customTeamId;

        [PublicAPI]
        public void OnRedTeamButtonPressed()
        {
            ButtonPress(1);
        }

        [PublicAPI]
        public void OnYellowTeamButtonPressed()
        {
            ButtonPress(2);
        }

        [PublicAPI]
        public void OnStaffTeamButtonPressed()
        {
            ButtonPress(255);
        }

        [PublicAPI]
        public void OnCustomTeamButtonPressed()
        {
            ButtonPress(customTeamId);
        }

        private void ButtonPress(int destTeamId)
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null) return;

            SetTeam(localPlayer.TeamId != destTeamId ? destTeamId : 0);
        }

        private void SetTeam(int teamId)
        {
            console.Evaluate($"PlayerManager team {teamId}");
        }
    }
}