using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    public abstract class ProjectilePool : UdonSharpBehaviour
    {
        [PublicAPI]
        public abstract bool HasInitialized { get; }

        [PublicAPI]
        public abstract ProjectileBase GetProjectile();

        [PublicAPI]
        public abstract ProjectileBase Shoot(Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime time, int playerId,
            float trailTime, Gradient trailGradient);

        [PublicAPI]
        public virtual ProjectileBase Shoot(Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime time, int playerId)
            => Shoot(pos, rot, velocity, torque, drag, damageType, time, playerId, float.NaN, null);
    }
}