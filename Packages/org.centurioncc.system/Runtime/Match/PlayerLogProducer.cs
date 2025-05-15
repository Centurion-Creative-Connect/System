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
        private PlayerManager playerManager;

        [SerializeField] [NewbieInject]
        private GameMatchHandler matchHandler;

        private void OnEnable()
        {
            playerManager.SubscribeCallback(this);
        }

        private void OnDisable()
        {
            playerManager.UnsubscribeCallback(this);
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            matchHandler.EnsureStatsTableExist(player);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeamId)
        {
            matchHandler.UpdateTeam(player);
        }

        public override void OnKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            matchHandler.AddMatchEventLog("killed", victim.LastHitData.ToDictionary());

            var distance = victim.LastHitData.Distance;

            matchHandler.IncrementDeath(victim);
            matchHandler.IncrementKill(attacker, victim.LastHitData.WeaponType, distance);
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