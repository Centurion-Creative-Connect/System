using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.Rule
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoShootingWhileDeadRule : GunManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        public override bool CanShoot()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            return localPlayer == null || !localPlayer.IsDead;
        }
    }
}