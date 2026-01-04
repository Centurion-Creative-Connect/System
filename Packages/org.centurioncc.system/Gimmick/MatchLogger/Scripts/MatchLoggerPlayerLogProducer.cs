using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.MatchLogger
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MatchLoggerPlayerLogProducer : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private MatchLogger matchLogger;

        private void OnEnable()
        {
            playerManager.Subscribe(this);
        }

        private void OnDisable()
        {
            playerManager.Unsubscribe(this);
        }

        public override void OnPlayerAdded(PlayerBase player)
        {
            matchLogger.EnsureStatsTableExist(player);
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeamId)
        {
            matchLogger.UpdateTeam(player);
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            matchLogger.AddMatchEventLog("killed", victim.LastDamageInfo.ToDictionary());

            var originPos = victim.LastDamageInfo.OriginatedPosition();
            var hitPos = victim.LastDamageInfo.HitPosition();
            var distance = Vector3.Distance(originPos, hitPos);

            matchLogger.IncrementDeath(victim);
            matchLogger.IncrementKill(attacker, victim.LastDamageInfo.DamageType(), distance);
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            var dict = new DataDictionary();
            dict.Add("id", player.PlayerId);
            dict.Add("name", VRCPlayerApi.GetPlayerById(player.PlayerId).SafeGetDisplayName());
            dict.Add("time", Networking.GetNetworkDateTime().ToString("O"));
            if (Utilities.IsValid(player.VrcPlayer) && player.VrcPlayer != null)
                dict.Add("position", player.VrcPlayer.GetPosition().ToDictionary());

            matchLogger.AddMatchEventLog("revived", dict);
        }
    }
}
