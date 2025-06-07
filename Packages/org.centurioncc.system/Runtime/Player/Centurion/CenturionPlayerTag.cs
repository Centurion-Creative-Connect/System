using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
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

        [Header("Exclusions")]
        [SerializeField]
        private bool excludeStaffTeam = true;

        [SerializeField]
        private bool onlyShowOnMaster;

        [SerializeField]
        private string[] targetRoleNames;

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

        private bool IsEnabledByRole()
        {
            return !IsRoleRestricted() || HasTargetRole();
        }

        private bool IsRoleRestricted()
        {
            return targetRoleNames.Length != 0;
        }

        private bool HasTargetRole()
        {
            var playerRoles = player.Roles;
            if (playerRoles == null) return false;
            foreach (var targetRoleName in targetRoleNames)
            {
                foreach (var playerRole in playerRoles)
                {
                    if (playerRole.RoleName != targetRoleName) continue;
                    return true;
                }
            }

            return false;
        }
    }
}