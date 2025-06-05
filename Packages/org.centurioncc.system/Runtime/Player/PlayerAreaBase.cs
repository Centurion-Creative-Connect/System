using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class PlayerAreaBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected PlayerManagerBase playerManager;

        public abstract string AreaName { get; }
        public abstract bool IsSafeZone { get; }

        public abstract PlayerBase[] GetPlayersInArea();
    }
}