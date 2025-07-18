using CenturionCC.System.Player;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Objective.Player
{
    public class EliminationObjective : PlayerManagerObjectiveBase
    {
        [SerializeField] [UdonSynced]
        private int targetTeamId;

        public int TargetTeamId
        {
            get => targetTeamId;
            private set
            {
                targetTeamId = value;
                UpdateProgress();
            }
        }

        protected override void OnObjectiveStart()
        {
            UpdateProgress();
        }

        protected override void OnObjectiveResume()
        {
            UpdateProgress();
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            UpdateProgress();
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            UpdateProgress();
        }

        [PublicAPI]
        public void SetTargetTeamId(int teamId)
        {
            TargetTeamId = teamId;
            RequestSync();
        }

        private void UpdateProgress()
        {
            if (!Networking.IsMaster) return;
            if (!IsActiveAndRunning) return;

            var teamPlayersCount = playerManager.GetPlayersInTeam(TargetTeamId).Length;
            var deadTeamPlayersCount = playerManager.GetDeadPlayersInTeam(TargetTeamId).Length;
            SetProgress(deadTeamPlayersCount / (float)teamPlayersCount);
        }
    }
}