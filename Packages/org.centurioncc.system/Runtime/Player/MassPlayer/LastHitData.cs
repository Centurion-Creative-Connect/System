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


        public int AttackerId { get; private set; }
        public int VictimId { get; private set; }
        public KillType Type { get; private set; } = KillType.Default;
        public BodyParts Parts { get; private set; } = BodyParts.Body;


        [field: UdonSynced]
        public int EventId { get; private set; }


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

        /// <summary>
        /// 3 bytes encoded in int.
        /// (byte)AttackerId &lt;&lt; 0;
        /// (byte)Type &lt;&lt; 8;
        /// (byte)Parts &lt;&lt; 16;
        /// </summary>
        [field: UdonSynced]
        public int EncodedData { get; private set; }

        private void Start()
        {
            if (Networking.IsMaster)
                _hasInit = true;
        }

        public void SetData(DamageDataSyncer syncer)
        {
            EventId = syncer.EventId;
            AttackerId = syncer.AttackerId;
            VictimId = syncer.VictimId;
            ActivatedPosition = syncer.ActivatedPosition;
            ActivatedTimeTicks = syncer.ActivatedTimeTicks;
            HitPosition = syncer.HitPosition;
            HitTimeTicks = syncer.HitTimeTicks;
            WeaponType = syncer.WeaponType;
            Type = syncer.Type;
            Parts = syncer.Parts;

            EncodedData = EncodeData(AttackerId, VictimId, (int)Type, (int)Parts);
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

            EncodedData = EncodeData(default, default, default, default);
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
                _lastEventId = EventId;
                return;
            }

            if (EventId == _lastEventId || EventId == default)
                return;

            _lastEventId = EventId;

            DecodeData(EncodedData, out var attacker, out var victim, out var type, out var parts);

            AttackerId = attacker;
            VictimId = victim;
            Type = type;
            Parts = parts;

            player.OnHitDataUpdated();
        }

        public static int EncodeData(int attacker, int victim, int type, int parts)
        {
            int encoded = (byte)attacker;
            encoded += (int)((byte)victim) << 8;
            encoded += (int)((byte)type) << 16;
            encoded += (int)((byte)parts) << 24;

            return encoded;
        }

        public static void DecodeData(int data, out int attacker, out int victim, out KillType type,
            out BodyParts parts)
        {
            // NOTE: This converts VRCPlayerApi.playerId(int) to byte. Which might cause issues in public instances. 
            attacker = (byte)(data & 0xFF);
            victim = (byte)((data >> 8) & 0xFF);
            type = (KillType)(byte)((data >> 16) & 0xFF);
            parts = (BodyParts)(byte)((data >> 24) & 0xFF);
        }
    }
}