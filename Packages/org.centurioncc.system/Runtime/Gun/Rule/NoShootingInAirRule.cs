using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.Rule
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NoShootingInAirRule : GunManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        public override bool CanShoot()
        {
            return Networking.LocalPlayer.IsPlayerGrounded();
        }
    }
}