using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LastHitData : UdonSharpBehaviour
    {
        [SerializeField]
        private PlayerBase player;

        private bool _hasInit;
        private int _lastEventId;

        public PlayerBase Player => player;

        [field: UdonSynced]
        public int EventId { get; private set; }
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
        public KillType Type { get; private set; } = KillType.Default;

        private void Start()
        {
            if (Networking.IsMaster)
                _hasInit = true;
        }

        public void SetData(DamageDataSyncer syncer)
        {
            EventId = syncer.EventId;
            AttackerId = syncer.AttackerId;
            ActivatedPosition = syncer.ActivatedPosition;
            ActivatedTimeTicks = syncer.ActivatedTimeTicks;
            HitPosition = syncer.HitPosition;
            HitTimeTicks = syncer.HitTimeTicks;
            WeaponType = syncer.WeaponType;
            Type = syncer.Type;
        }

        public void ResetData()
        {
            EventId = default;
            AttackerId = default;
            ActivatedPosition = default;
            ActivatedTimeTicks = default;
            HitPosition = default;
            HitTimeTicks = default;
            WeaponType = default;
            Type = default;
        }

        public void SyncData()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();

            if (EventId == _lastEventId || EventId == default)
                return;

            _lastEventId = EventId;
            player.OnHitDataUpdated();
        }

        public override void OnDeserialization()
        {
            if (!_hasInit)
            {
                _hasInit = true;
                return;
            }

            if (EventId == _lastEventId || EventId == default)
                return;

            _lastEventId = EventId;
            player.OnHitDataUpdated();
        }
    }
}