using UdonSharp;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DefaultGunBehaviour : GunBehaviourBase
    {
        public override bool RequireCustomHandle => false;

        public override void OnGunPickup(GunBase instance)
        {
            var animator = instance.TargetAnimator;
            if (animator) animator.SetBool(GunUtility.HasBulletParameter(), true);
        }

        public override void OnGunDrop(GunBase instance)
        {
            var animator = instance.TargetAnimator;
            if (animator) animator.SetBool(GunUtility.HasBulletParameter(), true);
        }

        public override void OnGunUpdate(GunBase instance)
        {
            if (instance.Trigger == TriggerState.Firing)
            {
                var shotResult = instance.TryToShoot();
                var hasSucceeded = shotResult == ShotResult.Succeeded || shotResult == ShotResult.SucceededContinuously;
                if (hasSucceeded)
                {
                    instance.LoadBullet();
                    instance.HasCocked = true;
                }
            }
        }

        public override void Setup(GunBase instance)
        {
            instance.State = GunState.Idle;
            instance.LoadBullet();
            instance.HasCocked = true;
        }

        public override void Dispose(GunBase instance)
        {
        }
    }
}