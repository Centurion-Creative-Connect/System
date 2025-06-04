using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Objective.Player
{
    public class EliminationObjective : PlayerManagerObjectiveBase
    {
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

        private void UpdateProgress()
        {
        }
    }
}