using JetBrains.Annotations;
namespace CenturionCC.System.Player
{
    public static class PlayerBaseExtensions
    {
        /// <summary>
        /// Null-safe way to get the display name of a <see cref="PlayerBase"/> instance.
        /// </summary>
        /// <param name="player">The <see cref="PlayerBase"/> to retrieve display name</param>
        /// <param name="unknownName">Name used when display name couldn't be retrieved from <paramref name="player"/>.</param>
        /// <param name="appendPlayerId">Append trailing player id?</param>
        /// <returns>`{<see cref="PlayerBase.DisplayName"/>}` or `{<see cref="PlayerBase.DisplayName"/>}.{<see cref="PlayerBase.PlayerId"/>}` or `{<paramref name="unknownName"/>}`</returns>
        [PublicAPI]
        public static string GetDisplayName([CanBeNull] this PlayerBase player, string unknownName, bool appendPlayerId)
        {
            return player
                ? appendPlayerId ? $"{player.DisplayName}.{player.PlayerId}" : player.DisplayName
                : unknownName;
        }

        /// <inheritdoc cref="GetDisplayName(CenturionCC.System.Player.PlayerBase,string,bool)"/>
        /// <remarks>
        /// Alias of <see cref="GetDisplayName(CenturionCC.System.Player.PlayerBase,string,bool)"/>. The unknownName is `???`.
        /// </remarks>
        [PublicAPI]
        public static string GetDisplayName([CanBeNull] this PlayerBase player, bool appendPlayerId)
        {
            return player.GetDisplayName("???", appendPlayerId);
        }

        /// <inheritdoc cref="GetDisplayName(CenturionCC.System.Player.PlayerBase,string,bool)"/>
        /// <remarks>
        /// Alias of <see cref="GetDisplayName(CenturionCC.System.Player.PlayerBase,string,bool)"/>. The appendPlayerId is false.
        /// </remarks>
        [PublicAPI]
        public static string GetDisplayName([CanBeNull] this PlayerBase player, string unknownName = "???")
        {
            return player.GetDisplayName(unknownName, false);
        }

        [PublicAPI]
        public static bool IsFriendly([CanBeNull] this PlayerBase lhs, [CanBeNull] PlayerBase rhs)
        {
            if (!lhs || !rhs) return false;

            return (lhs.TeamId == rhs.TeamId && !lhs.IsInFreeForAllTeam()) ||
                   (lhs.IsInStaffTeam() || rhs.IsInStaffTeam());
        }

        [PublicAPI]
        public static bool IsStaffTeamId(int teamId)
        {
            return teamId == 255;
        }

        [PublicAPI]
        public static bool IsFreeForAllTeamId(int teamId)
        {
            return teamId == 0;
        }

        [PublicAPI]
        public static bool IsSpecialTeamId(int teamId)
        {
            return IsFreeForAllTeamId(teamId) || IsStaffTeamId(teamId);
        }

        [PublicAPI]
        public static bool IsInFreeForAllTeam([CanBeNull] this PlayerBase player)
        {
            return player && IsFreeForAllTeamId(player.TeamId);
        }

        [PublicAPI]
        public static bool IsInStaffTeam([CanBeNull] this PlayerBase player)
        {
            return player && IsStaffTeamId(player.TeamId);
        }

        [PublicAPI]
        public static bool IsInSpecialTeam([CanBeNull] this PlayerBase player)
        {
            return player && IsSpecialTeamId(player.TeamId);
        }
    }
}
