using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DamageDataResolver : DamageDataResolverBase
    {
        private const string Prefix = "[<color=cyan>DamageDataResolver</color>] ";

        [NewbieInject] [HideInInspector] [SerializeField]
        private PrintableBase logger;
        [NewbieInject] [HideInInspector] [SerializeField]
        private PlayerManager playerManager;
        [SerializeField]
        private ResolverDataSyncer[] syncers;

        private readonly DataDictionary _lastKilledTimeDict = new DataDictionary();
        private int _callbackCount;

        private UdonSharpBehaviour[] _callbacks = new UdonSharpBehaviour[1];

        private VRCPlayerApi _local;

        public int MaxResendCount { get; set; } = 5;

        private void Start()
        {
            _local = Networking.LocalPlayer;
            playerManager.SubscribeCallback(this);
        }

        public bool SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.AddBehaviour(behaviour, ref _callbackCount, ref _callbacks);
        }

        public bool UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.RemoveBehaviour(behaviour, ref _callbackCount, ref _callbacks);
        }

        public override void Resolve(PlayerCollider pCol, DamageData damageData, Vector3 contactPoint)
        {
            var victimId = pCol.player.PlayerId;
            var attackerId = damageData.DamagerPlayerId;
            if (victimId != _local.playerId && attackerId != _local.playerId)
            {
                logger.LogError($"{Prefix}Will not resolve because data was not associated with local player");
                return;
            }

            var attacker = playerManager.GetPlayerById(attackerId);
            if (attacker == null)
            {
                logger.LogError($"{Prefix}Will not resolve because attacker was not found");
                return;
            }

            var hitResult = ComputeHitResultFromDateTime(
                damageData.DamageOriginTime,
                attacker.LastDiedDateTime,
                GetStoredLastKilledTime(victimId)
            );

            if (hitResult != HitResult.Hit)
            {
                logger.LogVerbose($"{Prefix}Will not resolve because calculated result was {GetResultName(hitResult)}");
                return;
            }

            var syncer = GetAvailableSyncer();
            var resolveRequest = victimId == _local.playerId
                ? ResolveRequest.SelfResolved
                : attackerId == _local.playerId
                    ? ResolveRequest.ToVictim
                    : ResolveRequest.ToAttacker;

            syncer.Send(
                victimId,
                attackerId,
                damageData.DamageOriginPosition,
                contactPoint,
                damageData.DamageOriginTime,
                damageData.DamageType,
                resolveRequest,
                resolveRequest == ResolveRequest.SelfResolved ? HitResult.Hit : HitResult.None
            );

            SetStoredLastKilledTime(victimId, Networking.GetNetworkDateTime());
            logger.LogVerbose($"{Prefix}Sending request {GetSyncerInfo(syncer)}");
        }

        public void RequestResolve(ResolverDataSyncer requester)
        {
            logger.LogVerbose($"{Prefix}Resolving {GetSyncerInfo(requester)}");

            // Request must be specified.
            if (requester.Request == ResolveRequest.None)
            {
                logger.LogError($"{Prefix}Tried to resolve requester with no request specified");
                return;
            }

            var attacker = playerManager.GetPlayerById(requester.AttackerId);
            var victim = playerManager.GetPlayerById(requester.VictimId);
            var result = requester.Result;

            if (result == HitResult.None)
            {
                // No self-replying
                if (requester.SenderId == _local.playerId)
                {
                    logger.LogVerbose($"{Prefix}Tried to self-reply. aborting!");
                    return;
                }

                // Check for errors
                if (requester.Request == ResolveRequest.None || requester.Request == ResolveRequest.SelfResolved)
                {
                    logger.LogError($"{Prefix}Tried to resolve invalid state requester. resetting!");
                    requester.MakeAvailable();
                    return;
                }

                // Not associated with this request
                if ((requester.Request == ResolveRequest.ToVictim && requester.VictimId != _local.playerId) ||
                    (requester.Request == ResolveRequest.ToAttacker && requester.AttackerId != _local.playerId))
                {
                    logger.LogVerbose($"{Prefix}Tried to resolve non-local request. aborting!");
                    return;
                }

                requester.SendReply(result = ComputeHitResultFromDateTime(
                    requester.OriginTime,
                    attacker.LastDiedDateTime,
                    GetStoredLastKilledTime(requester.VictimId)
                ));
                logger.LogVerbose(
                    $"{Prefix}Sending reply with {GetResultName(result)}. full: {GetSyncerInfo(requester)}");
            }

            Invoke_ResolvedCallback(requester);

            if (result == HitResult.Hit)
                playerManager.Invoke_OnKilled(attacker, victim);
        }

        public void RequestResend(ResolverDataSyncer requester)
        {
            logger.LogWarn($"{Prefix}Requesting resend {GetSyncerInfo(requester)}");
            if (requester.ResendCount > MaxResendCount)
            {
                logger.LogError(
                    $"{Prefix}Request for resend aborted because it was over max threshold of `{MaxResendCount}`");
                return;
            }

            var resendSyncer = GetAvailableSyncer(requester);
            resendSyncer.Resend(requester);
            logger.LogVerbose($"{Prefix}Resending with {GetSyncerInfo(resendSyncer)}");
        }

        [NotNull]
        public ResolverDataSyncer GetAvailableSyncer(ResolverDataSyncer except = null)
        {
            var oldestSyncer = GetAvailableSyncerOldest(except);
            if (oldestSyncer != null)
                return oldestSyncer;

            // Fallback
            foreach (var syncer in syncers)
                if (syncer.IsAvailable && syncer != except)
                    return syncer;

            logger.LogError($"{Prefix}Couldn't get available syncer. returning 0!");
            return syncers[0];
        }

        [CanBeNull]
        public ResolverDataSyncer GetAvailableSyncerRandom(ResolverDataSyncer except = null, int maxRetryCount = 5)
        {
            while (maxRetryCount >= 0)
            {
                var syncer = syncers[UnityEngine.Random.Range(0, syncers.Length)];
                if (syncer.IsAvailable && syncer != except)
                    return syncer;
                --maxRetryCount;
            }

            return null;
        }

        [CanBeNull]
        public ResolverDataSyncer GetAvailableSyncerOldest(ResolverDataSyncer except = null)
        {
            float usedTime = 0;
            ResolverDataSyncer oldestSyncer = null;
            foreach (var s in syncers)
            {
                if (!s.IsAvailable || s == except || s.LastUsedTime > usedTime)
                    continue;
                oldestSyncer = s;
                usedTime = s.LastUsedTime;
            }

            return oldestSyncer;
        }

        private DateTime GetStoredLastKilledTime(int playerId)
        {
            return _lastKilledTimeDict.TryGetValue(new DataToken(playerId), out var lastKilledTimeToken)
                ? new DateTime(lastKilledTimeToken.Long)
                : DateTime.MinValue;
        }

        private void SetStoredLastKilledTime(int playerId, DateTime time)
        {
            _lastKilledTimeDict.SetValue(
                new DataToken(playerId),
                new DataToken(time.Ticks)
            );
        }

        private void Invoke_ResolvedCallback(ResolverDataSyncer syncer)
        {
            foreach (var callback in _callbacks)
            {
                if (callback != null)
                    ((DamageDataResolverCallback)callback).OnResolved(syncer);
            }
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            SetStoredLastKilledTime(hitPlayer.PlayerId, Networking.GetNetworkDateTime());
        }

        public static HitResult ComputeHitResultFromDateTime(
            DateTime originTime,
            DateTime attackerDiedTime,
            DateTime victimDiedTime
        )
        {
            var sinceVictimDiedFromNow = Networking.GetNetworkDateTime().Subtract(victimDiedTime).TotalSeconds;
            var sinceVictimDied = originTime.Subtract(victimDiedTime).TotalSeconds;
            var sinceAttackerDied = originTime.Subtract(attackerDiedTime).TotalSeconds;

            Debug.Log(
                $"{Prefix}sinceVicDiedNow: {sinceVictimDiedFromNow:F2}, sinceVicDied: {sinceVictimDied:F2}, sinceAtkDied: {sinceAttackerDied:F2}");

            const double invincibleDuration = 10D;
            if (sinceAttackerDied < invincibleDuration)
                return HitResult.FailByAttackerDead;
            if (sinceVictimDied < invincibleDuration || sinceVictimDiedFromNow < invincibleDuration)
                return HitResult.FailByVictimDead;

            return HitResult.Hit;
        }

        private static string GetSyncerInfo(ResolverDataSyncer syncer)
        {
            return $"{syncer.name}, " +
                   $"{syncer.SenderId}/" +
                   $"{syncer.AttackerId}->" +
                   $"{syncer.VictimId}@" +
                   $"{syncer.OriginTimeTicks}:" +
                   $"{GetRequestName(syncer.Request)}:" +
                   $"{GetResultName(syncer.Result)}|" +
                   $"{syncer.ResendCount}";
        }

        private static string GetRequestName(ResolveRequest r)
        {
            switch (r)
            {
                case ResolveRequest.None:
                    return "None";
                case ResolveRequest.ToAttacker:
                    return "ByAttacker";
                case ResolveRequest.ToVictim:
                    return "ByVictim";
                case ResolveRequest.SelfResolved:
                    return "SelfResolved";
                default:
                    return "Unknown";
            }
        }

        private static string GetResultName(HitResult r)
        {
            switch (r)
            {
                case HitResult.None:
                    return "None";
                case HitResult.Hit:
                    return "Hit";
                case HitResult.Fail:
                    return "Fail";
                case HitResult.FailByAttackerDead:
                    return "FailByAttackerDead";
                case HitResult.FailByVictimDead:
                    return "FailByVictimDead";
                default:
                    return "Unknown";
            }
        }
    }

    public enum ResolveRequest
    {
        None,
        ToAttacker,
        ToVictim,
        SelfResolved,
    }

    public enum HitResult
    {
        None,
        Hit,
        Fail,
        FailByAttackerDead,
        FailByVictimDead
    }
}