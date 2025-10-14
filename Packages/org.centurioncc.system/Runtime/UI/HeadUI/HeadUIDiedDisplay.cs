using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.HeadUI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HeadUIDiedDisplay : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField]
        private float diedDelay = 6;

        [SerializeField]
        private GameObject[] enableOnDead;

        private void Start()
        {
            playerManager.Subscribe(this);
            SetObjectsOff();
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!victim.IsLocal)
                return;

            SendCustomEventDelayedSeconds(nameof(SetObjectsOn), diedDelay);
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            if (!player.IsLocal)
                return;

            SetObjectsOff();
        }

        public void SetObjectsOn()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null || !localPlayer.IsDead)
                return;

            SetObjectsActive(true);
        }

        public void SetObjectsOff()
        {
            SetObjectsActive(false);
        }

        private void SetObjectsActive(bool active)
        {
            foreach (var go in enableOnDead)
            {
                go.SetActive(active);
            }
        }
    }
}