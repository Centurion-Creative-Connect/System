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
    public class DamageDataResolver : DamageDataSyncerManagerCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private DamageDataSyncerManager damageDataSyncerManager;

        private VRCPlayerApi _local;

        [PublicAPI]
        public double InvincibleDuration { get; set; } = 10D;

        private void Start()
        {
            _local = Networking.LocalPlayer;
            playerManager.SubscribeCallback(this);
            damageDataSyncerManager.SubscribeCallback(this);
        }

        [PublicAPI]
        public bool ComputeHitResultFromDateTime(
            DateTime damageOriginatedTime,
            DateTime hitTime,
            DateTime attackerDiedTime,
            DateTime victimDiedTime
        )
        {
            return damageOriginatedTime <= hitTime &&
                   (!IsInInvincibleDuration(damageOriginatedTime, attackerDiedTime) &&
                    !IsInInvincibleDuration(hitTime, victimDiedTime));
        }

        [PublicAPI]
        public bool IsInInvincibleDuration(DateTime now, DateTime diedTime)
        {
            var diff = now.Subtract(diedTime).TotalSeconds;
            return diff < InvincibleDuration && diff >= 0;
        }

        #region Callbacks

        public override void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint)
        {
            if (playerCollider == null || damageData == null)
                return;

            var now = Networking.GetNetworkDateTime();
            var victim = playerCollider.player;
            var victimId = victim.PlayerId;
            var attackerId = damageData.DamagerPlayerId;
            var attacker = playerManager.GetPlayerById(attackerId);

            // Do not process if local was not associated with it
            if (victimId != _local.playerId && attackerId != _local.playerId)
                return;

            var hitResult = ComputeHitResultFromDateTime(
                damageData.DamageOriginTime,
                now,
                attacker.LastHitData.HitTime,
                victim.LastHitData.HitTime
            );

            if (!hitResult)
                return;

            SetAssumedDiedTime(victimId, Networking.GetNetworkDateTime());
        }

        public override void OnSyncerReceived(DamageDataSyncer syncer)
        {
            if (syncer.VictimId != _local.playerId)
                return;

            var victim = playerManager.GetPlayerById(syncer.VictimId);
            // if (victim.HasDied)
            //     return;

            var result = ComputeHitResultFromDateTime(
                syncer.ActivatedTime, syncer.HitTime,
                GetAssumedDiedTime(syncer.AttackerId), victim.LastHitData.HitTime
            );

            if (!result)
                return;

            victim.LastHitData.SetData(syncer);
            victim.LastHitData.SyncData();
        }

        #endregion

        #region DiedTimeGetterSetter

        private readonly DataDictionary _confirmedDiedTimeDict = new DataDictionary();
        private readonly DataDictionary _assumedDiedTimeDict = new DataDictionary();

        [PublicAPI]
        public DateTime GetConfirmedDiedTime(int playerId)
        {
            return _confirmedDiedTimeDict.TryGetValue(new DataToken(playerId), TokenType.Long,
                out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        [PublicAPI]
        public DateTime GetAssumedDiedTime(int playerId)
        {
            return _assumedDiedTimeDict.TryGetValue(new DataToken(playerId), TokenType.Long,
                out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        private void SetConfirmedDiedTime(PlayerBase player, DateTime time)
        {
            var playerIdToken = new DataToken(player.PlayerId);
            var timeToken = new DataToken(time.Ticks);

            _confirmedDiedTimeDict.SetValue(playerIdToken, timeToken);
            _assumedDiedTimeDict.SetValue(playerIdToken, timeToken);
            // player.PreviousDiedTime = time;
        }

        private void SetAssumedDiedTime(int playerId, DateTime time)
        {
            _assumedDiedTimeDict.SetValue(playerId, time.Ticks);
        }

        private void RevertAssumedDiedTime(int playerId)
        {
            _assumedDiedTimeDict.SetValue(playerId, GetConfirmedDiedTime(playerId).Ticks);
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