using JetBrains.Annotations;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    public static class VRCPlayerApiExtensions
    {
        [Pure]
        public static int SafeGetPlayerId([CanBeNull] this VRCPlayerApi api)
        {
            // Utilities.IsValid will check for null
            // ReSharper disable once PossibleNullReferenceException
            return Utilities.IsValid(api) ? api.playerId : -1;
        }

        [Pure]
        public static string SafeGetDisplayName([CanBeNull] this VRCPlayerApi api, string defaultName = "???")
        {
            // Utilities.IsValid will check for null
            // ReSharper disable once PossibleNullReferenceException
            return Utilities.IsValid(api) ? api.displayName : defaultName;
        }
    }
}