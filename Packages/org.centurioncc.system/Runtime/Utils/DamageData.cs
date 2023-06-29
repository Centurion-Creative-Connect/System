using System;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    public abstract class DamageData : UdonSharpBehaviour
    {
        public abstract bool ShouldApplyDamage { get; }
        public abstract int DamagerPlayerId { get; }
        public abstract Vector3 DamageOriginPosition { get; }
        public abstract Quaternion DamageOriginRotation { get; }
        public abstract DateTime DamageOriginTime { get; }
        public abstract string DamageType { get; }
    }
}