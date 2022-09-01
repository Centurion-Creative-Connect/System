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
            if (animator) animator.SetBool(GunHelper.HasBulletParameter(), true);
        }

        public override void OnGunDrop(GunBase instance)
        {
            var animator = instance.TargetAnimator;
            if (animator) animator.SetBool(GunHelper.HasBulletParameter(), true);
        }

        public override void OnGunUpdate(GunBase instance)
        {
            if (instance.Trigger == TriggerState.Firing)
                instance.TryToShoot();
        }

        public override void Setup(GunBase instance)
        {
            instance.State = GunState.ReadyToShoot;
        }

        public override void Dispose(GunBase instance)
        {
            
        }
    }
}