using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Objective.Player
{
    [RequireComponent(typeof(PlayerManagerObjectiveBaseRelay))]
    public abstract class PlayerManagerObjectiveBase : ObjectiveBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManager playerManager;

        public virtual void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
        }

        public virtual void OnPlayerRevived(PlayerBase player)
        {
        }
    }
}