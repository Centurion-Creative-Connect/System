using UdonSharp;

namespace CenturionCC.System.Player
{
    public abstract class DamageDataResolverCallback : UdonSharpBehaviour
    {
        public virtual void OnResolved(ResolverDataSyncer syncer)
        {
        }
    }
}