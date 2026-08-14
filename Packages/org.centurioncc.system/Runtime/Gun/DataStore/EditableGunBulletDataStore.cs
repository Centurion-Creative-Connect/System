using CenturionCC.System.Player;
using CenturionCC.System.Utils;
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

        [UdonSynced]
        public float headDamageMultiplier = 1;

        [UdonSynced]
        public float bodyDamageMultiplier = 1;

        [UdonSynced]
        public float armDamageMultiplier = 1;

        [UdonSynced]
        public float legDamageMultiplier = 1;

        [UdonSynced]
        public DetectionType detectionType = DetectionType.All;

        [UdonSynced]
        public bool respectFriendlyFireSetting = true;

        [UdonSynced]
        public bool canDamageSelf = false;

        [UdonSynced]
        public bool canDamageFriendly = true;

        [UdonSynced]
        public bool canDamageEnemy = true;

        public Gradient trailGradient;
        public GunRecoilPatternDataStore recoilPattern;

        public override int ProjectileCount => projectileCount;
        public override DetectionType DetectionType => detectionType;
        public override bool RespectFriendlyFireSetting => respectFriendlyFireSetting;
        public override bool CanDamageFriendly => canDamageFriendly;
        public override bool CanDamageEnemy => canDamageEnemy;
        public override bool CanDamageSelf => canDamageSelf;

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
                case BodyParts.Head: return headDamageMultiplier;
                case BodyParts.Body: return bodyDamageMultiplier;
                case BodyParts.LeftArm:
                case BodyParts.RightArm: return armDamageMultiplier;
                case BodyParts.LeftLeg:
                case BodyParts.RightLeg: return legDamageMultiplier;
                default: return 1;
            }
        }

        public void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
