using UdonSharp;

namespace CenturionCC.System.Gun
{
    public abstract class ProjectilePool : UdonSharpBehaviour
    {
        public abstract ProjectileBase GetProjectile();
    }
}