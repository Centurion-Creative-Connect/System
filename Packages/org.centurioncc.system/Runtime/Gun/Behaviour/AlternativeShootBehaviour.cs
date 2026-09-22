using CenturionCC.System.Gun.DataStore;
using System;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gun.Behaviour
{
    public class AlternativeShootBehaviour : GunBehaviourBase
    {
        [SerializeField]
        private Transform shootingTransform;

        [SerializeField]
        private ProjectileDataProvider projectileDataProvider;

        [SerializeField]
        private float secondsPerRound;

        [SerializeField]
        private bool doAlternativeShootOnAction = true;

        [SerializeField]
        private bool consumeMagazineBullet = false;
        private GunBase _instance;

        private DateTime _lastShotTime;
        private int _shootCount;

        public override void Setup(GunBase instance)
        {
            _instance = instance;
        }

        public override void Dispose(GunBase instance)
        {
            _shootCount = 0;
        }

        public override void OnAction(GunBase instance)
        {
            if (doAlternativeShootOnAction)
            {
                _TryAlternativeShoot();
            }
        }

        public void _TryAlternativeShoot()
        {
            var now = Networking.GetNetworkDateTime();
            if (now.Subtract(_lastShotTime).TotalSeconds < secondsPerRound)
            {
                return;
            }

            _instance.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(_instance.SendGunBehaviourEvent), "_AlternativeShoot");
        }

        public void _AlternativeShoot()
        {
            // NOTE(derpy): DamageData Guids should be synced across clients. but here we're generating new guid.
            _instance._ShootLocally(projectileDataProvider, _instance.ProjectilePool, shootingTransform.position, shootingTransform.rotation, _shootCount++, Guid.NewGuid(), consumeMagazineBullet);
            _lastShotTime = Networking.GetNetworkDateTime();
        }
    }
}
