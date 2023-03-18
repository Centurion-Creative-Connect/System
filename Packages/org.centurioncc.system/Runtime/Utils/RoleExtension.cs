using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;

namespace CenturionCC.System.Utils
{
    public static class RoleExtension
    {
        public static bool HasPermission(this RoleData role) =>
            role != null && role.RoleProperties.ContainsItem("moderator");

        public static bool IsGameStaff(this RoleData role) =>
            role != null && role.RoleProperties.ContainsItem("staff");

        public static bool IsGameCreator(this RoleData role) =>
            role != null && role.RoleProperties.ContainsItem("creator");
    }
}