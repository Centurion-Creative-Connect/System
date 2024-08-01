using System;
using CenturionCC.System.Player;
using CenturionCC.System.Player.MassPlayer;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    public static class DataDictionaryExtensions
    {
        [PublicAPI]
        public static DataDictionary ToDictionary(this LastHitData hitData)
        {
            var d = new DataDictionary();
            d.Add("eventId", hitData.EventId);
            d.Add("attacker", ToPlayerDictionary(hitData.AttackerId));
            d.Add("victim", ToPlayerDictionary(hitData.Player.PlayerId));
            d.Add("killType", hitData.Type.ToEnumName());
            d.Add("bodyParts", hitData.Parts.ToEnumName());
            d.Add("weapon", hitData.WeaponType);
            d.Add("origin", ToDamageDictionary(hitData.ActivatedPosition, hitData.ActivatedTime));
            d.Add("hit", ToDamageDictionary(hitData.HitPosition, hitData.HitTime));
            return d;
        }

        [PublicAPI]
        public static DataDictionary ToDictionary(this DamageDataSyncer syncer)
        {
            // U# does not support list collection initializer yet
            // ReSharper disable once UseObjectOrCollectionInitializer
            var damageData = new DataDictionary();
            damageData.Add("activatedTime", syncer.ActivatedTime.ToString("O"));
            damageData.Add("hitTime", syncer.HitTime.ToString("O"));
            damageData.Add("activatedPosition", syncer.ActivatedPosition.ToDictionary());
            damageData.Add("hitPosition", syncer.HitPosition.ToDictionary());
            damageData.Add("weaponName", syncer.WeaponType);

            // ReSharper disable once UseObjectOrCollectionInitializer
            var syncerData = new DataDictionary();
            syncerData.Add("senderId", syncer.SenderId);
            syncerData.Add("attackerId", syncer.AttackerId);
            syncerData.Add("victimId", syncer.VictimId);
            syncerData.Add("state", new DataToken(DamageDataSyncer.GetStateName(syncer.State)));
            syncerData.Add("result", new DataToken(DamageDataSyncer.GetResultName(syncer.Result)));
            syncerData.Add("resultContext", syncer.ResultContext);
            syncerData.Add("type", new DataToken(DamageDataSyncer.GetKillTypeName(syncer.Type)));
            syncerData.Add("parts", new DataToken(DamageDataSyncer.GetBodyPartsName(syncer.Parts)));
            syncerData.Add("damageData", damageData);

            return syncerData;
        }

        [PublicAPI]
        public static DataDictionary ToDictionary(this Vector3 vec3)
        {
            var d = new DataDictionary();
            d.Add("x", vec3.x);
            d.Add("y", vec3.y);
            d.Add("z", vec3.z);
            return d;
        }

        [PublicAPI]
        public static DataDictionary ToDamageDictionary(Vector3 position, DateTime time)
        {
            var d = new DataDictionary();
            d.Add("position", ToDictionary(position));
            d.Add("time", time.ToString("O"));
            return d;
        }

        [PublicAPI]
        public static DataDictionary ToPlayerDictionary(int playerId)
        {
            var d = new DataDictionary();
            d.Add("id", playerId);
            d.Add("name", VRCPlayerApi.GetPlayerById(playerId).SafeGetDisplayName());
            return d;
        }
    }
}