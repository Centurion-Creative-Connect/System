using CenturionCC.System.Utils;
using JetBrains.Annotations;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Player
{
    public static class PlayerManagerBaseExtensions
    {
        /// <summary>
        /// Retrieves a player instance by their VRC player ID.
        /// </summary>
        /// <param name="playerManager"></param>
        /// <param name="vrcPlayerId">The VRC player ID of the player to retrieve.</param>
        /// <returns>The <see cref="PlayerBase"/> instance corresponding to the specified VRC player ID, or <c>null</c> if no player is found.</returns>
        [PublicAPI] [CanBeNull]
        public static PlayerBase GetPlayerById(this PlayerManagerBase playerManager, int vrcPlayerId)
        {
            return playerManager.GetPlayer(VRCPlayerApi.GetPlayerById(vrcPlayerId));
        }

        /// <summary>
        /// Retrieves a player's <see cref="PlayerBase"/> instance by their display name.
        /// </summary>
        /// <param name="playerManager"></param>
        /// <param name="displayName">The display name of the player to search for.</param>
        /// <returns>The <see cref="PlayerBase"/> instance of the player with the specified display name, or null if no such player exists.</returns>
        [PublicAPI] [CanBeNull]
        public static PlayerBase GetPlayerByDisplayName(this PlayerManagerBase playerManager, string displayName)
        {
            var players = playerManager.GetPlayers();
            foreach (var player in players)
                if (player.DisplayName == displayName)
                    return player;

            return null;
        }

        /// <summary>
        /// Retrieves an array of players that belong to a specified team.
        /// </summary>
        /// <param name="playerManager"></param>
        /// <param name="teamId">The ID of the team whose players are to be retrieved.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing the players in the specified team. Returns an empty array if no players are found for the given team.</returns>
        [PublicAPI]
        public static PlayerBase[] GetPlayersInTeam(this PlayerManagerBase playerManager, int teamId)
        {
            var players = playerManager.GetPlayers();
            var dataList = new DataList();
            foreach (var player in players)
            {
                if (player.TeamId == teamId) dataList.Add(player);
            }

            var result = new PlayerBase[dataList.Count];
            for (var i = 0; i < dataList.Count; i++)
            {
                result[i] = (PlayerBase)dataList[i].Reference;
            }

            return result;
        }

        /// <summary>
        /// Retrieves an array of all dead players in the game.
        /// </summary>
        /// <returns>An array of <see cref="PlayerBase"/> objects representing the dead players.</returns>
        [PublicAPI]
        public static PlayerBase[] GetDeadPlayers(this PlayerManagerBase playerManager)
        {
            var players = playerManager.GetPlayers();
            var dataList = new DataList();
            foreach (var player in players)
            {
                if (player.IsDead) dataList.Add(player);
            }

            var result = new PlayerBase[dataList.Count];
            for (var i = 0; i < dataList.Count; i++)
            {
                result[i] = (PlayerBase)dataList[i].Reference;
            }

            return result;
        }

        /// <summary>
        /// Retrieves the array of dead players belonging to the specified team.
        /// </summary>
        /// <param name="playerManager"></param>
        /// <param name="teamId">The ID of the team whose dead players should be retrieved.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing dead players in the specified team, or an empty array if no such players exist.</returns>
        [PublicAPI]
        public static PlayerBase[] GetDeadPlayersInTeam(this PlayerManagerBase playerManager, int teamId)
        {
            var players = playerManager.GetPlayers();
            var dataList = new DataList();
            foreach (var player in players)
            {
                if (player.TeamId == teamId && player.IsDead) dataList.Add(player);
            }

            var result = new PlayerBase[dataList.Count];
            for (var i = 0; i < dataList.Count; i++)
            {
                result[i] = (PlayerBase)dataList[i].Reference;
            }

            return result;
        }

        /// <summary>
        /// Retrieves an array of all players designated as moderators.
        /// </summary>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing the moderator players.</returns>
        [PublicAPI]
        public static PlayerBase[] GetModeratorPlayers(this PlayerManagerBase playerManager)
        {
            var players = playerManager.GetPlayers();
            var dataList = new DataList();
            foreach (var player in players)
            {
                if (player.Roles.HasPermission()) dataList.Add(player);
            }

            var result = new PlayerBase[dataList.Count];
            for (var i = 0; i < dataList.Count; i++)
            {
                result[i] = (PlayerBase)dataList[i].Reference;
            }

            return result;
        }

        /// <summary>
        /// Retrieves all moderator players belonging to the specified team.
        /// </summary>
        /// <param name="playerManager"></param>
        /// <param name="teamId">The ID of the team for which to retrieve the moderator players.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing the moderator players in the specified team, or an empty array if there are none.</returns>
        [PublicAPI]
        public static PlayerBase[] GetModeratorPlayersInTeam(this PlayerManagerBase playerManager, int teamId)
        {
            var players = playerManager.GetPlayers();
            var dataList = new DataList();
            foreach (var player in players)
            {
                if (player.TeamId == teamId && player.Roles.HasPermission()) dataList.Add(player);
            }

            var result = new PlayerBase[dataList.Count];
            for (var i = 0; i < dataList.Count; i++)
            {
                result[i] = (PlayerBase)dataList[i].Reference;
            }

            return result;
        }

        /// <summary>
        /// Updates the view of all players.
        /// </summary>
        /// <param name="playerManager"></param>
        [PublicAPI]
        public static void UpdateAllPlayerView(this PlayerManagerBase playerManager)
        {
            var players = playerManager.GetPlayers();
            foreach (var player in players)
            {
                player.UpdateView();
            }
        }
    }
}
