using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.External.PlayerTag
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalPlayerTagManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleManager roleManager;

        [SerializeField]
        private GameObject sourceRemotePlayerTag;

        [Header("Settings")]
        [SerializeField]
        [Tooltip("Shows team tag even if local player is not in same team as remote")]
        private bool showOtherTeamsTag;

        [SerializeField]
        [Tooltip("Shows staff tag even if staff player is not in special team")]
        private bool showStaffTagWhileInTeam;

        [SerializeField]
        private bool showStaffTagOnlyInStaffTeam;

        private ExternalPlayerTagBase[] _remotePlayerTags = new ExternalPlayerTagBase[0];

        [PublicAPI]
        public bool ShowOtherTeamsTag
        {
            get => showOtherTeamsTag;
            set
            {
                showOtherTeamsTag = value;
                ReconstructTag();
            }
        }

        [PublicAPI]
        public bool ShowStaffTagWhileInTeam
        {
            get => showStaffTagWhileInTeam;
            set
            {
                showStaffTagWhileInTeam = value;
                ReconstructTag();
            }
        }

        private void Start()
        {
            sourceRemotePlayerTag.SetActive(false);
            playerManager.SubscribeCallback(this);
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            if (newId == -1)
            {
                var oldPlayerApi = VRCPlayerApi.GetPlayerById(oldId);
                DestroyPlayerTag(GetOrCreatePlayerTag(oldPlayerApi));
                return;
            }

            var taggedPlayerApi = VRCPlayerApi.GetPlayerById(newId);
            SetupTag(taggedPlayerApi);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!Utilities.IsValid(player.VrcPlayer)) return;

            var playerTag = GetOrCreatePlayerTag(player.VrcPlayer);
            if (playerTag == null) return;

            var team = player.TeamId;
            playerTag.SetTeamTag(team, playerManager.GetTeamColor(player.TeamId), playerManager.IsStaffTeamId(team));

            if (player.IsLocal)
            {
                // Local team change affects almost all player tags, so call changed event method directly.
                OnPlayerTagChanged(TagType.Team, playerManager.ShowTeamTag);
                return;
            }

            var localPlayer = playerManager.GetLocalPlayer();
            var localPlayerTeamId = localPlayer != null ? localPlayer.TeamId : 0;
            var localPlayerInSpecialTeam = playerManager.IsSpecialTeamId(localPlayerTeamId);

            UpdateTag(playerTag, localPlayerTeamId, localPlayerInSpecialTeam);
        }

        public override void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
            OnPlayerTagChanged(TagType.Team, playerManager.ShowTeamTag);
            OnPlayerTagChanged(TagType.Staff, playerManager.ShowStaffTag);
            OnPlayerTagChanged(TagType.Creator, playerManager.ShowCreatorTag);
        }

        public override void OnPlayerTagChanged(TagType type, bool isOn)
        {
            var localPlayer = playerManager.GetLocalPlayer();
            var localPlayerTeamId = localPlayer != null ? localPlayer.TeamId : 0;
            var localPlayerInSpecialTeam = playerManager.IsSpecialTeamId(localPlayerTeamId);

            foreach (var playerTag in _remotePlayerTags)
            {
                UpdateTag(playerTag, localPlayerTeamId, localPlayerInSpecialTeam);
            }
        }

        public override void OnKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!Utilities.IsValid(victim.VrcPlayer)) return;

            SetupTag(victim.VrcPlayer);
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            if (!Utilities.IsValid(player.VrcPlayer)) return;

            SetupTag(player.VrcPlayer);
        }

        private void SetupTag([CanBeNull] VRCPlayerApi taggedPlayerApi)
        {
            if (!Utilities.IsValid(taggedPlayerApi)) return;

            var playerTag = GetOrCreatePlayerTag(taggedPlayerApi);
            var localPlayer = playerManager.GetLocalPlayer();
            var localPlayerTeamId = localPlayer != null ? localPlayer.TeamId : 0;
            var localPlayerInSpecialTeam = playerManager.IsSpecialTeamId(localPlayerTeamId);

            UpdateTag(playerTag, localPlayerTeamId, localPlayerInSpecialTeam);
        }

        private void UpdateTag([CanBeNull] ExternalPlayerTagBase playerTag,
            int localPlayerTeamId, bool localPlayerInSpecialTeam)
        {
            if (playerTag == null) return;

            var taggedPlayerApi = playerTag.followingPlayer;
            if (!Utilities.IsValid(taggedPlayerApi)) return;

            var taggedPlayer = playerManager.GetPlayerById(taggedPlayerApi.playerId);
            var taggedPlayerRole = roleManager.GetPlayerRole(taggedPlayerApi);
            var taggedPlayerTeamId = taggedPlayer != null ? taggedPlayer.TeamId : 0;
            var teamTagColor = playerManager.GetTeamColor(taggedPlayerTeamId);

            var inSameTeam = localPlayerTeamId == taggedPlayerTeamId;
            var isStaffTeam = playerManager.IsStaffTeamId(taggedPlayerTeamId);
            var isDead = taggedPlayer != null && taggedPlayer.IsDead;

            var isSpecialTeam = localPlayerInSpecialTeam || playerManager.IsSpecialTeamId(taggedPlayerTeamId);
            var isStaffTagVisible =
                (!showStaffTagOnlyInStaffTeam || isStaffTeam) && (showStaffTagWhileInTeam || isSpecialTeam);

            var showTeam = playerManager.ShowTeamTag && (showOtherTeamsTag || isSpecialTeam || inSameTeam);
            var showMaster = playerManager.ShowStaffTag && isSpecialTeam;
            var showStaff = playerManager.ShowStaffTag && isStaffTagVisible;
            var showCreator = playerManager.ShowCreatorTag && isStaffTagVisible;

            playerTag.SetTagOn(TagType.Hit, isDead);

            playerTag.SetTagOn(TagType.Team, showTeam);
            playerTag.SetTeamTag(taggedPlayerTeamId, teamTagColor, isStaffTeam);

            playerTag.SetTagOn(GetStaffTagType(taggedPlayerRole), showStaff && taggedPlayerRole.IsGameStaff());
            playerTag.SetTagOn(TagType.Master, showMaster && taggedPlayerApi.isMaster);

            playerTag.SetTagOn(TagType.Creator, showCreator && taggedPlayerRole.IsGameCreator());
        }

        private static TagType GetStaffTagType([CanBeNull] RoleData role)
        {
            if (role == null) return TagType.Staff;

            switch (role.RoleName)
            {
                case "Owner":
                    return TagType.Owner;
                case "Developer":
                    return TagType.Dev;
                default:
                    return TagType.Staff;
            }
        }

        [CanBeNull]
        private ExternalPlayerTagBase GetOrCreatePlayerTag(VRCPlayerApi api)
        {
            foreach (var playerTag in _remotePlayerTags)
                if (playerTag.followingPlayer == api)
                    return playerTag;

            return CreatePlayerTag(api);
        }

        [CanBeNull]
        private ExternalPlayerTagBase CreatePlayerTag([CanBeNull] VRCPlayerApi api)
        {
            if (api == null || !Utilities.IsValid(api)) return null;

            Debug.Log($"[ExternalPlayerTagManager] Create player tag for '{api.playerId}'");
            var obj = Instantiate(sourceRemotePlayerTag, transform);
            obj.name = $"ExternalPlayerTag_{api.playerId}";
            var playerTag = obj.GetComponent<ExternalPlayerTagBase>();
            playerTag.Setup(this, api);
            _remotePlayerTags = _remotePlayerTags.AddAsList(playerTag);

            return playerTag;
        }

        public void RemovePlayerTag(ExternalPlayerTagBase playerTag)
        {
            Debug.Log(
                $"[ExternalPlayerTagManager] Removing player tag {(playerTag != null ? playerTag.name : "null")}");
            _remotePlayerTags = _remotePlayerTags.RemoveItem(playerTag);
        }

        private void DestroyPlayerTag(ExternalPlayerTagBase playerTag)
        {
            Debug.Log(
                $"[ExternalPlayerTagManager] Destroying player tag '{(playerTag != null ? playerTag.name : "null")}'");
            if (playerTag != null)
                playerTag.DestroyThis();
        }

        public void ClearTag()
        {
            var playerTags = new ExternalPlayerTagBase[_remotePlayerTags.Length];
            Array.Copy(_remotePlayerTags, playerTags, _remotePlayerTags.Length);
            foreach (var playerTag in playerTags)
                DestroyPlayerTag(playerTag);
        }

        public void ReconstructTag()
        {
            ClearTag();
            UpdateTag();
        }

        public void UpdateTag()
        {
            var players = playerManager.GetPlayers();
            foreach (var player in players)
            {
                if (player == null || player.VrcPlayer == null)
                    continue;
                SetupTag(player.VrcPlayer);
            }
        }
    }
}