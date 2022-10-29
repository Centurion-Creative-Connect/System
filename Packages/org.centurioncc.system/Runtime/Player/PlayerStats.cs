using System;
using JetBrains.Annotations;
using UdonSharp;

namespace CenturionCC.System.Player
{
    [Obsolete("This class is no longer used by ShooterPlayer class. Directly get stats from PlayerBase instead.")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerStats : UdonSharpBehaviour
    {
        [PublicAPI]
        public ShooterPlayer Player { get; private set; }

        [PublicAPI]
        public int Kill { get; set; }

        [PublicAPI]
        public int Death { get; set; }

        [PublicAPI]
        public DateTime LastHitTime { get; set; }

        [PublicAPI] [field: UdonSynced]
        public int LastDamagerPlayerId { get; set; }

        #region LocalProperty

        [PublicAPI]
        public DateTime AntiCheatLastSuspicionChangedTime { get; private set; }

        [PublicAPI]
        public int AntiCheatSuspicionLevel { get; set; }

        #endregion
    }
}