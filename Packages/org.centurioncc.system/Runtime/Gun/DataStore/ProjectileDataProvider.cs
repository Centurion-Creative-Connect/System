using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    public abstract class ProjectileDataProvider : UdonSharpBehaviour
    {
        public abstract int ProjectileCount { get; }

        public abstract void Get(int i,
            out Vector3 positionOffset, out Vector3 velocity,
            out Quaternion rotationOffset, out Vector3 torque,
            out float drag,
            out float trailDuration,
            out Gradient trailColor);
    }
}