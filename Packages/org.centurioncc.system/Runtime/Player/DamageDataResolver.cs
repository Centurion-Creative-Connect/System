using System;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DamageDataResolver : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private VRCPlayerApi _local;

        private void Start()
        {
            _local = Networking.LocalPlayer;
            playerManager.SubscribeCallback(this);
        }

        public SyncResult Resolve(DamageDataSyncer syncer)
        {
            if (syncer.VictimId != _local.playerId)
                return SyncResult.Unassigned;

            var victim = playerManager.GetPlayerById(syncer.VictimId);
            var attacker = playerManager.GetPlayerById(syncer.AttackerId);
            if (victim == null || attacker == null || victim.IsDead)
                return SyncResult.Cancelled;

            var result = ComputeHitResultFromDateTime(
                syncer.ActivatedTime,
                GetAssumedDiedTime(syncer.AttackerId),
                GetAssumedRevivedTime(syncer.AttackerId)
            );

            if (!result) return SyncResult.Cancelled;

            if (syncer.Type == KillType.FriendlyFire && playerManager.FriendlyFireMode == FriendlyFireMode.Both)
            {
                attacker.LastHitData.SetData(syncer);
                attacker.LastHitData.SyncData();
            }

            var lastHitData = syncer.Type == KillType.ReverseFriendlyFire ? attacker.LastHitData : victim.LastHitData;
            lastHitData.SetData(syncer);
            lastHitData.SyncData();
            return SyncResult.Hit;
        }

        [PublicAPI]
        public static bool ComputeHitResultFromDateTime(
            DateTime damageOriginatedTime,
            DateTime attackerDiedTime,
            DateTime attackerRevivedTime)
        {
            // Handle edge case where player was instantly revived 
            if (attackerRevivedTime == attackerDiedTime) return true;

            if (attackerDiedTime < attackerRevivedTime)
                return damageOriginatedTime <= attackerDiedTime || damageOriginatedTime >= attackerRevivedTime;

            return damageOriginatedTime <= attackerDiedTime && damageOriginatedTime >= attackerRevivedTime;
        }

        #region Callbacks

        public override void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint)
        {
            if (playerCollider == null || damageData == null)
                return;

            var victim = playerCollider.player;
            var victimId = victim.PlayerId;
            var attackerId = damageData.DamagerPlayerId;
            var attacker = playerManager.GetPlayerById(attackerId);

            // Do not process if local was not associated with it
            if (victimId != _local.playerId && attackerId != _local.playerId)
                return;

            if (victim.IsDead)
                return;

            var hitResult = ComputeHitResultFromDateTime(
                damageData.DamageOriginTime,
                attacker.LastHitData.HitTime,
                GetAssumedRevivedTime(attackerId)
            );

            if (!hitResult)
                return;

            SetAssumedDiedTime(victimId, Networking.GetNetworkDateTime());
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            if (player == null) return;

            SetAssumedRevivedTime(player.PlayerId, Networking.GetNetworkDateTime());
        }

        public override void OnResetPlayerStats(PlayerBase player)
        {
            if (player == null) return;

            _assumedDiedTimeDict.Remove(player.PlayerId);
            _assumedRevivedTimeDict.Remove(player.PlayerId);
        }

        public override void OnResetAllPlayerStats()
        {
            _assumedDiedTimeDict.Clear();
            _assumedRevivedTimeDict.Clear();
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            _assumedDiedTimeDict.Remove(oldId);
            _assumedDiedTimeDict.Remove(newId);

            _assumedRevivedTimeDict.Remove(oldId);
            _assumedRevivedTimeDict.Remove(newId);
        }

        #endregion

        #region DiedTimeGetterSetter

        private readonly DataDictionary _assumedDiedTimeDict = new DataDictionary();
        private readonly DataDictionary _assumedRevivedTimeDict = new DataDictionary();

        [PublicAPI]
        public DateTime GetAssumedDiedTime(int playerId)
        {
            return _assumedDiedTimeDict.TryGetValue(new DataToken(playerId), TokenType.Long, out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        private void SetAssumedDiedTime(int playerId, DateTime time)
        {
            _assumedDiedTimeDict.SetValue(playerId, time.Ticks);
        }

        [PublicAPI]
        public DateTime GetAssumedRevivedTime(int playerId)
        {
            return _assumedRevivedTimeDict.TryGetValue(new DataToken(playerId), TokenType.Long, out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        private void SetAssumedRevivedTime(int playerId, DateTime time)
        {
            _assumedRevivedTimeDict.SetValue(playerId, time.Ticks);
        }

        #endregion

        #region WatchdogProc

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }

        #endregion
    }
}