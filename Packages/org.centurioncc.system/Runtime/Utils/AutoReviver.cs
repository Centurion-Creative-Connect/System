using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AutoReviver : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField]
        private bool doAutoRevive = true;

        [SerializeField]
        private float autoReviveDelay = 6F;

        public bool DoAutoRevive { get; set; }
        public float AutoReviveDelay { get; set; }

        private void Start()
        {
            DoAutoRevive = doAutoRevive;
            AutoReviveDelay = autoReviveDelay;

            playerManager.Subscribe(this);
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!player.IsValid() || !player.isLocal) return;

            var local = playerManager.GetLocalPlayer();
            if (!local || !DoAutoRevive || !local.IsDead) return;

            local.Revive();
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!DoAutoRevive || !victim.IsLocal) return;

            Debug.Log($"[AutoReviver] Reviving in {AutoReviveDelay} seconds");
            victim.SendCustomEventDelayedSeconds(nameof(victim.Revive), AutoReviveDelay);
        }
    }
}