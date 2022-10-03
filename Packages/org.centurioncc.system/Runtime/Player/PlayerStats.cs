using System;
using JetBrains.Annotations;
using UdonSharp;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerStats : UdonSharpBehaviour
    {
        [UdonSynced] [FieldChangeCallback(nameof(Death))]
        private int _death = -1;
        [UdonSynced]
        private long _lastHitTimeLong = DateTime.MinValue.Ticks;
        private PlayerManager _manager;

        [PublicAPI]
        public ShooterPlayer Player { get; private set; }

        [PublicAPI]
        public int Kill { get; set; }

        [PublicAPI]
        public int Death
        {
            get => _death;
            set
            {
                var lastDeath = _death;
                _death = value;

                if (lastDeath == -1 || value == 0)
                    return;
            }
        }

        [PublicAPI]
        public DateTime LastHitTime
        {
            get => new DateTime(_lastHitTimeLong);
            set => _lastHitTimeLong = value.Ticks;
        }

        [PublicAPI] [field: UdonSynced]
        public int LastDamagerPlayerId { get; set; }

        #region LocalProperty

        private int _antiCheatSuspicionLevel;

        [PublicAPI]
        public DateTime AntiCheatLastSuspicionChangedTime { get; private set; }

        [PublicAPI]
        public int AntiCheatSuspicionLevel
        {
            get => _antiCheatSuspicionLevel;
            set
            {
                AntiCheatLastSuspicionChangedTime = DateTime.Now;
                _antiCheatSuspicionLevel = value;
            }
        }

        #endregion

        //
        // public override void OnDeserialization()
        // {
        //     _CheckDiff();
        // }
        //
        // public override void OnPreSerialization()
        // {
        //     _CheckDiff();
        // }
        //
        // private void _CheckDiff()
        // {
        //     // if (_invokeOnDeathNextOnDeserialization)
        //     // {
        //     //     _invokeOnDeathNextOnDeserialization = false;
        //     //     var firedPlayer = _manager.GetShooterPlayerByPlayerId(LastDamagerPlayerId);
        //     //     if (firedPlayer == null)
        //     //     {
        //     //         _manager.Logger.LogError(
        //     //             $"[PlayerStats-{Player.name}] _CheckDiff: Failed to get fired player for {GameManager.GetPlayerName(Player.VrcPlayer)}, {LastDamagerPlayerId}!");
        //     //         return;
        //     //     }
        //     //
        //     //     firedPlayer.PlayerStats.Kill++;
        //     //
        //     //     _manager.Invoke_OnKilled(firedPlayer, Player);
        //     // }
        // }
        //
        // public void Init(ShooterPlayer player, PlayerManager manager)
        // {
        //     Player = player;
        //     _manager = manager;
        // }
        //
        // public void ResetStats()
        // {
        //     Kill = 0;
        //     Death = 0;
        //     LastDamagerPlayerId = -1;
        //     LastHitTime = DateTime.MinValue;
        //
        //     _manager.Invoke_OnResetPlayerStats(Player);
        // }
        //
        // public void Sync()
        // {
        //     Networking.SetOwner(Networking.LocalPlayer, gameObject);
        //     RequestSerialization();
        // }
    }
}