using CenturionCC.System.Utils;

namespace CenturionCC.System.Player
{
    public abstract class DamageDataSyncerManagerCallback : PlayerManagerCallbackBase
    {
        public virtual void OnSyncerReceived(DamageDataSyncer syncer)
        {
        }

        /// <summary>
        /// Pre-condition check for sending a hit
        /// </summary>
        /// <param name="damageData">DamageData responsible for this hit call</param>
        /// <param name="attacker">Attacker responsible for this hit</param>
        /// <param name="victim">Victim responsible for this hit</param>>
        /// <returns>`true` if cancel sending, `false` otherwise.</returns>
        public virtual bool OnPreHitSend(DamageData damageData, PlayerBase attacker, PlayerBase victim)
        {
            return false;
        }
    }
}