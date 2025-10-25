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

        public virtual void OnGunsReset(GunManagerResetType type)
        {
            OnGunsReset();
        }

        public virtual void OnOccupyChanged(GunBase instance)
        {
        }

        public virtual void OnVariantChanged(GunBase instance)
        {
        }

        public virtual void OnPickedUpLocally(GunBase instance)
        {
        }

        public virtual void OnDropLocally(GunBase instance)
        {
        }

        public virtual void OnShoot(GunBase instance, ProjectileBase projectile)
        {
        }

        public virtual void OnEmptyShoot(GunBase instance)
        {
        }

        public virtual void OnShootFailed(GunBase instance, int reasonId)
        {
        }

        public virtual void OnShootCancelled(GunBase instance, int reasonId)
        {
        }

        public virtual void OnFireModeChanged(GunBase instance)
        {
        }
    }
}
