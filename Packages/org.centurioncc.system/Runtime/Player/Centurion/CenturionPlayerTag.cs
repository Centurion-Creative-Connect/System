using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
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

        [SerializeField]
        private bool useTeamColor;

        [SerializeField]
        private bool showOnDeath;

        [SerializeField]
        private bool showOnFriendlyTeam;

        [SerializeField]
        private bool excludeStaffTeamFromFriendlyTeam = true;

        [SerializeField]
        private bool showOnFreeForAllTeam;

        [SerializeField]
        private bool showOnStaffTeam;

        [SerializeField]
        private bool onlyShowOnMaster;

        [SerializeField]
        private bool isTeamTag;

        [SerializeField]
        private bool isStaffTag;

        [SerializeField]
        private bool isCreatorTag;

        [SerializeField]
        private RoleData[] targetRoles;

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
            var result = false;
            if (showOnFriendlyTeam)
                result |= playerManager.IsFriendly(playerManager.GetLocalPlayer(), player);
            if (excludeStaffTeamFromFriendlyTeam)
                result &= !playerManager.IsInStaffTeam(player);

            if (showOnFreeForAllTeam)
                result |= playerManager.IsInFreeForAllTeam(player);
            if (showOnStaffTeam)
                result |= playerManager.IsInStaffTeam(player);
            if (showOnDeath)
                result |= player.IsDead;

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
            var hasTargetRole = false;
            foreach (var role in targetRoles)
            {
                if (!player.Roles.ContainsItem(role)) continue;

                hasTargetRole = true;
                break;
            }

            return !IsRoleRestricted() || hasTargetRole;
        }

        private bool IsRoleRestricted()
        {
            return targetRoles.Length != 0;
        }
    }
}