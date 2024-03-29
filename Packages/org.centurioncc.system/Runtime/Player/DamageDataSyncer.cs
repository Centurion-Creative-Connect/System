using System;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
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
            _localResultContext = other._localResultContext;

            ResendCount = ++other.ResendCount;

            RequestSync();
        }

        public void UpdateResult(SyncResult result, byte resultContext)
        {
            _hasProcessed = false;

            LastUsedTime = Time.realtimeSinceStartup;

            _localResult = result;
            _localResultContext = resultContext;
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
            KillType type,
            BodyParts parts,
            byte resultContext
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
            _localParts = parts;
            _localResultContext = resultContext;

            ResendCount = 0;

            RequestSync();
        }

        public void ApplyLocal()
        {
            EventId = _localEventId;
            ActivatedPosition = _localActivatedPos;
            HitPosition = _localHitPos;
            ActivatedTimeTicks = _localActivatedTime.Ticks;
            HitTimeTicks = _localHitTime.Ticks;
            WeaponType = _localWeaponType;

            EncodedData = EncodeData(
                _localSenderId, _localVictimId, _localAttackerId,
                (int)_localState, (int)_localResult, (int)_localType, (int)_localParts
            );

            SenderId = _localSenderId;
            VictimId = _localVictimId;
            AttackerId = _localAttackerId;
            State = _localState;
            Result = _localResult;
            Type = _localType;
            Parts = _localParts;
            ResultContext = _localResultContext;
        }

        public void ApplyGlobal()
        {
            DecodeData(
                EncodedData,
                out var sender, out var victim, out var attacker,
                out var syncState, out var syncResult, out var killType, out var parts
            );

            SenderId = sender;
            VictimId = victim;
            AttackerId = attacker;
            State = syncState;
            Result = syncResult;
            Type = killType;
            Parts = parts;

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
            _localParts = Parts;
            _localResultContext = ResultContext;
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
                   $"{GetResultName(Result)}({ResultContext}):" +
                   $"{GetKillTypeName(Type)}:" +
                   $"{GetBodyPartsName(Parts)}|" +
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
                   $"{GetResultName(_localResult)}({_localResultContext}):" +
                   $"{GetKillTypeName(_localType)}:" +
                   $"{GetBodyPartsName(_localParts)}|" +
                   $"{ResendCount}";
        }

        private void RequestSync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _hasSent = false;
            RequestSerialization();
        }

        public static long EncodeData(int sender, int victim, int attacker, int state, int result, int type, int parts)
        {
            // NOTE: This converts VRCPlayerApi.playerId(int) to byte. Which might cause issues in public instances. 

            long encoded = (byte)sender;
            encoded += (long)((byte)victim) << 8;
            encoded += (long)((byte)attacker) << 16;
            encoded += (long)((byte)state) << 24;
            encoded += (long)((byte)result) << 32;
            encoded += (long)((byte)type) << 40;
            encoded += (long)((byte)parts) << 48;
            return encoded;
        }

        public static void DecodeData(long data,
            out int sender, out int victim, out int attacker,
            out SyncState state, out SyncResult result, out KillType type, out BodyParts parts)
        {
            sender = (byte)(data & 0xFF);
            victim = (byte)((data >> 8) & 0xFF);
            attacker = (byte)((data >> 16) & 0xFF);
            state = (SyncState)(byte)((data >> 24) & 0xFF);
            result = (SyncResult)(byte)((data >> 32) & 0xFF);
            type = (KillType)(byte)((data >> 40) & 0xFF);
            parts = (BodyParts)(byte)((data >> 48) & 0xFF);
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
        private BodyParts _localParts;
        private int _localVictimId;
        private string _localWeaponType;
        private byte _localResultContext;

        #endregion

        #region GlobalProperties

        [field: UdonSynced]
        public int EventId { get; private set; }
        public int SenderId { get; private set; }
        public int VictimId { get; private set; }
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
        public SyncState State { get; private set; } = SyncState.Unassigned;
        public SyncResult Result { get; private set; } = SyncResult.Unassigned;
        public KillType Type { get; private set; } = KillType.Default;
        public BodyParts Parts { get; private set; } = BodyParts.Body;
        [field: UdonSynced]
        public byte ResultContext { get; private set; }
        [field: UdonSynced]
        public long EncodedData { get; private set; }

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

            DecodeData(EncodedData,
                out var sender, out var victim, out var attacker,
                out var syncState, out var syncResult, out var killType, out var parts
            );

            SenderId = sender;
            VictimId = victim;
            AttackerId = attacker;
            State = syncState;
            Result = syncResult;
            Type = killType;
            Parts = parts;

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
                case SyncResult.Unassigned: return "Unassigned";
                case SyncResult.Hit: return "Hit";
                case SyncResult.Cancelled: return "Cancelled";
                default: return "Unknown";
            }
        }

        public static string GetStateName(SyncState r)
        {
            switch (r)
            {
                case SyncState.Unassigned: return "Unassigned";
                case SyncState.Sending: return "Sending";
                case SyncState.Received: return "Received";
                default: return "Unknown";
            }
        }

        public static string GetKillTypeName(KillType r)
        {
            switch (r)
            {
                case KillType.Default: return "Default";
                case KillType.FriendlyFire: return "FriendlyFire";
                default: return "Unknown";
            }
        }

        public static string GetBodyPartsName(BodyParts r)
        {
            switch (r)
            {
                case BodyParts.Body: return "Body";
                case BodyParts.Head: return "Head";
                case BodyParts.LeftArm: return "LeftArm";
                case BodyParts.LeftLeg: return "LeftLeg";
                case BodyParts.RightArm: return "RightArm";
                case BodyParts.RightLeg: return "RightLeg";
                default: return "Unknown";
            }
        }

        #endregion
    }

    public enum SyncState
    {
        /// <summary>
        /// Initial state before syncer is receiving or sending data
        /// </summary>
        Unassigned = 0,
        /// <summary>
        /// Waiting for data to be received
        /// </summary>
        Sending = 1,
        /// <summary>
        /// Waiting for next data
        /// </summary>
        Received = 2,
    }

    public enum SyncResult
    {
        Unassigned = 0,
        Hit = 1,
        Cancelled = 2
    }
}