using System.Diagnostics.Contracts;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    public static class VRCPlayerApiExtensions
    {
        [Pure]
        public static int SafeGetPlayerId(this VRCPlayerApi api)
        {
            return Utilities.IsValid(api) ? api.playerId : -1;
        }
    }
}