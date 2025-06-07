using System;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Match
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerLogProducer : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private GameMatchHandler matchHandler;

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
            matchHandler.EnsureStatsTableExist(player);
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeamId)
        {
            matchHandler.UpdateTeam(player);
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            matchHandler.AddMatchEventLog("killed", victim.LastDamageInfo.ToDictionary());

            var distance = Vector3.Distance(victim.LastDamageInfo.HitPosition(),
                victim.LastDamageInfo.OriginatedPosition());

            matchHandler.IncrementDeath(victim);
            matchHandler.IncrementKill(attacker, victim.LastDamageInfo.DamageType(), distance);
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            var dict = new DataDictionary();
            dict.Add("id", player.PlayerId);
            dict.Add("name", VRCPlayerApi.GetPlayerById(player.PlayerId).SafeGetDisplayName());
            dict.Add("time", Networking.GetNetworkDateTime().ToString("O"));
            if (Utilities.IsValid(player.VrcPlayer) && player.VrcPlayer != null)
                dict.Add("position", player.VrcPlayer.GetPosition().ToDictionary());

            matchHandler.AddMatchEventLog("revived", dict);
        }
    }
}