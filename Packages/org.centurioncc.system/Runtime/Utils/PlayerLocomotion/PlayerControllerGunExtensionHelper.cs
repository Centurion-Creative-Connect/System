using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Utils.PlayerLocomotion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerControllerGunExtensionHelper : GunManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerControllerGunExtension extension;
        [SerializeField] [NewbieInject]
        private PlayerController playerController;
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        private void Start()
        {
            gunManager.Subscribe(this);
        }

        public override void OnPickedUpLocally(GunBase instance)
        {
            if (!extension.useGunIntegration) return;
            extension.UpdateLowestGunProperty();
            playerController.UpdateLocalVrcPlayer();
        }

        public override void OnDropLocally(GunBase instance)
        {
            if (!extension.useGunIntegration) return;
            extension.UpdateLowestGunProperty();
            playerController.UpdateLocalVrcPlayer();
        }

        public override void OnShoot(GunBase instance, ProjectileBase projectile)
        {
            if (!extension.useGunIntegration) return;
            extension.UpdateLastShotTime();
        }
    }
}
