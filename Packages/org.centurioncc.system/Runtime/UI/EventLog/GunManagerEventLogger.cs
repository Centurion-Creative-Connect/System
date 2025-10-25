using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.EventLog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunManagerEventLogger : GunManagerCallbackBase
    {
        private const string Prefix = "G";

        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [NewbieInject]
        private SystemEventLogger systemEventLogger;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        public override void OnGunsReset(GunManagerResetType type)
        {
            systemEventLogger.AppendLog(Prefix, "Reset", $"---------- (Reset {type.ToEnumName()}) ----------");
        }
    }
}
