using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;

namespace CenturionCC.System.Utils
{
    public static class RoleExtension
    {
        public static bool HasPermission(this RoleData[] roles)
        {
            foreach (var role in roles)
            {
                if (role.HasPermission()) return true;
            }

            return false;
        }

        public static bool IsGameStaff(this RoleData[] roles)
        {
            foreach (var role in roles)
            {
                if (role.IsGameStaff()) return true;
            }

            return false;
        }

        public static bool IsGameCreator(this RoleData[] roles)
        {
            foreach (var role in roles)
            {
                if (role.IsGameCreator()) return true;
            }

            return false;
        }


        public static bool HasPermission(this RoleData role) =>
            role != null && role.RoleProperties.ContainsItem("moderator");

        public static bool IsGameStaff(this RoleData role) =>
            role != null && role.RoleProperties.ContainsItem("staff");

        public static bool IsGameCreator(this RoleData role) =>
            role != null && role.RoleProperties.ContainsItem("creator");
    }
}