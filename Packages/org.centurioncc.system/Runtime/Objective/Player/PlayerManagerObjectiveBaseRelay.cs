using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Objective.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlayerManagerObjectiveBaseRelay : PlayerManagerCallbackBase
    {
        [Header("Required by PlayerManager related Objectives")]
        [SerializeField] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private PlayerManagerObjectiveBase[] playerManagerObjectives;

        private void OnEnable()
        {
            playerManager.SubscribeCallback(this);
        }

        private void OnDisable()
        {
            playerManager.UnsubscribeCallback(this);
        }

        public override void OnKilled(PlayerBase attcker, PlayerBase victim, KillType type)
        {
            foreach (var pmObjective in playerManagerObjectives)
            {
                if (pmObjective) pmObjective.OnPlayerKilled(attcker, victim, type);
            }
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            foreach (var pmObjective in playerManagerObjectives)
            {
                if (pmObjective) pmObjective.OnPlayerRevived(player);
            }
        }
    }
}