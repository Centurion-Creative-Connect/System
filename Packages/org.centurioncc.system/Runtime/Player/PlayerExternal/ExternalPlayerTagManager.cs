using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.PlayerExternal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalPlayerTagManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleManager roleManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieLogger logger;
        [SerializeField]
        private GameObject sourceRemotePlayerTag;

        private ExternalPlayerTagBase[] _remotePlayerTags = new ExternalPlayerTagBase[0];

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
                if (!Utilities.IsValid(oldPlayerApi))
                    return;
                DestroyPlayerTag(GetOrCreatePlayerTag(oldPlayerApi));
                return;
            }

            var taggedPlayerApi = VRCPlayerApi.GetPlayerById(newId);
            SetupTag(taggedPlayerApi);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!Utilities.IsValid(player.VrcPlayer))
                return;

            var playerTag = GetOrCreatePlayerTag(player.VrcPlayer);
            playerTag.SetTeamTag(player.TeamId, playerManager.GetTeamColor(player.TeamId));

            if (player.IsLocal)
            {
                // Local team change affects almost all player tags, so call changed event method directly.
                OnPlayerTagChanged(TagType.Team, playerManager.ShowTeamTag);
                return;
            }

            var localPlayer = playerManager.GetLocalPlayer();
            var localPlayerTeamId = localPlayer != null ? localPlayer.TeamId : 0;
            var localPlayerInSpecialTeam = playerManager.IsSpecialTeamId(localPlayerTeamId);

            var taggedPlayerTeamId = player.TeamId;
            var taggedPlayerRole = player.Role;
            var inSameTeam = localPlayerTeamId == taggedPlayerTeamId;

            var showAlways = localPlayerInSpecialTeam ||
                             playerManager.IsSpecialTeamId(taggedPlayerTeamId);

            var showTeam = playerManager.ShowTeamTag && (showAlways || inSameTeam);
            var showStaff = playerManager.ShowStaffTag && showAlways;
            var showCreator = playerManager.ShowCreatorTag && showAlways;

            playerTag.SetTagOn(TagType.Team, showTeam);
            playerTag.SetTeamTag(taggedPlayerTeamId, playerManager.GetTeamColor(taggedPlayerTeamId));

            // Need to update role-specific tags since they should not be shown when playing game as team player
            playerTag.SetTagOn(GetStaffTagType(taggedPlayerRole),
                showStaff && taggedPlayerRole.IsGameStaff());
            playerTag.SetTagOn(TagType.Master,
                showStaff && player.VrcPlayer != null && Utilities.IsValid(player.VrcPlayer) &&
                player.VrcPlayer.isMaster);

            playerTag.SetTagOn(TagType.Creator,
                showCreator && taggedPlayerRole.IsGameCreator());
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

            switch (type)
            {
                case TagType.Team:
                {
                    foreach (var playerTag in _remotePlayerTags)
                    {
                        var taggedPlayerApi = playerTag.followingPlayer;
                        if (!Utilities.IsValid(taggedPlayerApi))
                            continue;

                        var taggedPlayer = playerManager.GetPlayerById(taggedPlayerApi.playerId);
                        var taggedPlayerRole = roleManager.GetPlayerRole(taggedPlayerApi);
                        var taggedPlayerTeamId = taggedPlayer != null ? taggedPlayer.TeamId : 0;
                        var inSameTeam = localPlayerTeamId == taggedPlayerTeamId;
                        var showAlways = localPlayerInSpecialTeam ||
                                         playerManager.IsSpecialTeamId(taggedPlayerTeamId);
                        var showTeam = playerManager.ShowTeamTag && (showAlways || inSameTeam);
                        var showStaff = playerManager.ShowStaffTag && showAlways;
                        var showCreator = playerManager.ShowCreatorTag && showAlways;

                        playerTag.SetTagOn(TagType.Team, showTeam);
                        playerTag.SetTeamTag(taggedPlayerTeamId, playerManager.GetTeamColor(taggedPlayerTeamId));

                        // Need to update role-specific tags since they should not be shown when playing game as team player
                        playerTag.SetTagOn(GetStaffTagType(taggedPlayerRole),
                            showStaff && taggedPlayerRole.IsGameStaff());
                        playerTag.SetTagOn(TagType.Master,
                            showStaff && taggedPlayerApi.isMaster);

                        playerTag.SetTagOn(TagType.Creator,
                            showCreator && taggedPlayerRole.IsGameCreator());
                    }

                    break;
                }
                case TagType.Staff:
                {
                    foreach (var playerTag in _remotePlayerTags)
                    {
                        var taggedPlayerApi = playerTag.followingPlayer;
                        if (!Utilities.IsValid(taggedPlayerApi))
                            continue;

                        var taggedPlayer = playerManager.GetPlayerById(taggedPlayerApi.playerId);
                        var taggedPlayerTeamId = taggedPlayer != null ? taggedPlayer.TeamId : 0;
                        var taggedPlayerRole = roleManager.GetPlayerRole(taggedPlayerApi);
                        var shouldShowSpecial = isOn &&
                                                (localPlayerInSpecialTeam ||
                                                 playerManager.IsSpecialTeamId(taggedPlayerTeamId));

                        playerTag.SetTagOn(GetStaffTagType(taggedPlayerRole),
                            shouldShowSpecial && taggedPlayerRole.IsGameStaff());
                        playerTag.SetTagOn(TagType.Master,
                            shouldShowSpecial && taggedPlayerApi.isMaster);
                    }

                    break;
                }
                case TagType.Creator:
                {
                    foreach (var playerTag in _remotePlayerTags)
                    {
                        var taggedPlayerApi = playerTag.followingPlayer;
                        if (!Utilities.IsValid(taggedPlayerApi))
                            continue;

                        var taggedPlayer = playerManager.GetPlayerById(taggedPlayerApi.playerId);
                        var taggedPlayerTeamId = taggedPlayer != null ? taggedPlayer.TeamId : 0;
                        var taggedPlayerRole = roleManager.GetPlayerRole(taggedPlayerApi);
                        var shouldShowSpecial = isOn &&
                                                (localPlayerInSpecialTeam ||
                                                 playerManager.IsSpecialTeamId(taggedPlayerTeamId));

                        playerTag.SetTagOn(TagType.Creator,
                            shouldShowSpecial && taggedPlayerRole.IsGameCreator());
                    }

                    break;
                }
            }
        }

        private void SetupTag(VRCPlayerApi taggedPlayerApi)
        {
            var playerTag = GetOrCreatePlayerTag(taggedPlayerApi);
            var taggedPlayer = playerManager.GetPlayerById(taggedPlayerApi.playerId);
            var taggedPlayerTeamId = taggedPlayer != null ? taggedPlayer.TeamId : 0;
            var taggedPlayerRole = roleManager.GetPlayerRole(taggedPlayerApi);

            var localPlayer = playerManager.GetLocalPlayer();
            var localPlayerTeamId = localPlayer != null ? localPlayer.TeamId : 0;
            var localPlayerInSpecialTeam = playerManager.IsSpecialTeamId(localPlayerTeamId);

            var shouldShowTeam = playerManager.ShowTeamTag && (localPlayerInSpecialTeam ||
                                                               playerManager.IsSpecialTeamId(taggedPlayerTeamId) ||
                                                               localPlayerTeamId == taggedPlayerTeamId);

            playerTag.SetTagOn(TagType.Team, shouldShowTeam);
            playerTag.SetTeamTag(taggedPlayerTeamId, playerManager.GetTeamColor(taggedPlayerTeamId));

            var shouldShowSpecial = localPlayerInSpecialTeam || playerManager.IsSpecialTeamId(taggedPlayerTeamId);
            var shouldShowStaff = shouldShowSpecial && playerManager.ShowStaffTag;

            playerTag.SetTagOn(GetStaffTagType(taggedPlayerRole), shouldShowStaff && taggedPlayerRole.IsGameStaff());
            playerTag.SetTagOn(TagType.Master, shouldShowStaff && taggedPlayerApi.isMaster);

            var shouldShowCreator = shouldShowSpecial && playerManager.ShowCreatorTag;

            playerTag.SetTagOn(TagType.Creator, shouldShowCreator && taggedPlayerRole.IsGameCreator());
        }

        private TagType GetStaffTagType(RoleData role)
        {
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

        private ExternalPlayerTagBase GetOrCreatePlayerTag(VRCPlayerApi api)
        {
            foreach (var playerTag in _remotePlayerTags)
                if (playerTag.followingPlayer == api)
                    return playerTag;

            return CreatePlayerTag(api);
        }

        private ExternalPlayerTagBase CreatePlayerTag(VRCPlayerApi api)
        {
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