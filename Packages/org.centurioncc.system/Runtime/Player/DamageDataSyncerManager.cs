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
    public class DamageDataSyncerManager : PlayerManagerCallbackBase
    {
        private const string Prefix = "[<color=cyan>DamageDataSyncerMgr</color>] ";

        [NewbieInject] [HideInInspector] [SerializeField]
        private PrintableBase logger;

        [NewbieInject] [HideInInspector] [SerializeField]
        private PlayerManager playerManager;

        [NewbieInject] [HideInInspector] [SerializeField]
        private DamageDataResolver resolver;

        [SerializeField] private DamageDataSyncer[] syncers;

        private readonly DataDictionary _resolvedEvents = new DataDictionary();

        private int _callbackCount;
        private UdonSharpBehaviour[] _callbacks = new UdonSharpBehaviour[1];

        private VRCPlayerApi _local;

        private WatchdogChildCallbackBase[] _watchdogChild;


        [PublicAPI] public int MaxResendCount { get; set; } = 2;
        [PublicAPI] public bool ProcessingPaused { get; private set; }

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

        public override void OnHitDetection(PlayerCollider pCol, DamageData damageData, Vector3 contactPoint)
        {
            if (ProcessingPaused)
            {
                logger.LogError($"{Prefix}Will not send because processing was paused");
                return;
            }

            if (pCol == null || damageData == null)
            {
                logger.LogVerbose(
                    $"{Prefix}Will not send because PlayerCollider or DamageData were null");
                return;
            }

            if (!damageData.ShouldApplyDamage)
            {
                logger.LogVerbose(
                    $"{Prefix}Will not send because ShouldApplyDamage == false");
                return;
            }

            var victimId = pCol.player.PlayerId;
            var attackerId = damageData.DamagerPlayerId;

            if (victimId == attackerId && !damageData.CanDamageSelf)
            {
                logger.LogVerbose($"{Prefix}Will not send because because self shooting");
                return;
            }

            if (damageData.DetectionType == DetectionType.AttackerSide && _local.playerId != attackerId)
            {
                logger.LogVerbose($"{Prefix}Will not send because local is not attacker");
                return;
            }

            if (damageData.DetectionType == DetectionType.VictimSide && _local.playerId != victimId)
            {
                logger.LogVerbose($"{Prefix}Will not set because local is not victim");
                return;
            }

            if (victimId != _local.playerId && attackerId != _local.playerId)
            {
                logger.LogVerbose($"{Prefix}Will not send because data was not associated with local player");
                return;
            }

            var attacker = playerManager.GetPlayerById(attackerId);
            if (attacker == null)
            {
                logger.LogError($"{Prefix}Will not send because attacker was not found");
                return;
            }

            var hitTime = Networking.GetNetworkDateTime();
            var victim = pCol.player;
            if (victim.IsDead)
            {
                logger.LogVerbose($"{Prefix}Will not send because attacker was already dead");
                return;
            }

            var killType = KillType.Default;
            if (attacker.TeamId == victim.TeamId && victim.TeamId != 0)
            {
                killType = KillType.FriendlyFire;

                if (!attacker.IsLocal)
                {
                    logger.LogVerbose($"{Prefix}Will not resolve because non-local friendly fire");
                    return;
                }

                if (!damageData.CanDamageFriendly)
                {
                    logger.LogVerbose($"{Prefix}Will not resolve because damaging friendly is disabled");
                    return;
                }

                if (damageData.RespectFriendlyFireSetting)
                {
                    switch (playerManager.FriendlyFireMode)
                    {
                        default:
                        case FriendlyFireMode.Never:
                        {
                            logger.LogVerbose($"{Prefix}Will not resolve because friendly fire");
                            playerManager.Invoke_OnFriendlyFire(attacker, victim);
                            return;
                        }
                        case FriendlyFireMode.Warning:
                        {
                            playerManager.Invoke_OnFriendlyFireWarning(victim, damageData, contactPoint);
                            return;
                        }
                        case FriendlyFireMode.Reverse:
                        {
                            killType = KillType.ReverseFriendlyFire;
                            break;
                        }
                        case FriendlyFireMode.Both:
                        case FriendlyFireMode.Always:
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                if (!damageData.CanDamageEnemy)
                {
                    logger.LogVerbose($"{Prefix}Will not resolve because damaging enemy is disabled");
                    return;
                }
            }

            if (Invoke_OnHitSendCallback(damageData, attacker, victim))
            {
                logger.LogVerbose($"{Prefix}Callback has refused this hit.");
                return;
            }


            var syncer = GetPlayerDependentSyncer(victim);

            syncer.Send(
                GetValidEventId(),
                victimId,
                attackerId,
                damageData.DamageOriginPosition,
                contactPoint,
                damageData.DamageOriginTime,
                hitTime,
                damageData.DamageType,
                SyncState.Sending,
                SyncResult.Unassigned,
                killType,
                pCol.parts,
                0x00
            );

            logger.LogVerbose($"{Prefix}Sending request {syncer.GetLocalInfo()}");
        }

        public void Receive(DamageDataSyncer requester)
        {
            if (ProcessingPaused)
            {
                logger.LogError(
                    $"{Prefix}{requester.GetGlobalInfo()} has requested resolve, but resolver is currently paused");
                return;
            }

            logger.LogVerbose($"{Prefix}Received {requester.GetGlobalInfo()}");

            if (requester.State == SyncState.Sending && requester.VictimId == _local.playerId)
            {
                if (!resolver.Resolve(requester, out var result, out var context))
                {
                    Debug.LogError(
                        $"{Prefix}{requester.GetGlobalInfo()} has requested resolve, but resolver rejected it");
                    return;
                }

                requester.UpdateResult(result, context);
            }

            AddResolvedEventId(requester);
            Invoke_OnReceiveCallback(requester);

            resolver.OnFinishing(requester);
        }

        public void RequestResend(DamageDataSyncer requester)
        {
            if (ProcessingPaused)
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

            var resendSyncer = GetPlayerDependentSyncer(playerManager.GetPlayerById(requester.VictimId));
            resendSyncer.Resend(requester);
            logger.Log($"{Prefix}Resending with {resendSyncer.GetLocalInfo()}");
        }

        [NotNull]
        private DamageDataSyncer GetPlayerDependentSyncer(PlayerBase victim)
        {
            if (victim == null)
                return syncers[0];
            var index = victim.Index % syncers.Length;
            return syncers[index];
        }

        private void AddResolvedEventId(DamageDataSyncer syncer)
        {
            var key = (DataToken)$"{syncer.EventId}";
            if (_resolvedEvents.ContainsKey(key))
            {
                var duplicateCount = 1;
                while (_resolvedEvents.ContainsKey($"{syncer.EventId}-{duplicateCount}"))
                    ++duplicateCount;

                key = new DataToken($"{syncer.EventId}-{duplicateCount}");
            }

            _resolvedEvents.SetValue(key, syncer.ToDictionary());
        }

        private int GetValidEventId()
        {
            var eventId = Random.Range(0x10000, int.MaxValue);
            while (_resolvedEvents.ContainsKey($"{eventId}")) eventId = Random.Range(0x10000, int.MaxValue);

            return eventId;
        }

        private void Invoke_OnReceiveCallback(DamageDataSyncer syncer)
        {
            foreach (var callback in _callbacks)
                if (callback != null)
                    ((DamageDataSyncerManagerCallback)callback).OnSyncerReceived(syncer);
        }

        private bool Invoke_OnHitSendCallback(DamageData damageData, PlayerBase attacker, PlayerBase victim)
        {
            foreach (var callback in _callbacks)
                if (callback != null &&
                    ((DamageDataSyncerManagerCallback)callback).OnPreHitSend(damageData, attacker, victim))
                    return true;
            return false;
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
            if (ProcessingPaused)
            {
                logger.LogError($"{Prefix}Tried to pause syncer, but it's already paused!");
                return;
            }

            ProcessingPaused = true;
            logger.LogWarn($"{Prefix}Paused process!");
        }

        public void ContinueProcessing()
        {
            if (!ProcessingPaused)
            {
                logger.LogError($"{Prefix}Tried to continue syncer, but it's not paused!");
                return;
            }

            foreach (var syncer in syncers)
                syncer.MakeAvailable();

            ProcessingPaused = false;
            logger.Log($"{Prefix}Continuing process!");
        }

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