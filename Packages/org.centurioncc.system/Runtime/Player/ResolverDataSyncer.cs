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
        private DamageDataResolver resolver;

        private bool _hasSent = true;

        private int _localAttackerId;
        private Vector3 _localHitPos;
        private Vector3 _localOriginPos;
        private DateTime _localOriginTime;

        private ResolveRequest _localRequest;
        private HitResult _localResult;
        private int _localVictimId;
        private string _localWeaponType;

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
        public ResolveRequest Request { get; private set; }
        [field: UdonSynced]
        public HitResult Result { get; private set; } = HitResult.Fail;

        public bool IsAvailable => _hasSent && Result != HitResult.None;
        public int ResendCount { get; private set; }

        public float LastUsedTime { get; private set; }

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
            HitResult result
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
            Result = HitResult.Fail;
        }

        private void RequestSync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _hasSent = false;
            RequestSerialization();
        }
    }
}