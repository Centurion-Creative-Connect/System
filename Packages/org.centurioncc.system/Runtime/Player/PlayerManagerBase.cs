using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    public abstract class PlayerManagerBase : UdonSharpBehaviour
    {
        private const string LogPrefix = "[<color=gold>PlayerManager</color>] ";

        [SerializeField] [NewbieInject]
        protected PrintableBase logger;

        public abstract bool IsDebug { get; set; }
        public abstract bool ShowTeamTag { get; protected set; }
        public abstract bool ShowStaffTag { get; protected set; }
        public abstract bool ShowCreatorTag { get; protected set; }
        public abstract FriendlyFireMode FriendlyFireMode { get; protected set; }

        /// <summary>
        /// Retrieves the local player's <see cref="PlayerBase"/> instance.
        /// </summary>
        /// <returns>The local player's <see cref="PlayerBase"/> instance, or null if the player has not been restored.</returns>
        [PublicAPI] [CanBeNull]
        public abstract PlayerBase GetLocalPlayer();

        /// <summary>
        /// Retrieves the <see cref="PlayerBase"/> instance associated with the specified <see cref="VRCPlayerApi"/> player.
        /// </summary>
        /// <param name="player">The <see cref="VRCPlayerApi"/> instance representing the player to retrieve.</param>
        /// <returns>The <see cref="PlayerBase"/> instance associated with the specified player, or null if not found.</returns>
        [PublicAPI] [CanBeNull]
        public abstract PlayerBase GetPlayer([CanBeNull] VRCPlayerApi player);

        /// <summary>
        /// Retrieves an array of all player instances.
        /// </summary>
        /// <returns>An array of <see cref="PlayerBase"/> representing all players, or an empty array if no players have been restored.</returns>
        [PublicAPI]
        public abstract PlayerBase[] GetPlayers();

        /// <summary>
        /// Toggles a player's tag visibility based on the specified <see cref="TagType"/>.
        /// </summary>
        /// <remarks>
        /// Only works for <see cref="TagType.Team"/>, <see cref="TagType.Staff"/>, and <see cref="TagType.Creator"/>.
        /// </remarks>
        /// <param name="type">The tag type to toggle.</param>
        /// <param name="isOn">A boolean value indicating whether the specified tag should be enabled (true) or disabled (false).</param>
        [PublicAPI]
        public abstract void SetPlayerTag(TagType type, bool isOn);

        /// <summary>
        /// Configures the friendly fire mode.
        /// </summary>
        /// <param name="mode">The desired <see cref="FriendlyFireMode"/> to set.</param>
        [PublicAPI]
        public abstract void SetFriendlyFireMode(FriendlyFireMode mode);

        /// <summary>
        /// Retrieves the color associated with the specified team.
        /// </summary>
        /// <param name="teamId">The unique identifier of the team for which to retrieve the color.</param>
        /// <returns>A <see cref="Color"/> representing the team's color.</returns>
        [PublicAPI]
        public abstract Color GetTeamColor(int teamId);

        #region InternalUtilities

        protected void UpdateAllPlayerView()
        {
            var players = GetPlayers();
            foreach (var player in players)
            {
                player.UpdateView();
            }
        }

        #endregion

        #region GetUtilities

        /// <summary>
        /// Retrieves a player instance by their VRC player ID.
        /// </summary>
        /// <param name="vrcPlayerId">The VRC player ID of the player to retrieve.</param>
        /// <returns>The <see cref="PlayerBase"/> instance corresponding to the specified VRC player ID, or <c>null</c> if no player is found.</returns>
        [PublicAPI] [CanBeNull]
        public virtual PlayerBase GetPlayerById(int vrcPlayerId)
        {
            return GetPlayer(VRCPlayerApi.GetPlayerById(vrcPlayerId));
        }

        /// <summary>
        /// Retrieves an array of players that belong to a specified team.
        /// </summary>
        /// <param name="teamId">The ID of the team whose players are to be retrieved.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing the players in the specified team. Returns an empty array if no players are found for the given team.</returns>
        [PublicAPI]
        public virtual PlayerBase[] GetPlayersInTeam(int teamId)
        {
            var players = GetPlayers();
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
        public virtual PlayerBase[] GetDeadPlayers()
        {
            var players = GetPlayers();
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
        /// <param name="teamId">The ID of the team whose dead players should be retrieved.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing dead players in the specified team, or an empty array if no such players exist.</returns>
        [PublicAPI]
        public virtual PlayerBase[] GetDeadPlayersInTeam(int teamId)
        {
            var players = GetPlayers();
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
        public virtual PlayerBase[] GetModeratorPlayers()
        {
            var players = GetPlayers();
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
        /// <param name="teamId">The ID of the team for which to retrieve the moderator players.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing the moderator players in the specified team, or an empty array if there are none.</returns>
        [PublicAPI]
        public virtual PlayerBase[] GetModeratorPlayersInTeam(int teamId)
        {
            var players = GetPlayers();
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
        /// Null-safe way to get the display name of a <see cref="PlayerBase"/> instance.
        /// </summary>
        /// <param name="player">The <see cref="PlayerBase"/> to retrieve display name</param>
        /// <param name="unknownName">Name used when display name couldn't be retrieved from <paramref name="player"/>.</param>
        /// <param name="appendPlayerId">Append trailing player id?</param>
        /// <returns>`{<see cref="PlayerBase.DisplayName"/>}` or `{<see cref="PlayerBase.DisplayName"/>}.{<see cref="PlayerBase.PlayerId"/>}` or `{<paramref name="unknownName"/>}`</returns>
        [PublicAPI]
        public static string GetDisplayName([CanBeNull] PlayerBase player, string unknownName, bool appendPlayerId)
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
        public static string GetDisplayName([CanBeNull] PlayerBase player, bool appendPlayerId)
        {
            return GetDisplayName(player, "???", appendPlayerId);
        }

        /// <inheritdoc cref="GetDisplayName(CenturionCC.System.Player.PlayerBase,string,bool)"/>
        /// <remarks>
        /// Alias of <see cref="GetDisplayName(CenturionCC.System.Player.PlayerBase,string,bool)"/>. The appendPlayerId is false.
        /// </remarks>
        [PublicAPI]
        public static string GetDisplayName([CanBeNull] PlayerBase player, string unknownName = "???")
        {
            return GetDisplayName(player, unknownName, false);
        }

        #endregion

        #region CheckUtilities

        [PublicAPI]
        public virtual bool IsStaffTeamId(int teamId)
        {
            return teamId == 255;
        }

        [PublicAPI]
        public virtual bool IsFreeForAllTeamId(int teamId)
        {
            return teamId == 0;
        }

        [PublicAPI]
        public virtual bool IsSpecialTeamId(int teamId)
        {
            return IsFreeForAllTeamId(teamId) || IsStaffTeamId(teamId);
        }

        [PublicAPI]
        public virtual bool IsFriendly(PlayerBase lhs, PlayerBase rhs)
        {
            return (lhs.TeamId == rhs.TeamId && !IsInFreeForAllTeam(lhs)) ||
                   (IsInStaffTeam(lhs) || IsInStaffTeam(rhs));
        }

        [PublicAPI]
        public bool IsInFreeForAllTeam(PlayerBase player)
        {
            return IsFreeForAllTeamId(player.TeamId);
        }

        [PublicAPI]
        public bool IsInStaffTeam(PlayerBase player)
        {
            return IsStaffTeamId(player.TeamId);
        }

        [PublicAPI]
        public bool IsInSpecialTeam(PlayerBase player)
        {
            return IsSpecialTeamId(player.TeamId);
        }

        #endregion

        #region PlayerManagerEvents

        protected int CallbackCount;
        protected UdonSharpBehaviour[] EventCallbacks = new UdonSharpBehaviour[5];

        [PublicAPI]
        public virtual void Subscribe(UdonSharpBehaviour callback)
        {
            CallbackUtil.AddBehaviour(callback, ref CallbackCount, ref EventCallbacks);
        }

        [PublicAPI]
        public virtual void Unsubscribe(UdonSharpBehaviour callback)
        {
            CallbackUtil.RemoveBehaviour(callback, ref CallbackCount, ref EventCallbacks);
        }

        public virtual void Invoke_OnPlayerAdded(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerAdded: {GetDisplayName(player, true)}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerAdded(player);
            }
        }

        public virtual void Invoke_OnPlayerRemoved(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerRemoved: {GetDisplayName(player, true)}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerRemoved(player);
            }
        }

        public virtual bool Invoke_OnDamagePreBroadcast(DamageInfo info)
        {
            logger.Log(
                $"{LogPrefix}OnDamagePreBroadcast: {GetDisplayName(GetPlayerById(info.AttackerId()), true)} -> {GetDisplayName(GetPlayerById(info.VictimId()), true)}");
            var result = false;
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) result |= pmCallback.OnDamagePreBroadcast(info);
            }

            logger.Log($"{LogPrefix}OnDamagePreBroadcast-result: {result}");
            return result;
        }

        public virtual bool Invoke_OnDamagePostBroadcast(DamageInfo info)
        {
            logger.Log(
                $"{LogPrefix}OnDamagePostBroadcast: {GetDisplayName(GetPlayerById(info.AttackerId()), true)} -> {GetDisplayName(GetPlayerById(info.VictimId()), true)}");
            var result = false;
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) result |= pmCallback.OnDamagePostBroadcast(info);
            }

            logger.Log($"{LogPrefix}OnDamagePostBroadcast-result: {result}");
            return result;
        }

        public virtual void Invoke_OnPlayerHealthChanged(PlayerBase player, float previousHealth)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerHealthChanged: {GetDisplayName(player, true)}, {previousHealth:F2} -> {player.Health:F2}");

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerHealthChanged(player, previousHealth);
            }
        }

        public virtual void Invoke_OnPlayerRevived(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerRevived: {GetDisplayName(player, true)}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerRevived(player);
            }
        }

        public virtual void Invoke_OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerKilled: {type.ToEnumName()}, {GetDisplayName(attacker, true)} -> {GetDisplayName(victim, true)}");
            victim.UpdateView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerKilled(attacker, victim, type);
            }
        }

        public virtual void Invoke_OnPlayerFriendlyFireWarning(PlayerBase victim, DamageInfo damageInfo)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerFriendlyFireWarning: {GetDisplayName(victim, true)}, {damageInfo.DamageType()}");
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerFriendlyFireWarning(victim, damageInfo);
            }
        }

        public virtual void Invoke_OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerTeamChanged: {GetDisplayName(player, true)}, {oldTeam} -> {player.TeamId}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerTeamChanged(player, oldTeam);
            }
        }

        public virtual void Invoke_OnPlayerStatsChanged(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerStatsChanged: {GetDisplayName(player, true)}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerStatsChanged(player);
            }
        }

        public virtual void Invoke_OnPlayerReset(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerReset: {GetDisplayName(player, true)}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerReset(player);
            }
        }

        public virtual void Invoke_OnPlayerTagChanged(TagType type, bool isOn)
        {
            logger.Log($"{LogPrefix}OnPlayerTagChanged: {type.ToEnumName()}, {isOn}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerTagChanged(type, isOn);
            }
        }

        public virtual void Invoke_OnFriendlyFireModeChanged(FriendlyFireMode previousMode)
        {
            logger.Log(
                $"{LogPrefix}OnFriendlyFireModeChanged: {previousMode.ToEnumName()} -> {FriendlyFireMode.ToEnumName()}");
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnFriendlyFireModeChanged(previousMode);
            }
        }

        public virtual void Invoke_OnDebugModeChanged(bool isOn)
        {
            logger.Log($"{LogPrefix}OnDebugModeChanged: {isOn}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnDebugModeChanged(isOn);
            }
        }

        public virtual void Invoke_OnPlayerEnteredArea(PlayerBase player, PlayerAreaBase area)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerEnteredArea: {GetDisplayName(player, true)}, {area.AreaName} ({player.IsInSafeZone})");
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerEnteredArea(player, area);
            }
        }

        public virtual void Invoke_OnPlayerExitedArea(PlayerBase player, PlayerAreaBase area)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerExitedArea: {GetDisplayName(player, true)}, {area.AreaName} ({player.IsInSafeZone})");
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerExitedArea(player, area);
            }
        }

        #endregion
    }
}