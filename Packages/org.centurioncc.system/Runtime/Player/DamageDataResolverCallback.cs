using UdonSharp;

namespace CenturionCC.System.Player
{
    public abstract class DamageDataResolverCallback : UdonSharpBehaviour
    {
        public virtual void OnResolved(ResolverDataSyncer syncer)
        {
        }

        public virtual void OnResolveAborted(ResolverDataSyncer syncer, string reason)
        {
        }
    }
}