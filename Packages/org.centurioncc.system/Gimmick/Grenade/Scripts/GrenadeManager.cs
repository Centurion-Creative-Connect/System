using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gimmick.Grenade
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GrenadeManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [Header("Optimization Settings")]
        [SerializeField] [Tooltip("Distance until explosion bullets reduction will begin. in meters")]
        private float bulletReductionNear = 10;

        [SerializeField] [Tooltip("Distance until explosion bullets reduction will fully disable bullets. in meters")]
        private float bulletsReductionFar = 15;

        private Grenade[] _localGrenades = new Grenade[0];

        // NOTE: Checked with HasLocalPlayer
        // ReSharper disable once PossibleNullReferenceException
        public bool CanExplode
        {
            get
            {
                var localPlayer = playerManager.GetLocalPlayer();
                return localPlayer != null && !localPlayer.IsDead;
            }
        }

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

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!victim.IsLocal) return;

            foreach (var grenade in _localGrenades)
            {
                grenade.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(grenade.ResetGrenade));
            }
        }
    }
}
