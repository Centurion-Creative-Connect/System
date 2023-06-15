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
            if (pCol == null || damageData == null)
            {
                logger.LogVerbose(
                    $"{Prefix}Will not resolve because PlayerCollider or DamageData were null");
                return;
            }

            if (!damageData.ShouldApplyDamage)
            {
                logger.LogVerbose(
                    $"{Prefix}Will not resolve because ShouldApplyDamage == false");
                return;
            }

            var victimId = pCol.player.PlayerId;
            var attackerId = damageData.DamagerPlayerId;

            if (victimId == attackerId)
            {
                logger.LogVerbose($"{Prefix}Will not resolve because because self shooting");
                return;
            }

            if (victimId != _local.playerId && attackerId != _local.playerId)
            {
                logger.LogVerbose($"{Prefix}Will not resolve because data was not associated with local player");
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
                logger.LogVerbose(
                    $"{Prefix}Will not resolve because calculated result was {ResolverDataSyncer.GetResultName(hitResult)}");
                return;
            }

            var victim = pCol.player;
            var killType = KillType.Default;
            if (attacker.TeamId == victim.TeamId && victim.TeamId != 0)
            {
                killType = KillType.FriendlyFire;

                if (!attacker.IsLocal)
                {
                    logger.LogVerbose($"{Prefix}Will not resolve because non-local friendly fire");
                    return;
                }

                if (!playerManager.AllowFriendlyFire)
                {
                    logger.LogVerbose($"{Prefix}Will not resolve because friendly fire");
                    return;
                }
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
                resolveRequest == ResolveRequest.SelfResolved ? HitResult.Hit : HitResult.Waiting,
                killType
            );

            SetStoredLastKilledTime(victimId, Networking.GetNetworkDateTime());
            logger.Log($"{Prefix}Sending request {syncer.GetLocalInfo()}");
        }

        public override void RequestResolve(ResolverDataSyncer requester)
        {
            logger.Log($"{Prefix}Resolving {requester.GetGlobalInfo()}");

            // Request must be specified.
            if (requester.Request == ResolveRequest.Unassigned)
            {
                logger.LogError($"{Prefix}Tried to resolve requester with no request specified");
                return;
            }

            var attacker = playerManager.GetPlayerById(requester.AttackerId);
            var victim = playerManager.GetPlayerById(requester.VictimId);
            var result = requester.Result;

            if (result == HitResult.Waiting)
            {
                // No self-replying
                if (requester.SenderId == _local.playerId)
                {
                    logger.LogWarn($"{Prefix}Tried to self-reply. aborting!");
                    return;
                }

                // Check for errors
                if (requester.Request == ResolveRequest.Unassigned || requester.Request == ResolveRequest.SelfResolved)
                {
                    logger.LogError($"{Prefix}Tried to resolve invalid state requester. resetting!");
                    requester.MakeAvailable();
                    return;
                }

                // Not associated with this request
                if ((requester.Request == ResolveRequest.ToVictim && requester.VictimId != _local.playerId) ||
                    (requester.Request == ResolveRequest.ToAttacker && requester.AttackerId != _local.playerId))
                {
                    logger.Log($"{Prefix}Tried to resolve non-local request. aborting!");
                    return;
                }

                requester.SendReply(result = ComputeHitResultFromDateTime(
                    requester.OriginTime,
                    attacker.LastDiedDateTime,
                    GetStoredLastKilledTime(requester.VictimId)
                ));
                logger.Log(
                    $"{Prefix}Sending reply with {ResolverDataSyncer.GetResultName(result)}. full: {requester.GetLocalInfo()}");
            }

            Invoke_ResolvedCallback(requester);

            if (result == HitResult.Hit)
            {
                if (requester.Type == KillType.FriendlyFire)
                {
                    playerManager.Invoke_OnFriendlyFire(attacker, victim);
                }

                playerManager.Invoke_OnKilled(attacker, victim, requester.Type);
            }
        }

        public override void RequestResend(ResolverDataSyncer requester)
        {
            logger.LogWarn($"{Prefix}Requesting resend {requester.GetLocalInfo()}");
            if (requester.ResendCount > MaxResendCount)
            {
                logger.LogError(
                    $"{Prefix}Request for resend aborted because it was over max threshold of `{MaxResendCount}`");
                return;
            }

            var resendSyncer = GetAvailableSyncer(requester);
            resendSyncer.Resend(requester);
            logger.Log($"{Prefix}Resending with {requester.GetLocalInfo()}");
        }

        [NotNull]
        public ResolverDataSyncer GetAvailableSyncer(ResolverDataSyncer except = null)
        {
            var oldestSyncer = GetAvailableSyncerOldest(except);
            if (oldestSyncer != null)
                return oldestSyncer;

            logger.LogError($"{Prefix}Couldn't get available syncer by oldest query, falling back!");
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
            var oldestSyncer = syncers[0];
            var usedTime = oldestSyncer.LastUsedTime;
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

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer, KillType type)
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
    }
}