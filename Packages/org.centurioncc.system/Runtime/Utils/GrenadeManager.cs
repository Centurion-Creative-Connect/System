using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GrenadeManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private Grenade[] _localGrenades = new Grenade[0];

        // NOTE: Checked with HasLocalPlayer
        // ReSharper disable once PossibleNullReferenceException
        public bool CanExplode => playerManager.HasLocalPlayer() && !playerManager.GetLocalPlayer().IsDead;

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