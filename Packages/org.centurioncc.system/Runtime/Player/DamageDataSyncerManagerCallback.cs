namespace CenturionCC.System.Player
{
    public abstract class DamageDataSyncerManagerCallback : PlayerManagerCallbackBase
    {
        public virtual void OnSyncerPreSerialization(DamageDataSyncer syncer)
        {
        }

        public virtual void OnSyncerDeserialized(DamageDataSyncer syncer)
        {
        }
    }
}