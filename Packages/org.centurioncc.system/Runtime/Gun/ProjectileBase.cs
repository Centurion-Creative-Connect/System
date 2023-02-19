using System;
using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    public abstract class ProjectileBase : DamageData
    {
        [NonSerialized]
        public bool IsCurrentlyActive;

        [PublicAPI]
        public virtual void Shoot(Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, int playerId)
            => Shoot(pos, rot, velocity, torque, drag, damageType, playerId, float.NaN, null);

        [PublicAPI]
        public abstract void Shoot(Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, int playerId,
            float trailTime, Gradient trailGradient);
    }
}