using System;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ResolverDataSyncer : UdonSharpBehaviour
    {
        [NewbieInject] [HideInInspector] [SerializeField]
        private DamageDataResolverBase resolver;

        private bool _hasSent = true;

        public bool IsAvailable => _hasSent && Result != HitResult.Waiting;
        public int ResendCount { get; private set; }
        public float LastUsedTime { get; private set; }

        public void Resend(ResolverDataSyncer other)
        {
            LastUsedTime = Time.realtimeSinceStartup;

            _localVictimId = other._localVictimId;
            _localAttackerId = other._localAttackerId;
            _localOriginPos = other._localOriginPos;
            _localHitPos = other._localHitPos;
            _localOriginTime = other._localOriginTime;
            _localWeaponType = other._localWeaponType;
            _localRequest = other._localRequest;
            _localResult = other._localResult;
            _localType = other._localType;

            ResendCount = ++other.ResendCount;

            RequestSync();
        }

        public void SendReply(HitResult result)
        {
            LastUsedTime = Time.realtimeSinceStartup;

            _localResult = result;

            RequestSync();
        }

        public void Send(
            int victimId,
            int attackerId,
            Vector3 originPos,
            Vector3 hitPos,
            DateTime originTime,
            string weaponType,
            ResolveRequest request,
            HitResult result,
            KillType type
        )
        {
            LastUsedTime = Time.realtimeSinceStartup;

            _localVictimId = victimId;
            _localAttackerId = attackerId;
            _localOriginPos = originPos;
            _localHitPos = hitPos;
            _localOriginTime = originTime;
            _localWeaponType = weaponType;

            _localRequest = request;
            _localResult = result;
            _localType = type;
            ResendCount = 0;

            RequestSync();
        }

        public void ApplyLocal()
        {
            SenderId = Networking.LocalPlayer.playerId;
            VictimId = _localVictimId;
            AttackerId = _localAttackerId;
            OriginPosition = _localOriginPos;
            HitPosition = _localHitPos;
            OriginTimeTicks = _localOriginTime.Ticks;
            WeaponType = _localWeaponType;

            Request = _localRequest;
            Result = _localResult;
            Type = _localType;
        }

        public void ApplyGlobal()
        {
            _localVictimId = VictimId;
            _localAttackerId = AttackerId;
            _localOriginPos = OriginPosition;
            _localHitPos = HitPosition;
            _localOriginTime = OriginTime;
            _localWeaponType = WeaponType;

            _localRequest = Request;
            _localResult = Result;
        }

        public void MakeAvailable()
        {
            _hasSent = true;
            Result = HitResult.Unassigned;
        }

        public string GetGlobalInfo()
        {
            return $"{name}, " +
                   $"{SenderId}/" +
                   $"{AttackerId}->" +
                   $"{VictimId}@" +
                   $"{OriginTime}:" +
                   $"{GetRequestName(Request)}:" +
                   $"{GetResultName(Result)}:" +
                   $"{GetKillTypeName(Type)}|" +
                   $"{ResendCount}";
        }

        public string GetLocalInfo()
        {
            return $"{name}, " +
                   $"{(_hasSent ? $"{SenderId}" : $"{Networking.LocalPlayer.playerId}?")}/" +
                   $"{_localAttackerId}->" +
                   $"{_localVictimId}@" +
                   $"{_localOriginTime}:" +
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

        private int _localAttackerId;
        private Vector3 _localHitPos;
        private Vector3 _localOriginPos;
        private DateTime _localOriginTime;

        private ResolveRequest _localRequest;
        private HitResult _localResult;
        private KillType _localType;
        private int _localVictimId;
        private string _localWeaponType;

        #endregion

        #region GlobalProperties

        [field: UdonSynced]
        public int SenderId { get; private set; }
        [field: UdonSynced]
        public int VictimId { get; private set; }
        [field: UdonSynced]
        public int AttackerId { get; private set; }
        [field: UdonSynced]
        public Vector3 OriginPosition { get; private set; }
        [field: UdonSynced]
        public Vector3 HitPosition { get; private set; }
        [field: UdonSynced]
        public long OriginTimeTicks { get; private set; }
        public DateTime OriginTime =>
            DateTime.MinValue.Ticks < OriginTimeTicks && DateTime.MaxValue.Ticks > OriginTimeTicks
                ? new DateTime(OriginTimeTicks)
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
                RequestSync();
                return;
            }

            ResendCount = 0;
            if (Result != HitResult.Waiting)
                resolver.RequestResolve(this);
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            LastUsedTime = Time.realtimeSinceStartup;
            if (!_hasSent)
                resolver.RequestResend(this);

            ApplyGlobal();

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