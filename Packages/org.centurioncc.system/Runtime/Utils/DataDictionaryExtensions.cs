using System;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    public static class DataDictionaryExtensions
    {
        [PublicAPI]
        public static DataDictionary ToDictionary(this ProjectileBase projectile)
        {
            var dict = new DataDictionary();
            dict.Add("attacker", ToPlayerDictionary(projectile.DamagerPlayerId));
            dict.Add("damageType", projectile.DamageType);
            dict.Add("detectionType", projectile.DetectionType.ToEnumName());

            var originDict = new DataDictionary();
            originDict.Add("position", projectile.DamageOriginPosition.ToDictionary());
            originDict.Add("rotation", projectile.DamageOriginRotation.ToDictionary());
            originDict.Add("time", projectile.DamageOriginTime.ToString("O"));

            dict.Add("origin", originDict);
            return dict;
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
        public static DataDictionary ToDictionary(this Quaternion q)
        {
            var d = new DataDictionary();
            d.Add("x", q.x);
            d.Add("y", q.y);
            d.Add("z", q.z);
            d.Add("w", q.w);
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
        public static DataDictionary ToDamageDictionary(Vector3 position, DateTime time, BodyParts parts)
        {
            var d = ToDamageDictionary(position, time);
            d.Add("parts", parts.ToEnumName());
            return d;
        }

        [PublicAPI]
        public static DataDictionary ToDamageDictionary(Vector3 position, Quaternion rotation, DateTime time)
        {
            var d = new DataDictionary();
            d.Add("position", ToDictionary(position));
            d.Add("rotation", ToDictionary(rotation));
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