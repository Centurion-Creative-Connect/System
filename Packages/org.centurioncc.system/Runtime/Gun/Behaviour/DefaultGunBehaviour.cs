using UdonSharp;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DefaultGunBehaviour : GunBehaviourBase
    {
        public override bool RequireCustomHandle => false;

        public override void OnGunPickup(GunBase instance)
        {
            if (!instance.HasBulletInChamber)
                instance._LoadBullet();
            instance.HasCocked = true;
        }

        public override void OnGunDrop(GunBase instance)
        {
        }

        public override void OnGunUpdate(GunBase instance)
        {
            if (instance.Trigger == TriggerState.Firing)
            {
                var shotResult = instance._TryToShoot();
                var hasSucceeded = shotResult == ShotResult.Succeeded || shotResult == ShotResult.SucceededContinuously;
                if (hasSucceeded)
                {
                    if (!instance.HasBulletInChamber)
                        instance._LoadBullet();
                    instance.HasCocked = true;
                }
            }
        }

        public override void Setup(GunBase instance)
        {
            instance.State = GunState.Idle;
            if (!instance.HasBulletInChamber)
                instance._LoadBullet();
            instance.HasCocked = true;
        }

        public override void Dispose(GunBase instance)
        {
        }
    }
}