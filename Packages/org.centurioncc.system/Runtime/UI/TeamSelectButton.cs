using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TeamSelectButton : PlayerManagerCallbackBase
    {
        [SerializeField][HideInInspector][NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] private int targetTeamId;
        [SerializeField] private GameObject changeToTargetButton;
        [SerializeField] private GameObject changeToDefaultButton;

        private void Start()
        {
            playerManager.SubscribeCallback(this);
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            var player = playerManager.GetLocalPlayer();
            bool isInTargetTeam = player != null && player.TeamId == targetTeamId || player == null && targetTeamId == 0;

            if (changeToTargetButton != null)
                changeToTargetButton.SetActive(!isInTargetTeam);

            if (changeToDefaultButton != null)
                changeToDefaultButton.SetActive(isInTargetTeam);
        }

        public override void Interact()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null) return;

            localPlayer.SetTeam(localPlayer.TeamId != targetTeamId ? targetTeamId : 0);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!player.IsLocal) return;

            UpdateButtonState();
        }
    }
}
