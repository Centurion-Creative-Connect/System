using System;
using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UdonSharp;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;

namespace CenturionCC.System.Player
{
    public class CenturionDamageResolver : UdonSharpBehaviour
    {
        private readonly DataDictionary _processingEvents = new DataDictionary();
        private readonly DataList _resolvedEventIds = new DataList();

        [PublicAPI]
        public bool IsEventResolved(Guid eventId)
        {
            return _resolvedEventIds.Contains(eventId.ToString("D"));
        }

        [PublicAPI]
        public bool IsEventProcessing(Guid eventId)
        {
            return _processingEvents.ContainsKey(eventId.ToString("D"));
        }

        [PublicAPI]
        public void BroadcastEvent(DamageInfo info)
        {
        }

        [NetworkCallable(100)]
        public void Internal_BroadcastEvent(byte[] damageInfoBytes)
        {
        }

        [NetworkCallable(100)]
        public void Internal_BroadcastResolved(bool result, byte[] eventGuidBytes)
        {
            var eventGuid = new Guid(eventGuidBytes);
            if (IsEventProcessing(eventGuid))
            {
                _processingEvents.Remove(eventGuid.ToString("D"));
                return;
            }

            _resolvedEventIds.Add(eventGuid.ToString("D"));
        }
    }
}