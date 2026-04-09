using CenturionCC.System.Gun.DataStore;
using JetBrains.Annotations;
namespace CenturionCC.System.Gun
{
    public static class GunManagerBaseExtensions
    {
        [PublicAPI] [CanBeNull]
        public static GunVariantDataStore GetVariantDataById(this GunManagerBase gunManager, byte uniqueId, bool useFallback = true)
        {
            foreach (var dataStore in gunManager.GetVariantDataInstances())
            {
                if (dataStore == null || dataStore.UniqueId != uniqueId) continue;
                return dataStore;
            }

            return useFallback ? gunManager.FallbackVariantData : null;
        }

        [PublicAPI] [CanBeNull]
        public static GunVariantDataStore GetVariantDataByWeaponName(this GunManagerBase gunManager, string weaponName, bool useFallback = true)
        {
            foreach (var dataStore in gunManager.GetVariantDataInstances())
            {
                if (dataStore == null || dataStore.WeaponName != weaponName) continue;
                return dataStore;
            }

            return useFallback ? gunManager.FallbackVariantData : null;
        }
    }
}
