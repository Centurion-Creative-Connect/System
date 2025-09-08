using UdonSharp;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DefaultGunBehaviour : GunBehaviourBase
    {
        public override bool RequireCustomHandle => false;

        public override void OnGunPickup(GunBase instance)
        {
        }

        public override void OnGunDrop(GunBase instance)
        {
        }

        public override void OnGunUpdate(GunBase instance)
        {
            if (instance.Trigger == TriggerState.Firing)
            {
                if (!instance.HasCocked)
                    instance.HasCocked = true;

                var shotResult = instance.TryToShoot();
                var hasShot = shotResult == ShotResult.Succeeded || shotResult == ShotResult.SucceededContinuously ||
                              shotResult == ShotResult.Failed;
                if (hasShot && !instance.HasBulletInChamber)
                    instance.LoadBullet();
            }
        }

        public override void Setup(GunBase instance)
        {
            instance.State = GunState.Idle;
        }

        public override void Dispose(GunBase instance)
        {
        }
    }
}