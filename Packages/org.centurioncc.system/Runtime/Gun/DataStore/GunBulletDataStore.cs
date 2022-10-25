using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunBulletDataStore : ProjectileDataProvider
    {
        [FormerlySerializedAs("bulletSpeed")]
        [SerializeField] [Tooltip("In m/s")]
        private float baseSpeed = 85F;
        [FormerlySerializedAs("bulletDrag")]
        [SerializeField]
        private float baseDrag = 1F;
        [FormerlySerializedAs("bulletHopUpStrength")]
        [SerializeField]
        private float baseHopUpStrength;
        [FormerlySerializedAs("bulletTrailTime")]
        [SerializeField]
        private float trailTime = 0.1F;
        [FormerlySerializedAs("bulletTrailGradient")]
        [SerializeField]
        private Gradient trailGradient = new Gradient();
        [SerializeField]
        private int projectileCount = 1;
        [SerializeField]
        private GunRecoilPatternDataStore recoilPattern;

        public override int ProjectileCount => projectileCount;

        public override void Get(int i,
            out Vector3 positionOffset, out Vector3 velocity,
            out Quaternion rotationOffset, out Vector3 torque,
            out float drag,
            out float trailDuration, out Gradient trailColor)
        {
            recoilPattern.Get(i, out var speedOffset, out var recoilOffset, out positionOffset);
            drag = baseDrag;
            rotationOffset = Quaternion.Euler(recoilOffset);
            velocity = Vector3.forward * (baseSpeed + speedOffset);
            torque = new Vector3(baseHopUpStrength, 0, 0) + recoilOffset;
            trailDuration = trailTime;
            trailColor = trailGradient;
        }
    }
}