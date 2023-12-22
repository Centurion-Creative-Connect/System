namespace CenturionCC.System.Player
{
    public abstract class DamageDataSyncerManagerCallback : PlayerManagerCallbackBase
    {
        public virtual void OnSyncerReceived(DamageDataSyncer syncer)
        {
        }
    }
}