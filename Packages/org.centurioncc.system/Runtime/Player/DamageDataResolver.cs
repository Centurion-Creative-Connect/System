using System;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
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

        private readonly DataDictionary _resolvedEvents = new DataDictionary();

        private int _callbackCount;
        private UdonSharpBehaviour[] _callbacks = new UdonSharpBehaviour[1];

        private VRCPlayerApi _local;

        private WatchdogChildCallbackBase[] _watchdogChild;

        [PublicAPI]
        public double InvincibleDuration { get; set; } = 10D;
        [PublicAPI]
        public int MaxResendCount { get; set; } = 2;
        [PublicAPI]
        public bool ResolvingPaused { get; private set; }

        private void Start()
        {
            _watchdogChild = new WatchdogChildCallbackBase[syncers.Length];
            for (int i = 0; i < _watchdogChild.Length; i++)
                _watchdogChild[i] = (WatchdogChildCallbackBase)(UdonSharpBehaviour)syncers[i];

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
            if (ResolvingPaused)
            {
                logger.LogError($"{Prefix}Will not resolve because processing was paused");
                return;
            }

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

            var hitTime = Networking.GetNetworkDateTime();
            var hitResult = ComputeHitResultFromDateTime(
                damageData.DamageOriginTime,
                hitTime,
                GetAssumedDiedTime(attackerId),
                GetAssumedDiedTime(victimId)
            );

            if (hitResult != HitResult.Hit)
            {
                logger.LogWarn(
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
            var resolveRequest = attackerId == _local.playerId
                ? ResolveRequest.ToVictim
                : ResolveRequest.ToAttacker;

            syncer.Send(
                GetValidEventId(),
                victimId,
                attackerId,
                damageData.DamageOriginPosition,
                contactPoint,
                damageData.DamageOriginTime,
                hitTime,
                damageData.DamageType,
                resolveRequest,
                HitResult.Waiting,
                killType
            );

            SetAssumedDiedTime(victimId, hitTime);
            logger.Log($"{Prefix}Sending request {syncer.GetLocalInfo()}");
        }

        public override void RequestResolve(ResolverDataSyncer requester)
        {
            if (ResolvingPaused)
            {
                logger.LogError(
                    $"{Prefix}{requester.GetGlobalInfo()} has requested resolve, but resolver is currently paused");
                Invoke_ResolveAbortedCallback(requester, "PAUSED");
                return;
            }

            logger.Log($"{Prefix}Resolving {requester.GetGlobalInfo()}");

            // Request must be specified.
            if (requester.Request == ResolveRequest.Unassigned)
            {
                logger.LogError($"{Prefix}Tried to resolve requester with no request specified");
                Invoke_ResolveAbortedCallback(requester, "NO_REQUEST");
                return;
            }

            var attacker = playerManager.GetPlayerById(requester.AttackerId);
            var victim = playerManager.GetPlayerById(requester.VictimId);
            var result = requester.Result;
            if (attacker == null || victim == null)
            {
                logger.LogError($"{Prefix}Tried to resolve requester with invalid attacker or victim");
                Invoke_ResolveAbortedCallback(requester, "INVALID_PLAYER");
                return;
            }

            if (result == HitResult.Waiting)
            {
                // No self-replying
                if (requester.SenderId == _local.playerId)
                {
                    logger.LogWarn($"{Prefix}Tried to self-reply. aborting!");
                    Invoke_ResolveAbortedCallback(requester, "SELF_REPLY");
                    return;
                }

                // Check for errors
                if (requester.Request == ResolveRequest.Unassigned)
                {
                    logger.LogError($"{Prefix}Tried to resolve invalid state requester. resetting!");
                    Invoke_ResolveAbortedCallback(requester, "INVALID_STATE");
                    requester.MakeAvailable();
                    return;
                }

                // Not associated with this request
                if ((requester.Request == ResolveRequest.ToVictim && requester.VictimId != _local.playerId) ||
                    (requester.Request == ResolveRequest.ToAttacker && requester.AttackerId != _local.playerId))
                {
                    logger.Log($"{Prefix}Tried to resolve non-local request. aborting!");
                    Invoke_ResolveAbortedCallback(requester, "REQUEST_NON_LOCAL");
                    return;
                }

                var assumedResult = ComputeHitResultFromDateTime(
                    requester.ActivatedTime,
                    requester.HitTime,
                    GetAssumedDiedTime(requester.AttackerId),
                    GetAssumedDiedTime(requester.VictimId)
                );

                result = ComputeHitResultFromDateTime(
                    requester.ActivatedTime,
                    requester.HitTime,
                    GetConfirmedDiedTime(requester.AttackerId),
                    GetConfirmedDiedTime(requester.VictimId)
                );

                if (assumedResult != result)
                {
                    logger.LogWarn(
                        $"{Prefix}assumed result: {ResolverDataSyncer.GetResultName(assumedResult)} != confirmed result {ResolverDataSyncer.GetResultName(result)}");
                }

                requester.SendReply(result);
                if (result == HitResult.Hit) SetConfirmedDiedTime(victim, requester.HitTime);

                logger.Log(
                    $"{Prefix}Sending reply with {ResolverDataSyncer.GetResultName(result)}. full: {requester.GetLocalInfo()}");
                return;
            }

            AddResolvedEventId(requester);
            Invoke_ResolvedCallback(requester);

            if (result == HitResult.Hit)
            {
                SetConfirmedDiedTime(victim, requester.HitTime);
                ++attacker.Kills;
                ++victim.Deaths;

                if (requester.Type == KillType.FriendlyFire) playerManager.Invoke_OnFriendlyFire(attacker, victim);

                playerManager.Invoke_OnKilled(attacker, victim, requester.Type);
            }
            else if (result == HitResult.Fail ||
                     result == HitResult.FailByAttackerDead ||
                     result == HitResult.FailByVictimDead)
            {
                RevertAssumedDiedTime(requester.VictimId);
            }
        }

        public override void RequestResend(ResolverDataSyncer requester)
        {
            if (ResolvingPaused)
            {
                logger.LogError(
                    $"{Prefix}{requester.GetGlobalInfo()} has requested resend, but resolver is currently paused");
                return;
            }

            logger.LogWarn($"{Prefix}Requesting resend {requester.GetLocalInfo()}");
            if (requester.ResendCount > MaxResendCount)
            {
                logger.LogError(
                    $"{Prefix}Request for resend aborted because it was over max threshold of `{MaxResendCount}`");
                return;
            }

            var resendSyncer = GetAvailableSyncer(requester);
            resendSyncer.Resend(requester);
            logger.Log($"{Prefix}Resending with {resendSyncer.GetLocalInfo()}");
        }

        [NotNull]
        private ResolverDataSyncer GetAvailableSyncer(ResolverDataSyncer except = null)
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
        private ResolverDataSyncer GetAvailableSyncerRandom(ResolverDataSyncer except = null, int maxRetryCount = 5)
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
        private ResolverDataSyncer GetAvailableSyncerOldest(ResolverDataSyncer except = null)
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

        private void AddResolvedEventId(ResolverDataSyncer syncer)
        {
            var key = (DataToken)$"{syncer.EventId}";
            if (_resolvedEvents.ContainsKey(key))
            {
                var duplicateCount = 1;
                while (_resolvedEvents.ContainsKey($"{syncer.EventId}-{duplicateCount}"))
                    ++duplicateCount;

                key = new DataToken($"{syncer.EventId}-{duplicateCount}");
                logger.LogError(
                    $"{Prefix}Duplicated EventId found ({syncer.EventId}), Recording as {key.String}");
            }

            _resolvedEvents.SetValue(key, syncer.AsDataToken());
        }

        private int GetValidEventId()
        {
            var eventId = UnityEngine.Random.Range(0x10000, int.MaxValue);
            while (_resolvedEvents.ContainsKey($"{eventId}")) eventId = UnityEngine.Random.Range(0x10000, int.MaxValue);

            return eventId;
        }

        private void Invoke_ResolvedCallback(ResolverDataSyncer syncer)
        {
            foreach (var callback in _callbacks)
                if (callback != null)
                    ((DamageDataResolverCallback)callback).OnResolved(syncer);
        }

        private void Invoke_ResolveAbortedCallback(ResolverDataSyncer syncer, string reason)
        {
            foreach (var callback in _callbacks)
                if (callback != null)
                    ((DamageDataResolverCallback)callback).OnResolveAborted(syncer, reason);
        }

        [PublicAPI]
        public HitResult ComputeHitResultFromDateTime(
            DateTime damageOriginatedTime,
            DateTime hitTime,
            DateTime attackerDiedTime,
            DateTime victimDiedTime
        )
        {
            return
                damageOriginatedTime > hitTime
                    ? HitResult.Fail // Hit Time should not be earlier than originated time
                    : IsInInvincibleDuration(damageOriginatedTime, attackerDiedTime)
                        ? HitResult.FailByAttackerDead // Attacker should be alive at originated time
                        : IsInInvincibleDuration(hitTime, victimDiedTime)
                            ? HitResult.FailByVictimDead // Victim should be alive at hit time
                            : HitResult.Hit;
        }

        [PublicAPI]
        public bool IsInInvincibleDuration(DateTime now, DateTime diedTime)
        {
            var diff = now.Subtract(diedTime).TotalSeconds;
            return diff < InvincibleDuration && diff >= 0;
        }

        [PublicAPI]
        public DataDictionary GetResolvedEvents()
        {
            return _resolvedEvents.DeepClone();
        }

        public bool GetEventsJson(JsonExportType exportType, out DataToken jsonToken)
        {
            return VRCJson.TrySerializeToJson(new DataToken(_resolvedEvents), exportType, out jsonToken);
        }

        public void PauseProcessing()
        {
            if (ResolvingPaused)
            {
                logger.LogError($"{Prefix}Tried to pause resolver, but it's already paused!");
                return;
            }

            ResolvingPaused = true;
            logger.LogWarn($"{Prefix}Paused process!");
        }

        public void ContinueProcessing()
        {
            if (!ResolvingPaused)
            {
                logger.LogError($"{Prefix}Tried to continue resolver, but it's not paused!");
                return;
            }

            foreach (var syncer in syncers)
                syncer.MakeAvailable();

            ResolvingPaused = false;
            logger.Log($"{Prefix}Continuing process!");
        }

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
            player.PreviousDiedTime = time;
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
            return _watchdogChild;
        }

        #endregion
    }
}