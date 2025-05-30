using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CenturionDamageResolver : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private CenturionPlayerManager playerManager;

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
        public void BroadcastDamageInfo(DamageInfo info)
        {
            Internal_BroadcastDamageInfo(info.ToBytes());
        }

        [NetworkCallable(100)]
        public void Internal_BroadcastDamageInfo(byte[] damageInfoBytes)
        {
            var damageInfo = DamageInfo.FromBytes(damageInfoBytes);
            if (IsEventResolved(damageInfo.EventId()))
            {
                return;
            }

            _processingEvents.Add(damageInfo.EventId().ToString("D"), damageInfo);
            playerManager.Internal_ApplyLocalDamageInfo(damageInfo);
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