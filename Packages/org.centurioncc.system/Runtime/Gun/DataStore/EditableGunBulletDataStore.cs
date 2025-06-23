using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EditableGunBulletDataStore : ProjectileDataProvider
    {
        [UdonSynced]
        public int projectileCount;

        [UdonSynced]
        public float speed;

        [UdonSynced]
        public float damage;

        [UdonSynced]
        public float drag;

        [UdonSynced]
        public float hopUpStrength;

        [UdonSynced]
        public float trailTime;

        [UdonSynced]
        public float lifeTimeInSeconds;

        public Gradient trailGradient;
        public GunRecoilPatternDataStore recoilPattern;

        public override int ProjectileCount => projectileCount;

        public override void Get(int i,
            out Vector3 posOff, out Vector3 vel,
            out Quaternion rotOff, out Vector3 torque,
            out float dr, out float dm,
            out float trailDur, out Gradient trailColor,
            out float lifeTime)
        {
            recoilPattern.Get(i, out var spdOff, out var recOff, out posOff);
            dr = drag;
            dm = damage;
            rotOff = Quaternion.Euler(recOff);
            vel = Vector3.forward * (speed + spdOff);
            torque = new Vector3(hopUpStrength, 0, 0) + recOff;
            trailDur = trailTime;
            trailColor = trailGradient;
            lifeTime = lifeTimeInSeconds;
        }

        public void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}