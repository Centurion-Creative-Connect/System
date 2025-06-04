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
        public virtual void Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime originTime, int playerId)
            => Shoot(eventId, pos, rot, velocity, torque, drag, damageType, originTime, playerId, float.NaN, null, 5F);

        [PublicAPI]
        public virtual void Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime originTime, int playerId,
            float trailTime, Gradient trailGradient)
            => Shoot(eventId, pos, rot, velocity, torque, drag, damageType, originTime, playerId, float.NaN, null, 5F);

        [PublicAPI]
        public abstract void Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime originTime, int playerId,
            float trailTime, Gradient trailGradient,
            float lifeTimeInSeconds);

        public void SetDamageSetting(
            DetectionType type,
            bool respectFriendlyFireSetting = true,
            bool canDamageSelf = false,
            bool canDamageFriendly = true,
            bool canDamageEnemy = true
        )
        {
            DetectionType = type;
            RespectFriendlyFireSetting = respectFriendlyFireSetting;
            CanDamageSelf = canDamageSelf;
            CanDamageFriendly = canDamageFriendly;
            CanDamageEnemy = canDamageEnemy;
        }

        public void ResetDamageSetting()
        {
            SetDamageSetting(DetectionType.All);
        }
    }
}