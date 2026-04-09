using CenturionCC.System.Gun;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.MatchLogger
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MatchLoggerGunLogProducer : GunManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [NewbieInject]
        private MatchLogger matchLogger;

        private void OnEnable()
        {
            gunManager.Subscribe(this);
        }

        private void OnDisable()
        {
            gunManager.Unsubscribe(this);
        }

        public override void OnShoot(GunBase instance, ProjectileBase projectile)
        {
            matchLogger.AddMatchEventLog("shoot", projectile.ToDictionary());
        }
    }
}
