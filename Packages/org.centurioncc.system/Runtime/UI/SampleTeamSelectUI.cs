using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleTeamSelectUI : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieConsole console;

        public void OnRedTeamButtonPressed()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null)
                return;

            SetTeam(localPlayer.TeamId != 1 ? 1 : 0);
        }

        public void OnYellowTeamButtonPressed()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null)
                return;

            SetTeam(localPlayer.TeamId != 2 ? 2 : 0);
        }

        private void SetTeam(int teamId)
        {
            console.Evaluate($"PlayerManager team {teamId}");
        }
    }
}