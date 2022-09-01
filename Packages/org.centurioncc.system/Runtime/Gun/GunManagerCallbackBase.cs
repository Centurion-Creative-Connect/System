using UdonSharp;

namespace CenturionCC.System.Gun
{
    public abstract class GunManagerCallbackBase : UdonSharpBehaviour
    {
        public virtual bool CanShoot()
        {
            return true;
        }
        
        public virtual void OnGunsReset()
        {
        }

        public virtual void OnOccupyChanged(ManagedGun instance)
        {
        }

        public virtual void OnVariantChanged(ManagedGun instance)
        {
        }

        public virtual void OnPickedUpLocally(ManagedGun instance)
        {
        }

        public virtual void OnDropLocally(ManagedGun instance)
        {
        }

        public virtual void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
        }

        public virtual void OnEmptyShoot(ManagedGun instance)
        {
        }

        public virtual void OnShootFailed(ManagedGun instance, int reasonId)
        {
        }

        public virtual void OnShootCancelled(ManagedGun instance, int reasonId)
        {
        }

        public virtual void OnFireModeChanged(ManagedGun instance)
        {
        }
    }
}