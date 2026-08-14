using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class PlayerAreaBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected PlayerManagerBase playerManager;

        [PublicAPI]
        public abstract string AreaName { get; }

        [PublicAPI]
        public abstract bool IsSafeZone { get; }

        [PublicAPI]
        public abstract PlayerBase[] GetPlayersInArea();

        [PublicAPI("1.1.0")]
        public abstract bool IsInside(Vector3 position);
    }
}
