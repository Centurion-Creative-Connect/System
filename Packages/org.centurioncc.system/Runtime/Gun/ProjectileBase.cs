using CenturionCC.System.Gun.DataStore;
using CenturionCC.System.Player;
using System;
using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    public abstract class ProjectileBase : DamageData
    {
        [CanBeNull]
        private ProjectileDataProvider _dataProvider;

        public override DetectionType DetectionType => _dataProvider ? _dataProvider.DetectionType : DetectionType.All;
        public override bool RespectFriendlyFireSetting => !_dataProvider || _dataProvider.RespectFriendlyFireSetting;
        public override bool CanDamageEnemy => !_dataProvider || _dataProvider.CanDamageEnemy;
        public override bool CanDamageFriendly => !_dataProvider || _dataProvider.CanDamageFriendly;
        public override bool CanDamageSelf => _dataProvider && _dataProvider.CanDamageSelf;

        public override float GetDamageMultiplier(BodyParts bodyParts)
        {
            return _dataProvider ? _dataProvider.GetDamageMultiplier(bodyParts) : 1;
        }

        [PublicAPI]
        public virtual void Shoot(Guid eventId,
                                  Vector3 pos, Quaternion rot,
                                  Vector3 velocity, Vector3 torque, float drag,
                                  string damageType, float damageAmount,
                                  DateTime originTime, int playerId)
            => Shoot(eventId, pos, rot, velocity, torque, drag, damageType, damageAmount, originTime, playerId,
                float.NaN, null, 5F);

        [PublicAPI]
        public virtual void Shoot(Guid eventId,
                                  Vector3 pos, Quaternion rot,
                                  Vector3 velocity, Vector3 torque, float drag,
                                  string damageType, float damageAmount,
                                  DateTime originTime, int playerId,
                                  float trailTime, Gradient trailGradient)
            => Shoot(eventId, pos, rot, velocity, torque, drag, damageType, damageAmount, originTime, playerId,
                float.NaN, null, 5F);

        [PublicAPI]
        public abstract void Shoot(Guid eventId,
                                   Vector3 pos, Quaternion rot,
                                   Vector3 velocity, Vector3 torque, float drag,
                                   string damageType, float damageAmount,
                                   DateTime originTime, int playerId,
                                   float trailTime, Gradient trailGradient,
                                   float lifeTimeInSeconds);

        [PublicAPI]
        public void SetProjectileDataProvider([CanBeNull] ProjectileDataProvider provider)
        {
            _dataProvider = provider;
        }
    }
}
