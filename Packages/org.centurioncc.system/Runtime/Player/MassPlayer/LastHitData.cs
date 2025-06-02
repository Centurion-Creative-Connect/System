using System;
using UdonSharp;
using UnityEngine;
using CenturionCC.System.Utils;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LastHitData : UdonSharpBehaviour
    {
        [SerializeField]
        private PlayerBase player;

        public PlayerBase Player => player;

        public int AttackerId { get; private set; }
        public int VictimId { get; private set; }
        public KillType Type { get; private set; } = KillType.Default;
        public BodyParts Parts { get; private set; } = BodyParts.Body;

        public Guid EventId { get; private set; }

        public Vector3 ActivatedPosition { get; private set; }

        public Vector3 HitPosition { get; private set; }

        public long ActivatedTimeTicks { get; private set; }

        public DateTime ActivatedTime =>
            DateTime.MinValue.Ticks < ActivatedTimeTicks && DateTime.MaxValue.Ticks > ActivatedTimeTicks
                ? new DateTime(ActivatedTimeTicks)
                : DateTime.MinValue;

        public long HitTimeTicks { get; private set; }

        public DateTime HitTime =>
            DateTime.MinValue.Ticks < HitTimeTicks && DateTime.MaxValue.Ticks > HitTimeTicks
                ? new DateTime(HitTimeTicks)
                : DateTime.MinValue;


        public string WeaponType { get; private set; }

        public float Distance { get; private set; }

        public void SetData(DamageInfo info, KillType type)
        {
            EventId = info.EventId();
            AttackerId = info.AttackerId();
            VictimId = info.VictimId();

            HitPosition = info.HitPosition();
            HitTimeTicks = info.HitTime().Ticks;
            Parts = info.HitParts();

            ActivatedPosition = info.OriginatedPosition();
            ActivatedTimeTicks = info.OriginatedTime().Ticks;

            WeaponType = info.DamageType();
            Distance = Vector3.Distance(ActivatedPosition, HitPosition);

            Type = type;
        }

        public void SetData(DamageDataSyncer syncer)
        {
            EventId = Guid.NewGuid();
            AttackerId = syncer.AttackerId;
            VictimId = syncer.VictimId;
            ActivatedPosition = syncer.ActivatedPosition;
            ActivatedTimeTicks = syncer.ActivatedTimeTicks;
            HitPosition = syncer.HitPosition;
            HitTimeTicks = syncer.HitTimeTicks;
            WeaponType = syncer.WeaponType;
            Type = syncer.Type;
            Parts = syncer.Parts;
            Distance = Vector3.Distance(ActivatedPosition, HitPosition);
        }

        public void ResetData()
        {
            EventId = default;
            AttackerId = default;
            VictimId = default;
            ActivatedPosition = default;
            ActivatedTimeTicks = default;
            HitPosition = default;
            HitTimeTicks = default;
            WeaponType = default;
            Type = default;
            Distance = default;
        }

        public void SyncData()
        {
            // do nothing for now
        }
    }
}