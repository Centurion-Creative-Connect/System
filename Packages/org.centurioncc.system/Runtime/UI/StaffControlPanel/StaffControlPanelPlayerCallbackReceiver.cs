using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.StaffControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StaffControlPanelPlayerCallbackReceiver : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private StaffControlPanelUI ui;

        private void Start()
        {
            playerManager.Subscribe(this);
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            switch (type)
            {
                case KillType.Default:
                    ui.IncrementKills();
                    break;
                case KillType.FriendlyFire:
                    ui.IncrementFriendlyFires();
                    break;
                case KillType.ReverseFriendlyFire:
                    ui.IncrementReverseFriendlyFires();
                    break;
            }
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            ui.UpdateUI();
        }

        public override void OnPlayerTagChanged(TagType type, bool isOn)
        {
            ui.UpdateUI();
        }

        public override void OnFriendlyFireModeChanged(FriendlyFireMode previousMode)
        {
            ui.UpdateUI();
        }

        public override void OnDebugModeChanged(bool isOn)
        {
            ui.UpdateUI();
        }
    }
}