using System;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace CenturionCC.System.Match
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunLogProducer : GunManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [NewbieInject]
        private GameMatchHandler matchHandler;

        private void OnEnable()
        {
            gunManager.SubscribeCallback(this);
        }

        private void OnDisable()
        {
            gunManager.UnsubscribeCallback(this);
        }

        public override void OnShoot(GunBase instance, ProjectileBase projectile)
        {
            matchHandler.AddMatchEventLog("shoot", projectile.ToDictionary());
        }
    }
}
