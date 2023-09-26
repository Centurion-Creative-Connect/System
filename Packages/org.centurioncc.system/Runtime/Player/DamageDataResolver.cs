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
        [SerializeField] [HideInInspector] [NewbieInject]
        private DamageDataSyncerManager damageDataSyncerManager;

        private VRCPlayerApi _local;

        [PublicAPI]
        public double InvincibleDuration { get; set; } = 10D;

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
            if (victim.IsDead)
                return SyncResult.Cancelled;

            var result = ComputeHitResultFromDateTime(
                syncer.ActivatedTime,
                syncer.HitTime,
                GetAssumedDiedTime(syncer.AttackerId),
                victim.LastHitData.HitTime
            );

            if (!result)
                return SyncResult.Cancelled;

            victim.LastHitData.SetData(syncer);
            victim.LastHitData.SyncData();
            return SyncResult.Hit;
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

        #endregion

        #region DiedTimeGetterSetter

        private readonly DataDictionary _assumedDiedTimeDict = new DataDictionary();

        [PublicAPI]
        public DateTime GetAssumedDiedTime(int playerId)
        {
            return _assumedDiedTimeDict.TryGetValue(new DataToken(playerId), TokenType.Long,
                out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        private void SetAssumedDiedTime(int playerId, DateTime time)
        {
            var lastAssumedTime = GetAssumedDiedTime(playerId);
            if (Math.Abs(time.Subtract(lastAssumedTime).TotalSeconds) < InvincibleDuration)
                return;

            _assumedDiedTimeDict.SetValue(playerId, time.Ticks);
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