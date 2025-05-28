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

        public virtual DetectionType DetectionType { get; protected set; } = DetectionType.All;
        public virtual bool RespectFriendlyFireSetting { get; protected set; } = true;
        public virtual bool CanDamageSelf { get; protected set; } = false;
        public virtual bool CanDamageFriendly { get; protected set; } = true;
        public virtual bool CanDamageEnemy { get; protected set; } = true;
    }

    public enum DetectionType
    {
        /// <summary>
        /// Use both Local and Remote detection to determine.
        /// </summary>
        /// <remarks>
        /// Friendly damage must only be determined at local.
        /// </remarks>
        All,

        /// <summary>
        /// Use only attacker client visual to determine.
        /// </summary>
        /// <remarks>
        /// Friendly damage must only be determined at local.
        /// </remarks>
        AttackerSide,

        /// <summary>
        /// Use only victim client visual to determine.
        /// </summary>
        /// <remarks>
        /// Friendly damage must only be determined at local.
        /// </remarks>
        VictimSide
    }
}