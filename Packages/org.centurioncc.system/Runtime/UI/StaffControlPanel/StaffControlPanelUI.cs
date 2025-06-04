using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.UI.StaffControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StaffControlPanelUI : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        [Header("DebugMode")]
        [SerializeField]
        private Toggle playerColliderToggle;

        [SerializeField]
        private Toggle gunTrailToggle;

        [SerializeField]
        private Toggle gunPickupToggle;

        [Header("PlayerTags")]
        [SerializeField]
        private Toggle teamTagToggle;

        [SerializeField]
        private Toggle staffTagToggle;

        [SerializeField]
        private Toggle creatorTagToggle;

        [Header("General Settings")]
        [SerializeField]
        private TMP_Dropdown friendlyFireDropdown;

        [Header("Logic Buttons")]
        [SerializeField]
        private Button gunResetButton;

        [SerializeField]
        private Button teamResetButton;

        [SerializeField]
        private Button teamShuffleButton;

        [Header("Team Player Count")]
        [SerializeField]
        private TMP_Text nonePlayerCountText;

        [SerializeField]
        private TMP_Text redPlayerCountText;

        [SerializeField]
        private TMP_Text yellowPlayerCountText;

        [Header("Statistics")]
        [SerializeField]
        private TMP_Text systemStatistics;

        [SerializeField]
        private TMP_Text instanceStatistics;

        private bool _inUpdateUiCall;
        private int _totalFriendlyFires;
        private int _totalKills;
        private int _totalPlayersJoined;
        private int _totalReverseFriendlyFires;
        private int _totalShots;

        private void Start()
        {
            UpdateUI();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            ++_totalPlayersJoined;
        }

        public void UpdateUI()
        {
            logger.LogVerbose("[StaffControlPanelUI] UpdateUI");
            _inUpdateUiCall = true;

            teamTagToggle.isOn = playerManager.ShowTeamTag;
            staffTagToggle.isOn = playerManager.ShowStaffTag;
            creatorTagToggle.isOn = playerManager.ShowCreatorTag;

            playerColliderToggle.isOn = playerManager.IsDebug;
            gunTrailToggle.isOn = gunManager.useDebugBulletTrail;
            gunPickupToggle.isOn = gunManager.IsDebugGunHandleVisible;

            friendlyFireDropdown.value = (int)playerManager.FriendlyFireMode;

            nonePlayerCountText.text = $"<color=grey>NON: {playerManager.GetPlayersInTeam(0).Length:00}</color>";
            redPlayerCountText.text = $"<color=red>RED: {playerManager.GetPlayersInTeam(1).Length:00}</color>";
            yellowPlayerCountText.text = $"<color=yellow>YEL: {playerManager.GetPlayersInTeam(2).Length:00}</color>";

            systemStatistics.text = $"Shots: {_totalShots:0000}\n" +
                                    $"Hits : {_totalKills:0000}\n" +
                                    $"FF   : {_totalFriendlyFires:0000}\n" +
                                    $"RFF  : {_totalReverseFriendlyFires:0000}";

            instanceStatistics.text = $"Total Players  : {_totalPlayersJoined:00}\n" +
                                      $"Current Players: {VRCPlayerApi.GetPlayerCount():00}";

            _inUpdateUiCall = false;
        }


        public void IncrementKills()
        {
            ++_totalKills;
        }

        public void IncrementShots()
        {
            ++_totalShots;
        }

        public void IncrementFriendlyFires()
        {
            ++_totalFriendlyFires;
        }

        public void IncrementReverseFriendlyFires()
        {
            ++_totalReverseFriendlyFires;
        }

        public void ActivateGunResetButton()
        {
            gunResetButton.interactable = true;
        }

        #region uGUICallbacks

        [PublicAPI("Called by uGUI Toggle")]
        public void OnTeamTagToggle()
        {
            if (_inUpdateUiCall) return;
            playerManager.SetPlayerTag(TagType.Team, teamTagToggle.isOn);
        }

        [PublicAPI("Called by uGUI Toggle")]
        public void OnStaffTagToggle()
        {
            if (_inUpdateUiCall) return;
            playerManager.SetPlayerTag(TagType.Staff, staffTagToggle.isOn);
        }

        [PublicAPI("Called by uGUI Toggle")]
        public void OnCreatorTagToggle()
        {
            if (_inUpdateUiCall) return;
            playerManager.SetPlayerTag(TagType.Creator, creatorTagToggle.isOn);
        }

        [PublicAPI("Called by uGUI Toggle")]
        public void OnPlayerColliderToggle()
        {
            if (_inUpdateUiCall) return;
            playerManager.IsDebug = playerColliderToggle.isOn;
        }

        [PublicAPI("Called by uGUI Toggle")]
        public void OnGunTrailToggle()
        {
            if (_inUpdateUiCall) return;
            gunManager.useDebugBulletTrail = gunTrailToggle.isOn;
        }

        [PublicAPI("Called by uGUI Toggle")]
        public void OnGunPickupSphereToggle()
        {
            if (_inUpdateUiCall) return;
            gunManager.IsDebugGunHandleVisible = gunPickupToggle.isOn;
        }

        [PublicAPI("Called by uGUI Button")]
        public void OnGunResetButton()
        {
            gunResetButton.interactable = false;
            gunManager.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(gunManager.MasterOnly_ResetUnusedGuns));
        }

        [PublicAPI("Called by uGUI Button")]
        public void OnTeamResetButton()
        {
            var players = playerManager.GetPlayers();
            foreach (var player in players)
            {
                if (!playerManager.IsInStaffTeam(player))
                    player.SetTeam(0);
            }
        }

        [PublicAPI("Called by uGUI Button")]
        public void OnTeamShuffleButton()
        {
            var players = playerManager.GetPlayers();

            // create a player list excluding staff players
            var playerList = new DataList();
            foreach (var player in players)
            {
                if (!playerManager.IsInStaffTeam(player))
                    playerList.Add(player);
            }

            // shuffle player list
            for (var i = 0; i < playerList.Count - 1; i++)
            {
                var rnd = Random.Range(i, playerList.Count);
                // UdonSharp does not support deconstruction
                // ReSharper disable once SwapViaDeconstruction
                var temp = playerList[i];
                playerList[i] = playerList[rnd];
                playerList[rnd] = temp;
            }

            // set player teams
            const int teamCount = 2;
            var teamSize = playerList.Count / teamCount;

            for (var team = 0; team < teamCount; team++)
            {
                for (var j = 0; j < teamSize; j++)
                {
                    var i = teamSize * team + j;
                    var player = (PlayerBase)playerList[i].Reference;
                    player.SetTeam(team + 1);
                }
            }
        }

        [PublicAPI("Called by uGUI Dropdown")]
        public void OnFriendlyFireModeDropdown()
        {
            if (_inUpdateUiCall) return;
            playerManager.SetFriendlyFireMode((FriendlyFireMode)friendlyFireDropdown.value);
        }

        #endregion
    }
}