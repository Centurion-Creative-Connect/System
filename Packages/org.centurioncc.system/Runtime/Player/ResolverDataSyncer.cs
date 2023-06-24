using System;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ResolverDataSyncer : UdonSharpBehaviour
    {
        [NewbieInject] [HideInInspector] [SerializeField]
        private DamageDataResolverBase resolver;
        private bool _hasResolved = false;

        private bool _hasSent = true;

        public bool IsAvailable => _hasSent && Result != HitResult.Waiting;
        public int ResendCount { get; private set; }
        public float LastUsedTime { get; private set; }

        public void Resend(ResolverDataSyncer other)
        {
            _hasResolved = other._hasResolved;

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
            _localRequest = other._localRequest;
            _localResult = other._localResult;
            _localType = other._localType;

            ResendCount = ++other.ResendCount;

            RequestSync();
        }

        public void SendReply(HitResult result)
        {
            _hasResolved = false;

            LastUsedTime = Time.realtimeSinceStartup;

            _localResult = result;

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
            ResolveRequest request,
            HitResult result,
            KillType type
        )
        {
            _hasResolved = false;

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

            _localRequest = request;
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

            Request = _localRequest;
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

            _localRequest = Request;
            _localResult = Result;
        }

        public void MakeAvailable()
        {
            _hasSent = true;
            _hasResolved = false;
            Result = HitResult.Unassigned;
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
                   $"{GetRequestName(Request)}:" +
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
                   $"{GetRequestName(_localRequest)}:" +
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

        #region LocalProperties

        private int _localEventId;
        private int _localSenderId;
        private int _localAttackerId;
        private Vector3 _localActivatedPos;
        private Vector3 _localHitPos;
        private DateTime _localActivatedTime;
        private DateTime _localHitTime;

        private ResolveRequest _localRequest;
        private HitResult _localResult;
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
        public ResolveRequest Request { get; private set; } = ResolveRequest.Unassigned;
        [field: UdonSynced]
        public HitResult Result { get; private set; } = HitResult.Unassigned;
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
                    $"[ResolverDataSyncer-{name}] Failed to send data@PostSerialization, Requesting sync again!");
                RequestSync();
                return;
            }

            if (!_hasResolved)
            {
                // Set flag before requesting resolve, because resolve may call ResolverDataSyncer#SendReply
                _hasResolved = true;
                resolver.RequestResolve(this);
            }
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            var previousTime = LastUsedTime;
            LastUsedTime = Time.realtimeSinceStartup;

            // TODO: test resending
            var mayHaveOverwritten = _localResult == HitResult.Waiting ||
                                     (previousTime + 1F > LastUsedTime && _localResult == HitResult.Hit);
            if (!_hasSent &&
                _localSenderId == Networking.LocalPlayer.playerId &&
                SenderId != _localSenderId &&
                EventId != _localEventId &&
                mayHaveOverwritten)
                resolver.RequestResend(this);

            ApplyGlobal();

            // Set flag before requesting resolve, because resolve may call ResolverDataSyncer#SendReply
            _hasResolved = true;
            resolver.RequestResolve(this);
        }

        #endregion

        #region GetReadableEnumNames

        public static string GetRequestName(ResolveRequest r)
        {
            switch (r)
            {
                case ResolveRequest.Unassigned:
                    return "Unassigned";
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

        public static string GetResultName(HitResult r)
        {
            switch (r)
            {
                case HitResult.Unassigned:
                    return "Unassigned";
                case HitResult.Waiting:
                    return "Waiting";
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
        public static DataToken AsDataToken(this ResolverDataSyncer syncer)
        {
            // U# does not support list collection initializer yet
            // ReSharper disable once UseObjectOrCollectionInitializer
            var damageData = new DataDictionary();
            damageData.Add("activatedTime", syncer.ActivatedTime.ToString("s"));
            damageData.Add("hitTime", syncer.HitTime.ToString("s"));
            damageData.Add("activatedPosition", AsDataToken(syncer.ActivatedPosition));
            damageData.Add("hitPosition", AsDataToken(syncer.HitPosition));
            damageData.Add("weaponName", syncer.WeaponType);

            // ReSharper disable once UseObjectOrCollectionInitializer
            var syncerData = new DataDictionary();
            syncerData.Add("senderId", syncer.SenderId);
            syncerData.Add("attackerId", syncer.AttackerId);
            syncerData.Add("victimId", syncer.VictimId);
            syncerData.Add("request", (sbyte)syncer.Request);
            syncerData.Add("result", (sbyte)syncer.Result);
            syncerData.Add("type", (sbyte)syncer.Type);
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

    public enum ResolveRequest : sbyte
    {
        /// <summary>
        /// Initial state before syncer is receiving or sending data
        /// </summary>
        Unassigned = -2,
        /// <summary>
        /// Requesting to attacker to resolve
        /// </summary>
        ToAttacker = 1,
        /// <summary>
        /// Requesting to victim to resolve
        /// </summary>
        ToVictim = 2,
        /// <summary>
        /// Has resolved locally, and broadcasting a result
        /// </summary>
        SelfResolved = 3,
    }

    public enum HitResult : sbyte
    {
        /// <summary>
        /// Initial state before syncer is receiving or sending data
        /// </summary>
        Unassigned = -2,
        /// <summary>
        /// Waiting for resolving
        /// </summary>
        Waiting = -1,
        /// <summary>
        /// Success result
        /// </summary>
        Hit = 0,
        /// <summary>
        /// Failed result
        /// </summary>
        Fail = 1,
        /// <summary>
        /// Failed result because attacker was already dead
        /// </summary>
        FailByAttackerDead = 2,
        /// <summary>
        /// Failed result because victim was already dead
        /// </summary>
        FailByVictimDead = 3
    }
}