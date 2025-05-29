using System;
using CenturionCC.System.Player;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
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

            return (DamageInfo)new DataList(data);
        }
    }

    public static class DamageInfoExt
    {
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
            const int guidBytes = 16;
            const int vec3Bytes = 12;
            const int quatBytes = 16;
            const int totalLen = guidBytes + sizeof(int) * 2 + sizeof(long) * 2 + sizeof(float) + vec3Bytes * 2 +
                                 quatBytes;
            return new byte[0];
        }
    }
}