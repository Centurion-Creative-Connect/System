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

        [SerializeField] [Header("Experimental")]
        public bool useTimeBasedCheck;

        private VRCPlayerApi _local;

        private void Start()
        {
            _local = Networking.LocalPlayer;
            playerManager.SubscribeCallback(this);
        }

        public bool Resolve(DamageDataSyncer syncer, out SyncResult result, out byte resultContext)
        {
            if (syncer.VictimId != _local.playerId)
            {
                result = SyncResult.Unassigned;
                resultContext = default;
                return false;
            }

            var victim = playerManager.GetPlayerById(syncer.VictimId);
            var attacker = playerManager.GetPlayerById(syncer.AttackerId);
            if (victim == null || attacker == null)
            {
                result = SyncResult.Cancelled;
                resultContext = SyncResultContext.PlayerCouldNotBeFound;
                return true;
            }

            if (victim.IsDead)
            {
                result = SyncResult.Cancelled;
                resultContext = SyncResultContext.VictimAlreadyDead;
                return true;
            }

            if (useTimeBasedCheck)
            {
                var hitResult = ComputeHitResultFromDateTime(
                    syncer.ActivatedTime,
                    GetAssumedDiedTime(syncer.AttackerId),
                    GetAssumedRevivedTime(syncer.AttackerId)
                );

                if (!hitResult)
                {
                    result = SyncResult.Cancelled;
                    resultContext = SyncResultContext.AttackerAlreadyDead;
                    return true;
                }
            }

            if (syncer.Type == KillType.FriendlyFire && playerManager.FriendlyFireMode == FriendlyFireMode.Both)
            {
                attacker.LastHitData.SetData(syncer);
                attacker.LastHitData.SyncData();
            }

            var lastHitData = syncer.Type == KillType.ReverseFriendlyFire ? attacker.LastHitData : victim.LastHitData;
            lastHitData.SetData(syncer);
            lastHitData.SyncData();
            result = SyncResult.Hit;
            resultContext = SyncResultContext.None;
            return true;
        }

        public void OnFinishing(DamageDataSyncer syncer)
        {
            switch (syncer.Result)
            {
                case SyncResult.Unassigned:
                default:
                {
                    // No finishing up needed for in-progress syncer
                    return;
                }
                case SyncResult.Hit:
                {
                    // Update confirmed died time
                    UpdateConfirmedDiedTime(syncer.VictimId);
                    return;
                }
                case SyncResult.Cancelled:
                {
                    // Revert assumed to confirmed died time
                    RevertAssumedDiedTime(syncer.VictimId);
                    return;
                }
            }
        }

        [PublicAPI]
        public static bool ComputeHitResultFromDateTime(
            DateTime damageOriginatedTime,
            DateTime attackerDiedTime,
            DateTime attackerRevivedTime)
        {
            // Handle edge case where player was instantly revived 
            if (attackerRevivedTime == attackerDiedTime) return true;

            // When attacker is alive
            if (attackerDiedTime < attackerRevivedTime) return attackerRevivedTime <= damageOriginatedTime;

            // When attacker is dead
            return damageOriginatedTime <= attackerDiedTime;
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

            if (attacker == null)
                return;

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

            _confirmedDiedTimeDict.Remove(player.PlayerId);
            _assumedDiedTimeDict.Remove(player.PlayerId);
            _assumedRevivedTimeDict.Remove(player.PlayerId);
        }

        public override void OnResetAllPlayerStats()
        {
            _confirmedDiedTimeDict.Clear();
            _assumedDiedTimeDict.Clear();
            _assumedRevivedTimeDict.Clear();
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            _confirmedDiedTimeDict.Remove(oldId);
            _confirmedDiedTimeDict.Remove(newId);

            _assumedDiedTimeDict.Remove(oldId);
            _assumedDiedTimeDict.Remove(newId);

            _assumedRevivedTimeDict.Remove(oldId);
            _assumedRevivedTimeDict.Remove(newId);
        }

        #endregion

        #region DiedTimeGetterSetter

        private readonly DataDictionary _confirmedDiedTimeDict = new DataDictionary();
        private readonly DataDictionary _assumedDiedTimeDict = new DataDictionary();
        private readonly DataDictionary _assumedRevivedTimeDict = new DataDictionary();

        [PublicAPI]
        public DateTime GetConfirmedDiedTime(int playerId)
        {
            return _confirmedDiedTimeDict.TryGetValue(new DataToken(playerId), TokenType.Long, out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        private void SetConfirmedDiedTime(int playerId, DateTime time)
        {
            _confirmedDiedTimeDict.SetValue(playerId, time.Ticks);
        }

        private void UpdateConfirmedDiedTime(int playerId)
        {
            SetConfirmedDiedTime(playerId, GetAssumedDiedTime(playerId));
        }

        [PublicAPI]
        public DateTime GetAssumedDiedTime(int playerId)
        {
            return _assumedDiedTimeDict.TryGetValue(playerId, TokenType.Long, out var timeToken)
                ? new DateTime(timeToken.Long)
                : DateTime.MinValue;
        }

        private void SetAssumedDiedTime(int playerId, DateTime time)
        {
            _assumedDiedTimeDict.SetValue(playerId, time.Ticks);
        }

        private void RevertAssumedDiedTime(int playerId)
        {
            SetAssumedDiedTime(playerId, GetConfirmedDiedTime(playerId));
        }

        [PublicAPI]
        public DateTime GetAssumedRevivedTime(int playerId)
        {
            return _assumedRevivedTimeDict.TryGetValue(playerId, TokenType.Long, out var timeToken)
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

    public static class SyncResultContext
    {
        public const byte None = 0x00;
        public const byte GenericFail = 0x01;
        public const byte AttackerAlreadyDead = 0x02;
        public const byte VictimAlreadyDead = 0x03;
        public const byte PlayerCouldNotBeFound = 0x04;

        public static string GetContextMessage(byte context)
        {
            // NOTE: Switch Expression is not supported in UdonSharp yet
            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (context)
            {
                default: return "UNDEFINED";
                case 0x00: return "NONE";
                case 0x01: return "FAIL";
                case 0x02: return "ATTACKER_DEAD";
                case 0x03: return "VICTIM_DEAD";
                case 0x04: return "PLAYER_NOT_FOUND";
            }
        }
    }
}