using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunBulletDataStore : ProjectileDataProvider
    {
        [FormerlySerializedAs("bulletSpeed")] [SerializeField] [Tooltip("In m/s")]
        private float baseSpeed = 85F;

        [FormerlySerializedAs("bulletDrag")] [SerializeField]
        private float baseDrag = 1F;

        [FormerlySerializedAs("bulletHopUpStrength")] [SerializeField]
        private float baseHopUpStrength;

        [FormerlySerializedAs("bulletTrailTime")] [SerializeField]
        private float trailTime = 0.1F;

        [FormerlySerializedAs("bulletTrailGradient")] [SerializeField]
        private Gradient trailGradient = new Gradient();

        [SerializeField] [Tooltip("In seconds, defaults to 5s")]
        private float lifeTime = 5F;

        [SerializeField] private int projectileCount = 1;
        [SerializeField] private GunRecoilPatternDataStore recoilPattern;

        [Header("Damage Data Settings")]
        [SerializeField] private float baseDamage = 100F;
        [SerializeField] private DetectionType detectionType = DetectionType.All;
        [SerializeField] private bool respectFriendlyFireSetting = true;
        [SerializeField] private bool canDamageSelf = false;
        [SerializeField] private bool canDamageFriendly = true;
        [SerializeField] private bool canDamageEnemy = true;

        [Header("Damage Multipliers")]
        [SerializeField] private float bodyDamageMultiplier = 1;
        [SerializeField] private float headDamageMultiplier = 1;
        [SerializeField] private float armDamageMultiplier = 1;
        [SerializeField] private float legDamageMultiplier = 1;

        public GunRecoilPatternDataStore RecoilPattern => recoilPattern;
        public override int ProjectileCount => projectileCount;
        public override DetectionType DetectionType => detectionType;
        public override bool RespectFriendlyFireSetting => respectFriendlyFireSetting;
        public override bool CanDamageFriendly => canDamageFriendly;
        public override bool CanDamageEnemy => canDamageEnemy;
        public override bool CanDamageSelf => canDamageSelf;

        public override void Get(int i,
                                 out Vector3 positionOffset, out Vector3 velocity,
                                 out Quaternion rotationOffset, out Vector3 torque, out float drag,
                                 out float damageAmount,
                                 out float trailDuration, out Gradient trailColor, out float lifeTimeInSeconds)
        {
            var speedOffset = 0F;
            var recoilOffset = Vector3.zero;
            positionOffset = Vector3.zero;

            if (recoilPattern) recoilPattern.Get(i, out speedOffset, out recoilOffset, out positionOffset);

            damageAmount = baseDamage;
            drag = baseDrag;
            rotationOffset = Quaternion.Euler(recoilOffset);
            velocity = Vector3.forward * (baseSpeed + speedOffset);
            torque = new Vector3(baseHopUpStrength, 0, 0) + recoilOffset;
            trailDuration = trailTime;
            trailColor = trailGradient;
            lifeTimeInSeconds = lifeTime;
        }

        public override void GetRecoil(out Vector3 position, out Quaternion rotation)
        {
            if (!recoilPattern)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            recoilPattern.GetRecoil(out position, out rotation);
        }
        public override float GetDamageMultiplier(BodyParts parts)
        {
            switch (parts)
            {
                case BodyParts.Body: return bodyDamageMultiplier;
                case BodyParts.Head: return headDamageMultiplier;
                case BodyParts.LeftArm:
                case BodyParts.RightArm: return armDamageMultiplier;
                case BodyParts.LeftLeg:
                case BodyParts.RightLeg: return legDamageMultiplier;
                default: return 1;
            }
        }
    }
}
