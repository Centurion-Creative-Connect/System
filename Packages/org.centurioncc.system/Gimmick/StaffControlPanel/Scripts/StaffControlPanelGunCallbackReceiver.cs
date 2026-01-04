using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.StaffControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StaffControlPanelGunCallbackReceiver : GunManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

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

        public override void OnShoot(GunBase instance, ProjectileBase projectile)
        {
            ui.IncrementShots();
        }
    }
}
