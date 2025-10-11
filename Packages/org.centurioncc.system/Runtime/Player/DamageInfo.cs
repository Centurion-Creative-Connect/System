using System;
using System.Text;
using CenturionCC.System.Utils;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    public enum DamageInfoFields
    {
        EventId,
        VictimId,
        AttackerId,
        HitPos,
        OriginPos,
        OriginRot,
        HitTime,
        HitParts,
        OriginTime,
        DamageType,
        DamageAmount,
        DetectionType,
        RespectFriendlyFireSetting,
        CanDamageSelf,
        CanDamageFriendly,
        CanDamageEnemy,

        Count
    }

    public class DamageInfo : DataList
    {
        private const int GuidSize = 16;
        private const int IntSize = sizeof(int);
        private const int FloatSize = sizeof(float);
        private const int LongSize = sizeof(long);
        private const int ByteSize = sizeof(byte);

#if !COMPILER_UDONSHARP
        private DamageInfo(DataToken[] data) : base(data)
        {
        }
#endif

        public static DamageInfo NewEmpty()
        {
            return New(
                Guid.Empty,
                -1, -1,
                Vector3.zero, BodyParts.Body,
                Vector3.zero, Quaternion.identity,
                DateTime.MinValue, DateTime.MinValue,
                "", 0,
                DetectionType.None,
                true, false, false, false
            );
        }

        public static DamageInfo New(VRCPlayerApi victim, Vector3 contactPoint, BodyParts contactParts, DamageData data)
        {
            return New(
                data.EventId,
                Utilities.IsValid(victim) ? victim.playerId : -1,
                data.DamagerPlayerId,
                contactPoint,
                contactParts,
                data.DamageOriginPosition,
                data.DamageOriginRotation,
                Networking.GetNetworkDateTime(),
                data.DamageOriginTime,
                data.DamageType,
                data.DamageAmount,
                data.DetectionType,
                data.RespectFriendlyFireSetting,
                data.CanDamageSelf,
                data.CanDamageFriendly,
                data.CanDamageEnemy
            );
        }

        public static DamageInfo New(Guid eventId,
            int victimId, int attackerId,
            Vector3 hitPos, BodyParts hitParts,
            Vector3 originPos, Quaternion originRot,
            DateTime hitTime, DateTime originTime,
            string damageType, float damageAmount,
            DetectionType detectionType, bool respectFriendlyFire,
            bool canDamageSelf, bool canDamageFriendly, bool canDamageEnemy)
        {
            var data = new DataToken[(int)DamageInfoFields.Count];
            data[(int)DamageInfoFields.EventId] = new DataToken(eventId);
            data[(int)DamageInfoFields.VictimId] = victimId;
            data[(int)DamageInfoFields.AttackerId] = attackerId;
            data[(int)DamageInfoFields.HitPos] = new DataToken(hitPos);
            data[(int)DamageInfoFields.HitParts] = new DataToken(hitParts);
            data[(int)DamageInfoFields.OriginPos] = new DataToken(originPos);
            data[(int)DamageInfoFields.OriginRot] = new DataToken(originRot);
            data[(int)DamageInfoFields.HitTime] = new DataToken(hitTime);
            data[(int)DamageInfoFields.OriginTime] = new DataToken(originTime);
            data[(int)DamageInfoFields.DamageType] = new DataToken(damageType);
            data[(int)DamageInfoFields.DamageAmount] = new DataToken(damageAmount);
            data[(int)DamageInfoFields.DetectionType] = new DataToken(detectionType);
            data[(int)DamageInfoFields.RespectFriendlyFireSetting] = new DataToken(respectFriendlyFire);
            data[(int)DamageInfoFields.CanDamageSelf] = new DataToken(canDamageSelf);
            data[(int)DamageInfoFields.CanDamageFriendly] = new DataToken(canDamageFriendly);
            data[(int)DamageInfoFields.CanDamageEnemy] = new DataToken(canDamageEnemy);

#if COMPILER_UDONSHARP
            return (DamageInfo)new DataList(data);
#else
            return new DamageInfo(data);
#endif
        }

        public static DamageInfo FromBytes(byte[] damageInfoBytes)
        {
            var offset = 0;

            var eventIdBytes = new byte[GuidSize];
            Buffer.BlockCopy(damageInfoBytes, offset, eventIdBytes, 0, GuidSize);

            var eventId = new Guid(eventIdBytes);
            offset += GuidSize;

            var victimId = BitConverter.ToInt32(damageInfoBytes, offset);
            offset += IntSize;

            var attackerId = BitConverter.ToInt32(damageInfoBytes, offset);
            offset += IntSize;

            var hitPos = EncodingUtil.ToVector3(damageInfoBytes, offset);
            offset += FloatSize * 3;

            var hitTimeTicks = BitConverter.ToInt64(damageInfoBytes, offset);
            offset += LongSize;

            var hitParts = (BodyParts)damageInfoBytes[offset];
            offset += ByteSize;

            var originPos = EncodingUtil.ToVector3(damageInfoBytes, offset);
            offset += FloatSize * 3;

            var originRot = EncodingUtil.ToQuaternion(damageInfoBytes, offset);
            offset += FloatSize * 4;

            var originTimeTicks = BitConverter.ToInt64(damageInfoBytes, offset);
            offset += LongSize;

            var damageTypeLength = BitConverter.ToInt32(damageInfoBytes, offset);
            offset += IntSize;

            var damageType = Encoding.UTF8.GetString(damageInfoBytes, offset, damageTypeLength);
            offset += damageTypeLength;

            var damageAmount = BitConverter.ToSingle(damageInfoBytes, offset);
            offset += FloatSize;

            EncodingUtil.ToDamageOptions(damageInfoBytes, offset,
                out var detectionType,
                out var respectFriendlyFire,
                out var canDamageSelf,
                out var canDamageFriendly,
                out var canDamageEnemy
            );

            return New(
                eventId,
                victimId,
                attackerId,
                hitPos,
                hitParts,
                originPos,
                originRot,
                DateTime.MinValue.Ticks <= hitTimeTicks && DateTime.MaxValue.Ticks >= hitTimeTicks
                    ? new DateTime(hitTimeTicks)
                    : DateTime.MinValue,
                DateTime.MinValue.Ticks <= originTimeTicks && DateTime.MaxValue.Ticks >= originTimeTicks
                    ? new DateTime(originTimeTicks)
                    : DateTime.MinValue,
                damageType,
                damageAmount,
                detectionType,
                respectFriendlyFire,
                canDamageSelf,
                canDamageFriendly,
                canDamageEnemy
            );
        }
    }

    public static class DamageInfoExt
    {
        private const int IntSize = sizeof(int);
        private const int FloatSize = sizeof(float);
        private const int LongSize = sizeof(long);
        private const int ByteSize = sizeof(byte);

        public static Guid EventId(this DamageInfo instance) =>
            (Guid)instance[(int)DamageInfoFields.EventId].Reference;

        public static int VictimId(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.VictimId].Int;

        public static int AttackerId(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.AttackerId].Int;

        public static Vector3 HitPosition(this DamageInfo instance) =>
            (Vector3)instance[(int)DamageInfoFields.HitPos].Reference;

        public static BodyParts HitParts(this DamageInfo instance) =>
            (BodyParts)instance[(int)DamageInfoFields.HitParts].Reference;

        public static Vector3 OriginatedPosition(this DamageInfo instance) =>
            (Vector3)instance[(int)DamageInfoFields.OriginPos].Reference;

        public static Quaternion OriginatedRotation(this DamageInfo instance) =>
            (Quaternion)instance[(int)DamageInfoFields.OriginRot].Reference;

        public static DateTime HitTime(this DamageInfo instance) =>
            (DateTime)instance[(int)DamageInfoFields.HitTime].Reference;

        public static DateTime OriginatedTime(this DamageInfo instance) =>
            (DateTime)instance[(int)DamageInfoFields.OriginTime].Reference;

        public static string DamageType(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.DamageType].String;

        public static float DamageAmount(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.DamageAmount].Float;

        public static DetectionType DetectionType(this DamageInfo instance) =>
            (DetectionType)instance[(int)DamageInfoFields.DetectionType].Reference;

        public static bool RespectFriendlyFireSetting(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.RespectFriendlyFireSetting].Boolean;

        public static bool CanDamageSelf(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.CanDamageSelf].Boolean;

        public static bool CanDamageFriendly(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.CanDamageFriendly].Boolean;

        public static bool CanDamageEnemy(this DamageInfo instance) =>
            instance[(int)DamageInfoFields.CanDamageEnemy].Boolean;

        public static DataDictionary ToDictionary(this DamageInfo instance)
        {
            var d = new DataDictionary();
            d.Add("eventId", instance.EventId().ToString("D"));
            d.Add("victim", DataDictionaryExtensions.ToPlayerDictionary(instance.VictimId()));
            d.Add("attacker", DataDictionaryExtensions.ToPlayerDictionary((instance.AttackerId())));
            d.Add("hit", DataDictionaryExtensions.ToDamageDictionary(
                instance.HitPosition(), instance.HitTime(), instance.HitParts()));
            d.Add("origin", DataDictionaryExtensions.ToDamageDictionary(
                instance.OriginatedPosition(), instance.OriginatedRotation(), instance.OriginatedTime()));
            d.Add("damageType", instance.DamageType());
            d.Add("amount", instance.DamageAmount());
            d.Add("detectionType", instance.DetectionType().ToEnumName());
            d.Add("respectFriendlyFireSetting", instance.RespectFriendlyFireSetting());
            d.Add("canDamageSelf", instance.CanDamageSelf());
            d.Add("canDamageFriendly", instance.CanDamageFriendly());
            d.Add("canDamageEnemy", instance.CanDamageEnemy());

            return d;
        }

        public static byte[] ToBytes(this DamageInfo instance)
        {
            var size = 0;

            var eventIdBytes = instance.EventId().ToByteArray();
            size += eventIdBytes.Length;

            var victimIdBytes = BitConverter.GetBytes(instance.VictimId());
            size += victimIdBytes.Length;

            var attackerIdBytes = BitConverter.GetBytes(instance.AttackerId());
            size += attackerIdBytes.Length;

            var hitPosBytes = EncodingUtil.GetBytes(instance.HitPosition());
            size += hitPosBytes.Length;

            var hitTimeTickBytes = BitConverter.GetBytes(instance.HitTime().Ticks);
            size += hitTimeTickBytes.Length;

            var hitPartBytes = new[] { instance.HitParts().ToByte() };
            size += hitPartBytes.Length;

            var originPosBytes = EncodingUtil.GetBytes(instance.OriginatedPosition());
            size += originPosBytes.Length;

            var originRotBytes = EncodingUtil.GetBytes(instance.OriginatedRotation());
            size += originRotBytes.Length;

            var originTimeTickBytes = BitConverter.GetBytes(instance.OriginatedTime().Ticks);
            size += originTimeTickBytes.Length;

            var damageTypeBytes = Encoding.UTF8.GetBytes(instance.DamageType());
            size += damageTypeBytes.Length;
            size += IntSize;

            var damageAmountBytes = BitConverter.GetBytes(instance.DamageAmount());
            size += damageAmountBytes.Length;

            var damageOptionBytes = EncodingUtil.GetBytes(
                instance.DetectionType(),
                instance.RespectFriendlyFireSetting(),
                instance.CanDamageSelf(),
                instance.CanDamageFriendly(),
                instance.CanDamageEnemy()
            );
            size += damageOptionBytes.Length;

            var output = new byte[size];
            var offset = 0;

            Buffer.BlockCopy(eventIdBytes, 0, output, offset, eventIdBytes.Length);
            offset += eventIdBytes.Length;

            Buffer.BlockCopy(victimIdBytes, 0, output, offset, victimIdBytes.Length);
            offset += victimIdBytes.Length;
            Buffer.BlockCopy(attackerIdBytes, 0, output, offset, attackerIdBytes.Length);
            offset += attackerIdBytes.Length;
            Buffer.BlockCopy(hitPosBytes, 0, output, offset, hitPosBytes.Length);
            offset += hitPosBytes.Length;
            Buffer.BlockCopy(hitTimeTickBytes, 0, output, offset, hitTimeTickBytes.Length);
            offset += hitTimeTickBytes.Length;
            Buffer.BlockCopy(hitPartBytes, 0, output, offset, hitPartBytes.Length);
            offset += hitPartBytes.Length;
            Buffer.BlockCopy(originPosBytes, 0, output, offset, originPosBytes.Length);
            offset += originPosBytes.Length;
            Buffer.BlockCopy(originRotBytes, 0, output, offset, originRotBytes.Length);
            offset += originRotBytes.Length;
            Buffer.BlockCopy(originTimeTickBytes, 0, output, offset, originTimeTickBytes.Length);
            offset += originTimeTickBytes.Length;
            Buffer.BlockCopy(BitConverter.GetBytes(damageTypeBytes.Length), 0, output, offset, IntSize);
            offset += IntSize;
            Buffer.BlockCopy(damageTypeBytes, 0, output, offset, damageTypeBytes.Length);
            offset += damageTypeBytes.Length;
            Buffer.BlockCopy(damageAmountBytes, 0, output, offset, damageAmountBytes.Length);
            offset += damageAmountBytes.Length;
            Buffer.BlockCopy(damageOptionBytes, 0, output, offset, damageOptionBytes.Length);
            return output;
        }

        public static bool IsEmpty(this DamageInfo instance)
        {
            return Guid.Empty == instance.EventId();
        }
    }
}