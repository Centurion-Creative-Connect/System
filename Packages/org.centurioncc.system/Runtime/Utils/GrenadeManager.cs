using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GrenadeManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [Header("Optimization Settings")]
        [SerializeField] [Tooltip("Distance until explosion bullets reduction will begin. in meters")]
        private float bulletReductionNear = 10;

        [SerializeField] [Tooltip("Distance until explosion bullets reduction will fully disable bullets. in meters")]
        private float bulletsReductionFar = 15;

        private Grenade[] _localGrenades = new Grenade[0];

        // NOTE: Checked with HasLocalPlayer
        // ReSharper disable once PossibleNullReferenceException
        public bool CanExplode => playerManager.HasLocalPlayer() && !playerManager.GetLocalPlayer().IsDead;

        public float BulletReductionNear => bulletReductionNear;
        public float BulletReductionFar => bulletsReductionFar;

        public void AddLocalGrenade(Grenade grenade)
        {
            _localGrenades = _localGrenades.AddAsSet(grenade);
        }

        public void RemoveLocalGrenade(Grenade grenade)
        {
            _localGrenades = _localGrenades.RemoveItem(grenade);
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer, KillType type)
        {
            if (!hitPlayer.IsLocal) return;

            foreach (var grenade in _localGrenades)
            {
                grenade.ResetGrenade();
            }
        }
    }
}