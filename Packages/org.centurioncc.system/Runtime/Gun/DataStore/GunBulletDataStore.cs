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

        public GunRecoilPatternDataStore RecoilPattern => recoilPattern;
        public override int ProjectileCount => projectileCount;

        public override void Get(int i,
            out Vector3 positionOffset, out Vector3 velocity,
            out Quaternion rotationOffset, out Vector3 torque,
            out float drag,
            out float trailDuration, out Gradient trailColor, out float lifeTimeInSeconds)
        {
            var speedOffset = 0F;
            var recoilOffset = Vector3.zero;
            positionOffset = Vector3.zero;

            if (recoilPattern) recoilPattern.Get(i, out speedOffset, out recoilOffset, out positionOffset);

            drag = baseDrag;
            rotationOffset = Quaternion.Euler(recoilOffset);
            velocity = Vector3.forward * (baseSpeed + speedOffset);
            torque = new Vector3(baseHopUpStrength, 0, 0) + recoilOffset;
            trailDuration = trailTime;
            trailColor = trailGradient;
            lifeTimeInSeconds = lifeTime;
        }
    }
}