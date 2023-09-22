using System;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DamageDataSyncer : UdonSharpBehaviour
    {
        [NewbieInject] [HideInInspector] [SerializeField]
        private DamageDataSyncerManager manager;
        private bool _hasProcessed = false;

        private bool _hasSent = true;

        public bool IsAvailable => _hasSent && State != SyncState.Sending;
        public int ResendCount { get; private set; }
        public float LastUsedTime { get; private set; }

        public void Resend(DamageDataSyncer other)
        {
            _hasProcessed = other._hasProcessed;

            LastUsedTime = Time.realtimeSinceStartup;

            _localEventId = other._localEventId;
            _localSenderId = other._localSenderId;
            _localVictimId = other._localVictimId;
            _localAttackerId = other._localAttackerId;
            _localActivatedPos = other._localActivatedPos;
            _localHitPos = other._localHitPos;
            _localActivatedTime = other._localActivatedTime;
            _localHitTime = other._localHitTime;
            _localWeaponType = other._localWeaponType;
            _localState = other._localState;
            _localType = other._localType;

            ResendCount = ++other.ResendCount;

            RequestSync();
        }

        public void UpdateResult(SyncResult result)
        {
            _hasProcessed = false;

            LastUsedTime = Time.realtimeSinceStartup;

            _localResult = result;
            _localState = SyncState.Received;

            RequestSync();
        }

        public void Send(
            int eventId,
            int victimId,
            int attackerId,
            Vector3 originPos,
            Vector3 hitPos,
            DateTime activatedTime,
            DateTime hitTime,
            string weaponType,
            SyncState state,
            SyncResult result,
            KillType type
        )
        {
            _hasProcessed = false;

            LastUsedTime = Time.realtimeSinceStartup;

            _localEventId = eventId;
            _localSenderId = Networking.LocalPlayer.playerId;
            _localVictimId = victimId;
            _localAttackerId = attackerId;
            _localActivatedPos = originPos;
            _localHitPos = hitPos;
            _localActivatedTime = activatedTime;
            _localHitTime = hitTime;
            _localWeaponType = weaponType;

            _localState = state;
            _localResult = result;
            _localType = type;

            ResendCount = 0;

            RequestSync();
        }

        public void ApplyLocal()
        {
            EventId = _localEventId;
            SenderId = _localSenderId;
            VictimId = _localVictimId;
            AttackerId = _localAttackerId;
            ActivatedPosition = _localActivatedPos;
            HitPosition = _localHitPos;
            ActivatedTimeTicks = _localActivatedTime.Ticks;
            HitTimeTicks = _localHitTime.Ticks;
            WeaponType = _localWeaponType;

            State = _localState;
            Result = _localResult;
            Type = _localType;
        }

        public void ApplyGlobal()
        {
            _localEventId = EventId;
            _localSenderId = SenderId;
            _localVictimId = VictimId;
            _localAttackerId = AttackerId;
            _localActivatedPos = ActivatedPosition;
            _localHitPos = HitPosition;
            _localActivatedTime = ActivatedTime;
            _localHitTime = HitTime;
            _localWeaponType = WeaponType;

            _localState = State;
            _localResult = Result;
            _localType = Type;
        }

        public void MakeAvailable()
        {
            _hasSent = true;
            _hasProcessed = false;
            State = SyncState.Unassigned;
        }

        public string GetShortEventId()
        {
            return (EventId % 0x10000).ToString("X5");
        }

        public string GetShortLocalEventId()
        {
            return (_localEventId % 0x10000).ToString("X5");
        }

        public string GetGlobalInfo()
        {
            return $"{name}:" +
                   $"{GetShortEventId()}," +
                   $"{SenderId}/" +
                   $"{AttackerId}->" +
                   $"{VictimId}@" +
                   $"{ActivatedTime:s}/" +
                   $"{HitTime:s}:" +
                   $"{GetStateName(State)}:" +
                   $"{GetResultName(Result)}:" +
                   $"{GetKillTypeName(Type)}|" +
                   $"{ResendCount}";
        }

        public string GetLocalInfo()
        {
            return $"{name}:" +
                   $"{GetShortLocalEventId()}," +
                   $"{(_hasSent ? $"{SenderId}" : $"{Networking.LocalPlayer.playerId}?")}/" +
                   $"{_localAttackerId}->" +
                   $"{_localVictimId}@" +
                   $"{_localActivatedTime:s}/" +
                   $"{_localHitTime:s}:" +
                   $"{GetStateName(_localState)}:" +
                   $"{GetResultName(_localResult)}:" +
                   $"{GetKillTypeName(_localType)}|" +
                   $"{ResendCount}";
        }

        private void RequestSync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _hasSent = false;
            RequestSerialization();
        }

        #region WatchdogProc

        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        #endregion

        #region LocalProperties

        private int _localEventId;
        private int _localSenderId;
        private int _localAttackerId;
        private Vector3 _localActivatedPos;
        private Vector3 _localHitPos;
        private DateTime _localActivatedTime;
        private DateTime _localHitTime;

        private SyncState _localState;
        private SyncResult _localResult;
        private KillType _localType;
        private int _localVictimId;
        private string _localWeaponType;

        #endregion

        #region GlobalProperties

        [field: UdonSynced]
        public int EventId { get; private set; }
        [field: UdonSynced]
        public int SenderId { get; private set; }
        [field: UdonSynced]
        public int VictimId { get; private set; }
        [field: UdonSynced]
        public int AttackerId { get; private set; }
        [field: UdonSynced]
        public Vector3 ActivatedPosition { get; private set; }
        [field: UdonSynced]
        public Vector3 HitPosition { get; private set; }
        [field: UdonSynced]
        public long ActivatedTimeTicks { get; private set; }
        public DateTime ActivatedTime =>
            DateTime.MinValue.Ticks < ActivatedTimeTicks && DateTime.MaxValue.Ticks > ActivatedTimeTicks
                ? new DateTime(ActivatedTimeTicks)
                : DateTime.MinValue;
        [field: UdonSynced]
        public long HitTimeTicks { get; private set; }
        public DateTime HitTime =>
            DateTime.MinValue.Ticks < HitTimeTicks && DateTime.MaxValue.Ticks > HitTimeTicks
                ? new DateTime(HitTimeTicks)
                : DateTime.MinValue;
        [field: UdonSynced]
        public string WeaponType { get; private set; }
        [field: UdonSynced]
        public SyncState State { get; private set; } = SyncState.Unassigned;
        [field: UdonSynced]
        public SyncResult Result { get; private set; } = SyncResult.Unassigned;
        [field: UdonSynced]
        public KillType Type { get; private set; } = KillType.Default;

        #endregion

        #region NetworkingEvents

        public override void OnPreSerialization()
        {
            ApplyLocal();
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            LastUsedTime = Time.realtimeSinceStartup;
            _hasSent = result.success;
            if (!_hasSent)
            {
                Debug.LogError(
                    $"[DamageDataSyncer-{name}] Failed to send data@PostSerialization, Requesting sync again!");
                RequestSync();
                return;
            }

            if (!_hasProcessed)
            {
                // Set flag before requesting resolve, because resolve may call ResolverDataSyncer#SendReply
                _hasProcessed = true;
                manager.Receive(this);
            }
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            var previousTime = LastUsedTime;
            LastUsedTime = Time.realtimeSinceStartup;

            // TODO: test resending
            var mayHaveOverwritten = _localState == SyncState.Sending ||
                                     (previousTime + 1F > LastUsedTime && _localState == SyncState.Received);
            if (!_hasSent &&
                _localSenderId == Networking.LocalPlayer.playerId &&
                SenderId != _localSenderId &&
                EventId != _localEventId &&
                mayHaveOverwritten)
                manager.RequestResend(this);

            ApplyGlobal();

            // Set flag before requesting resolve, because resolve may call ResolverDataSyncer#SendReply
            _hasProcessed = true;
            manager.Receive(this);
        }

        #endregion

        #region GetReadableEnumNames

        public static string GetResultName(SyncResult r)
        {
            switch (r)
            {
                case SyncResult.Unassigned:
                    return "Unassigned";
                case SyncResult.Hit:
                    return "Hit";
                case SyncResult.Cancelled:
                    return "Cancelled";
                default:
                    return "Unknown";
            }
        }

        public static string GetStateName(SyncState r)
        {
            switch (r)
            {
                case SyncState.Unassigned:
                    return "Unassigned";
                case SyncState.Sending:
                    return "Sending";
                case SyncState.Received:
                    return "Received";
                default:
                    return "Unknown";
            }
        }

        public static string GetKillTypeName(KillType r)
        {
            switch (r)
            {
                case KillType.Default:
                    return "Default";
                case KillType.FriendlyFire:
                    return "FriendlyFire";
                default:
                    return "Unknown";
            }
        }

        #endregion
    }

    public static class ResolverDataSyncerExtensions
    {
        public static DataToken AsDataToken(this DamageDataSyncer syncer)
        {
            // U# does not support list collection initializer yet
            // ReSharper disable once UseObjectOrCollectionInitializer
            var damageData = new DataDictionary();
            damageData.Add("activatedTime", syncer.ActivatedTime.ToString("O"));
            damageData.Add("hitTime", syncer.HitTime.ToString("O"));
            damageData.Add("activatedPosition", AsDataToken(syncer.ActivatedPosition));
            damageData.Add("hitPosition", AsDataToken(syncer.HitPosition));
            damageData.Add("weaponName", syncer.WeaponType);

            // ReSharper disable once UseObjectOrCollectionInitializer
            var syncerData = new DataDictionary();
            syncerData.Add("senderId", syncer.SenderId);
            syncerData.Add("attackerId", syncer.AttackerId);
            syncerData.Add("victimId", syncer.VictimId);
            syncerData.Add("state", new DataToken(DamageDataSyncer.GetStateName(syncer.State)));
            syncerData.Add("result", new DataToken(DamageDataSyncer.GetResultName(syncer.Result)));
            syncerData.Add("type", new DataToken(DamageDataSyncer.GetKillTypeName(syncer.Type)));
            syncerData.Add("damageData", damageData);

            return new DataToken(syncerData);
        }

        private static DataToken AsDataToken(Vector3 value)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var reference = new DataDictionary();
            reference.Add("x", value.x);
            reference.Add("y", value.y);
            reference.Add("z", value.z);
            return new DataToken(reference);
        }
    }

    public enum SyncState
    {
        /// <summary>
        /// Initial state before syncer is receiving or sending data
        /// </summary>
        Unassigned = -2,
        /// <summary>
        /// Waiting for data to be received
        /// </summary>
        Sending = 1,
        /// <summary>
        /// Waiting for next data
        /// </summary>
        Received = 0,
    }

    public enum SyncResult
    {
        Unassigned = -1,
        Hit = 0,
        Cancelled = 1
    }
}