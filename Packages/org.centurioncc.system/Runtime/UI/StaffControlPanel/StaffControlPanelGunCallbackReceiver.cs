using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.StaffControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StaffControlPanelGunCallbackReceiver : GunManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private StaffControlPanelUI ui;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        public override void OnGunsReset(GunManagerResetType type)
        {
            ui.ActivateGunResetButton();
        }

        public override void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
            ui.IncrementShots();
        }
    }
}