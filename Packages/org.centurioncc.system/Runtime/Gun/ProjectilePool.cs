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
        public abstract ProjectileBase Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, float damageAmount,
            DateTime time, int playerId,
            float trailTime, Gradient trailGradient,
            float lifeTimeInSeconds);

        [PublicAPI]
        public virtual ProjectileBase Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, float damageAmount,
            DateTime time, int playerId,
            float trailTime, Gradient trailGradient)
            => Shoot(eventId,
                pos, rot,
                velocity, torque, drag,
                damageType, damageAmount,
                time, playerId,
                trailTime, trailGradient, 5F);

        [PublicAPI]
        public virtual ProjectileBase Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, float damageAmount,
            DateTime time, int playerId)
            => Shoot(eventId,
                pos, rot,
                velocity, torque, drag,
                damageType, damageAmount,
                time, playerId,
                float.NaN, null, 5F);
    }
}