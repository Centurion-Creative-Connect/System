﻿using System;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.UI
{
    [RequireComponent(typeof(Renderer))] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(50000000)]
    public class SafetyAreaPlatformUI : PlayerManagerCallbackBase
    {
        [SerializeField] private Sprite resetNotYet;
        [SerializeField] private Sprite resetDone;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button[] teamButtons;
        [SerializeField] private GameObject[] moderatorOnlyObjects;
        [SerializeField] private Image resetStatusImage;
        [SerializeField] private Text lastResetText;
        [SerializeField] private Text gunStatusText;
        [SerializeField] private Text playerStatusText;
        [SerializeField] private Text teamStatusText;
        [SerializeField] private Text activeModeratorListText;
        [SerializeField] private Text statisticsText;
        [SerializeField] private Text statusText;
        [SerializeField] private Toggle showTeamTagToggle;
        [SerializeField] private Toggle showStaffTagToggle;

        [SerializeField] [TextArea]
        private string lastResetMessage = "{0}前にリセットしました!";

        [SerializeField] [TextArea]
        private string gunStatusMessage = "{0} 丁がフィールド上に出てます!";

        [SerializeField] [TextArea]
        private string playerStatusMessage = "{0} ({1}) 人がプレイヤーとして居ます!";

        [SerializeField] [TextArea]
        private string teamStatusMessage = "うち " +
                                           "<color=red>{2} ({3})</color> 人が<color=red>赤</color>, " +
                                           "<color=yellow>{4} ({5})</color> 人が<color=yellow>黄</color>" +
                                           "チームです!\n" +
                                           "<color=gray>{0} ({1}) 人はチームに入っていません!</color>";

        [SerializeField] [TextArea]
        private string statisticsMessage = "{0} 回ヒット判定が出ました!\n" +
                                           "  {1} 回はあなたが撃たれたヒット判定でした!\n" +
                                           "  {2} 回はあなたが撃ったヒット判定でした!\n" +
                                           "  {3} 回はフレンドリーファイヤでした!\n" +
                                           "{4} 回BB弾が撃たれました!\n" +
                                           "  {5} 回はあなたが撃ったBB弾でした!\n";

        [SerializeField] [NewbieInject]
        private RoleProvider roleProvider;

        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        private bool _currentResetStatusImage;

        private DateTime _lastGunResetTime;
        private bool _updateActiveModeratorsNextFrame;
        private bool _updatePlayerStatusNextFrame;
        private bool _updateStatisticsNextFrame;


        private void Start()
        {
            foreach (var o in moderatorOnlyObjects)
            {
                if (o == null) continue;
                o.SetActive(false);
            }

            gunManager.SubscribeCallback(this);
            playerManager.Subscribe(this);

            SendCustomEventDelayedSeconds(nameof(UpdateToggleState), 5F);
            SendCustomEventDelayedSeconds(nameof(UpdateModeratorOnlyObjects), 5F);
            SendCustomEventDelayedSeconds(nameof(UpdateStatisticsText), 10F);
            SendCustomEventDelayedSeconds(nameof(UpdatePlayerStatusText), 10F);
            SendCustomEventDelayedSeconds(nameof(UpdateGunStatusText), 10F);
        }

        private void OnWillRenderObject()
        {
            UpdateResetStatusText();
            UpdateInfoText();

            if (_updatePlayerStatusNextFrame)
            {
                _updatePlayerStatusNextFrame = false;
                UpdatePlayerStatusText();
                UpdateTeamStatusText();
            }

            if (_updateStatisticsNextFrame)
            {
                _updateStatisticsNextFrame = false;
                UpdateStatisticsText();
            }

            if (_updateActiveModeratorsNextFrame)
            {
                _updateActiveModeratorsNextFrame = false;
                UpdateActiveModeratorsText();
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            _updateActiveModeratorsNextFrame = true;
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _updateActiveModeratorsNextFrame = true;
        }

        public void ResetButtonOnClick()
        {
            Debug.Log("[SafetyAreaPlatformUI] ResetButtonOnClick");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(DisableResetButton));
        }

        public void TeamButtonOnClick()
        {
            Debug.Log("[SafetyAreaPlatformUI] TeamButtonOnClick");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TeamButtonOnOff));
        }

        public void UpdateModeratorOnlyObjects()
        {
            var isMod = roleProvider.GetPlayerRole().HasPermission();
            foreach (var o in moderatorOnlyObjects)
            {
                if (o == null) continue;
                o.SetActive(isMod);
            }
        }

        public void UpdateToggleState()
        {
            showTeamTagToggle.SetIsOnWithoutNotify(playerManager.ShowTeamTag);
            showStaffTagToggle.SetIsOnWithoutNotify(playerManager.ShowStaffTag);
        }

        public void UpdatePlayerStatusText()
        {
            playerStatusText.text =
                string.Format(playerStatusMessage, VRCPlayerApi.GetPlayerCount(), "UNKNOWN");
        }

        public void UpdateTeamStatusText()
        {
            var p = playerManager;
            teamStatusText.text = string.Format(
                teamStatusMessage,
                p.GetPlayersInTeam(0).Length,
                p.GetModeratorPlayersInTeam(0).Length,
                p.GetPlayersInTeam(1).Length,
                p.GetModeratorPlayersInTeam(1).Length,
                p.GetPlayersInTeam(2).Length,
                p.GetModeratorPlayersInTeam(2).Length,
                p.GetPlayersInTeam(3).Length,
                p.GetModeratorPlayersInTeam(3).Length,
                p.GetPlayersInTeam(4).Length,
                p.GetModeratorPlayersInTeam(4).Length
            );
        }

        public void UpdateGunStatusText()
        {
            gunStatusText.text = string.Format(gunStatusMessage, gunManager.OccupiedRemoteGunCount);
        }

        public void UpdateResetStatusText()
        {
            var diff = DateTime.Now.Subtract(_lastGunResetTime);
            if (diff.TotalSeconds < 60)
                lastResetText.text = string.Format(lastResetMessage, diff.ToString(@"%s' 秒'"));
            else if (diff.TotalMinutes < 60)
                lastResetText.text = string.Format(lastResetMessage, diff.ToString(@"mm' 分 'ss' 秒'"));
            else if (diff.TotalHours < 24)
                lastResetText.text = string.Format(lastResetMessage, diff.ToString(@"hh' 時間 'mm' 分 'ss' 秒'"));
            else
                lastResetText.text = string.Format(lastResetMessage, "1日以上");

            if (diff.TotalMinutes > 5 && _currentResetStatusImage)
            {
                _currentResetStatusImage = false;
                resetStatusImage.sprite = resetNotYet;
            }
            else if (diff.TotalMinutes < 5 && !_currentResetStatusImage)
            {
                _currentResetStatusImage = true;
                resetStatusImage.sprite = resetDone;
            }
        }

        public void UpdateStatisticsText()
        {
            statisticsText.text = string.Format(statisticsMessage,
                _localHitCount + _remoteHitCount,
                _hitDetectionCount,
                _localHitCount,
                _ffCount,
                _globalShotCount,
                _localShotCount);
        }

        public void UpdateInfoText()
        {
            statusText.text =
                $"{(Networking.IsClogged ? "<color=yellow>Sync in Progress!</color>" : "<color=green>Sync OK!</color>")}";
        }

        public void UpdateActiveModeratorsText()
        {
            // TODO: not yet impl
            var message = "";
            var modPlayers = roleProvider.GetPlayersOf(roleProvider.RoleOf("Staff"));
            foreach (var modPlayerApi in modPlayers)
            {
                var player = playerManager.GetPlayerById(modPlayerApi.playerId);
                message += $"{(!player ? NewbieUtils.GetPlayerName(modPlayerApi.playerId) : player.DisplayName)}\n";
            }

            activeModeratorListText.text = message;
        }

        public void TeamButtonOnOff()
        {
            DisableTeamButton();
            SendCustomEventDelayedSeconds(nameof(EnableTeamButton), 5f);
        }

        public void DisableTeamButton()
        {
            foreach (var button in teamButtons)
                button.interactable = false;
        }

        public void EnableTeamButton()
        {
            foreach (var button in teamButtons)
                button.interactable = true;
        }

        public void DisableResetButton()
        {
            resetButton.interactable = false;
        }

        public void EnableResetButton()
        {
            resetButton.interactable = true;
        }

        #region StatsCounter

        private int _ffCount;
        private int _hitDetectionCount;
        private int _localHitCount;
        private int _remoteHitCount;
        private int _globalShotCount;
        private int _localShotCount;

        #endregion

        #region PlayerManagerCallback

        public override void OnPlayerAdded(PlayerBase player)
        {
            _updatePlayerStatusNextFrame = true;
        }

        public override void OnPlayerRemoved(PlayerBase player)
        {
            _updatePlayerStatusNextFrame = true;
        }

        public override bool OnDamagePreBroadcast(DamageInfo info)
        {
            ++_hitDetectionCount;
            _updateStatisticsNextFrame = true;

            return false;
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (victim.IsLocal)
                ++_localHitCount;
            else
                ++_remoteHitCount;

            if (type == KillType.FriendlyFire)
                ++_ffCount;

            _updateStatisticsNextFrame = true;
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            _updatePlayerStatusNextFrame = true;
            _updateActiveModeratorsNextFrame = true;
        }

        public override void OnPlayerTagChanged(TagType type, bool isOn)
        {
            UpdateToggleState();
        }

        #endregion

        #region GunManagerCallback

        public bool CanShoot()
        {
            return true;
        }

        public void OnGunsReset(GunManagerResetType type)
        {
            _lastGunResetTime = DateTime.Now;
            UpdateResetStatusText();
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(EnableResetButton));
        }

        public void OnOccupyChanged(ManagedGun instance)
        {
            UpdateGunStatusText();
        }

        public void OnShoot(ManagedGun instance, GunBullet bullet)
        {
            ++_globalShotCount;
            if (instance != null && instance.IsLocal)
                ++_localShotCount;
            _updateStatisticsNextFrame = true;
        }

        #endregion
    }
}