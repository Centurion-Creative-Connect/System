using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionPlayerTag : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private PlayerBase player;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private Image image;

        [Header("Color")]
        [SerializeField]
        private bool useTeamColor;

        [Header("Show")]
        [SerializeField]
        private bool showOnDeath;

        [SerializeField]
        private bool showOnFriendlyTeam;

        [SerializeField]
        private bool showOnFreeForAllTeam;

        [SerializeField]
        private bool showOnStaffTeam;

        [Header("Filtering")]
        [SerializeField]
        private bool excludeStaffTeam = true;

        [SerializeField]
        private bool onlyShowOnMaster;

        [Header("Role Name Filtering")]
        [FormerlySerializedAs("targetRoleNames")]
        [SerializeField]
        private string[] onlyShowOnRoleNames;

        [SerializeField]
        private string[] excludeOnRoleNames;

        [Header("Display Name Filtering")]
        [SerializeField]
        private string[] onlyShowOnDisplayNames;

        [SerializeField]
        private string[] excludeOnDisplayNames;

        [Header("PlayerManager Categories")]
        [SerializeField]
        private bool isTeamTag;

        [SerializeField]
        private bool isStaffTag;

        [SerializeField]
        private bool isCreatorTag;

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (useTeamColor)
            {
                image.color = playerManager.GetTeamColor(player.TeamId);
            }

            gameObject.SetActive(ShouldShow());
        }

        private bool ShouldShow()
        {
            return IsEnabledByRole() &&
                   IsEnabledByDisplayName() &&
                   IsEnabledInManager() &&
                   IsEnabledByOptions();
        }

        private bool IsEnabledByOptions()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            var result = false;
            if (showOnFriendlyTeam)
                result |= playerManager.IsFriendly(localPlayer, player);
            if (showOnFreeForAllTeam)
                result |= playerManager.IsInFreeForAllTeam(player) || playerManager.IsInFreeForAllTeam(localPlayer);

            if (showOnStaffTeam)
                result |= playerManager.IsInStaffTeam(player);
            if (showOnDeath)
                result |= player.IsDead;

            if (excludeStaffTeam)
                result &= !playerManager.IsInStaffTeam(player);

            if (onlyShowOnMaster && player.VrcPlayer != null)
                result &= player.VrcPlayer.isMaster;

            return result;
        }

        private bool IsEnabledInManager()
        {
            return (!isTeamTag || playerManager.ShowTeamTag) &&
                   (!isStaffTag || playerManager.ShowStaffTag) &&
                   (!isCreatorTag || playerManager.ShowCreatorTag);
        }

        private bool IsEnabledByDisplayName()
        {
            return (onlyShowOnDisplayNames.Length == 0 || onlyShowOnDisplayNames.ContainsString(player.DisplayName)) &&
                   (excludeOnDisplayNames.Length == 0 || !excludeOnDisplayNames.ContainsString(player.DisplayName));
        }

        #region RoleChecks

        private bool IsEnabledByRole()
        {
            return (onlyShowOnRoleNames.Length == 0 || IsShownByRole()) &&
                   (excludeOnRoleNames.Length == 0 || !IsExcludedByRole());
        }

        private bool IsShownByRole()
        {
            var playerRoles = player.Roles;
            return playerRoles != null && RoleNameMatches(onlyShowOnRoleNames, playerRoles);
        }

        private bool IsExcludedByRole()
        {
            var playerRoles = player.Roles;
            return playerRoles != null && RoleNameMatches(excludeOnRoleNames, playerRoles);
        }

        private static bool RoleNameMatches(string[] roleNames, RoleData[] roles)
        {
            foreach (var role in roles)
            {
                foreach (var roleName in roleNames)
                {
                    if (role.RoleName == roleName) return true;
                }
            }

            return false;
        }

        #endregion
    }
}