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
        public float drag;
        [UdonSynced]
        public float hopUpStrength;
        [UdonSynced]
        public float trailTime;
        public Gradient trailGradient;
        public GunRecoilPatternDataStore recoilPattern;

        public override int ProjectileCount => projectileCount;

        public override void Get(int i,
            out Vector3 posOff, out Vector3 vel,
            out Quaternion rotOff, out Vector3 torque,
            out float d,
            out float trailDur, out Gradient trailColor)
        {
            recoilPattern.Get(i, out var spdOff, out var recOff, out posOff);
            d = drag;
            rotOff = Quaternion.Euler(recOff);
            vel = Vector3.forward * (speed + spdOff);
            torque = new Vector3(hopUpStrength, 0, 0) + recOff;
            trailDur = trailTime;
            trailColor = trailGradient;
        }

        public void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}