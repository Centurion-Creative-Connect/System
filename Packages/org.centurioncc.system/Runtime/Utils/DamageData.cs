using System;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    public abstract class DamageData : UdonSharpBehaviour
    {
        public abstract Guid EventId { get; }
        public abstract bool ShouldApplyDamage { get; }
        public abstract int DamagerPlayerId { get; }
        public abstract Vector3 DamageOriginPosition { get; }
        public abstract Quaternion DamageOriginRotation { get; }
        public abstract DateTime DamageOriginTime { get; }
        public abstract string DamageType { get; }

        public virtual float DamageAmount => 100;

        public virtual DetectionType DetectionType { get; protected set; } = DetectionType.All;
        public virtual bool RespectFriendlyFireSetting { get; protected set; } = true;
        public virtual bool CanDamageSelf { get; protected set; } = false;
        public virtual bool CanDamageFriendly { get; protected set; } = true;
        public virtual bool CanDamageEnemy { get; protected set; } = true;
    }

    public enum DetectionType
    {
        /// <summary>
        /// Disabled.
        /// </summary>
        None = 0,

        /// <summary>
        /// Use both Local and Remote detection to determine.
        /// </summary>
        /// <remarks>
        /// Friendly damage must only be determined at local.
        /// </remarks>
        All = 3,

        /// <summary>
        /// Use only attacker client visual to determine.
        /// </summary>
        /// <remarks>
        /// Friendly damage must only be determined at local.
        /// </remarks>
        AttackerSide = 1,

        /// <summary>
        /// Use only victim client visual to determine.
        /// </summary>
        /// <remarks>
        /// Friendly damage must only be determined at local.
        /// </remarks>
        VictimSide = 2,
    }

    public static class DetectionTypeExtensions
    {
        public static string ToEnumName(this DetectionType type)
        {
            switch (type)
            {
                case DetectionType.All:
                    return "All";
                case DetectionType.AttackerSide:
                    return "AttackerSide";
                case DetectionType.VictimSide:
                    return "VictimSide";
                default:
                    return "UNKNOWN";
            }
        }

        public static byte ToByte(this DetectionType type)
        {
            switch (type)
            {
                case DetectionType.All:
                    return 3;
                case DetectionType.AttackerSide:
                    return 1;
                case DetectionType.VictimSide:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}