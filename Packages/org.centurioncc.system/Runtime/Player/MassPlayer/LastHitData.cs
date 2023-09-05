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

        public void SyncData()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            player.OnHitDataUpdated();
        }

        public override void OnDeserialization()
        {
            player.OnHitDataUpdated();
        }
    }
}