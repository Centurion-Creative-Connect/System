using CenturionCC.System.Player;
using JetBrains.Annotations;
using System;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    public abstract class DamageData : UdonSharpBehaviour
    {
        [PublicAPI] public abstract Guid EventId { get; }
        [PublicAPI] public abstract bool ShouldApplyDamage { get; }
        [PublicAPI] public abstract int DamagerPlayerId { get; }
        [PublicAPI] public abstract Vector3 DamageOriginPosition { get; }
        [PublicAPI] public abstract Quaternion DamageOriginRotation { get; }
        [PublicAPI] public abstract DateTime DamageOriginTime { get; }
        [PublicAPI] public abstract string DamageType { get; }

        [PublicAPI] public virtual float DamageAmount { get; protected set; } = 100;
        [PublicAPI] public virtual DetectionType DetectionType { get; protected set; } = DetectionType.All;
        [PublicAPI] public virtual bool RespectFriendlyFireSetting { get; protected set; } = true;
        [PublicAPI] public virtual bool CanDamageSelf { get; protected set; } = false;
        [PublicAPI] public virtual bool CanDamageFriendly { get; protected set; } = true;
        [PublicAPI] public virtual bool CanDamageEnemy { get; protected set; } = true;
        [PublicAPI] public virtual float GetDamageMultiplier(BodyParts bodyParts) => 1;
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
