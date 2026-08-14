using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using System.Threading;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    public abstract class ProjectileDataProvider : UdonSharpBehaviour
    {
        public abstract int ProjectileCount { get; }
        public abstract DetectionType DetectionType { get; }
        public abstract bool RespectFriendlyFireSetting { get; }
        public abstract bool CanDamageFriendly { get; }
        public abstract bool CanDamageEnemy { get; }
        public abstract bool CanDamageSelf { get; }

        public virtual void Get(
            int i,
            out Vector3 positionOffset, out Vector3 velocity,
            out Quaternion rotationOffset, out Vector3 torque, out float drag,
            out float damageAmount,
            out float trailDuration, out Gradient trailColor, out float lifeTimeInSeconds
        )
        {
            positionOffset = Vector3.zero;
            velocity = Vector3.zero;
            rotationOffset = Quaternion.identity;
            torque = Vector3.zero;
            drag = 0;
            damageAmount = 0;
            trailDuration = 0;
            trailColor = new Gradient();
            lifeTimeInSeconds = 0;
        }

        public virtual void GetRecoil(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        public virtual float GetDamageMultiplier(BodyParts parts) => 1;
    }
}
