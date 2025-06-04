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
        private const string LogPrefix = "[PlayerManager] ";

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
        public abstract PlayerBase GetPlayer(VRCPlayerApi player);

        /// <summary>
        /// Retrieves a player instance by their VRC player ID.
        /// </summary>
        /// <param name="vrcPlayerId">The VRC player ID of the player to retrieve.</param>
        /// <returns>The <see cref="PlayerBase"/> instance corresponding to the specified VRC player ID, or <c>null</c> if no player is found.</returns>
        [PublicAPI] [CanBeNull]
        public abstract PlayerBase GetPlayerById(int vrcPlayerId);

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
        /// Retrieves an array of players that belong to a specified team.
        /// </summary>
        /// <param name="teamId">The ID of the team whose players are to be retrieved.</param>
        /// <returns>An array of <see cref="PlayerBase"/> instances representing the players in the specified team. Returns an empty array if no players are found for the given team.</returns>
        [PublicAPI]
        public virtual PlayerBase[] GetTeamPlayers(int teamId)
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
        public virtual PlayerBase[] GetDeadTeamPlayers(int teamId)
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
            logger.Log($"{LogPrefix}OnPlayerAdded: {player.DisplayName}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerAdded(player);
            }
        }

        public virtual void Invoke_OnPlayerRemoved(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerRemoved: {player.DisplayName}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerRemoved(player);
            }
        }

        public virtual void Invoke_OnPlayerHitDetection(
            PlayerColliderBase playerCollider, DamageData damageData, Vector3 contactPoint)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerHitDetection: {Networking.GetOwner(playerCollider.gameObject).SafeGetDisplayName()}, {damageData.DamageType}, {contactPoint:F2}");
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerHitDetection(playerCollider, damageData, contactPoint);
            }
        }

        public virtual void Invoke_OnPlayerRevived(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerRevived: {player.DisplayName}");
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
                $"{LogPrefix}OnPlayerKilled: {type.ToEnumName()}, {attacker.DisplayName} -> {victim.DisplayName}");
            victim.UpdateView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerKilled(attacker, victim, type);
            }
        }

        public virtual void Invoke_OnPlayerFriendlyFire(PlayerBase attacker, PlayerBase victim)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerFriendlyFire: {attacker.DisplayName} -> {victim.DisplayName}");
            victim.UpdateView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerFriendlyFire(attacker, victim);
            }
        }

        public virtual void Invoke_OnPlayerFriendlyFireWarning(PlayerBase victim, DamageInfo damageInfo)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerFriendlyFireWarning: {victim.DisplayName}, {damageInfo.DamageType()}");
            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerFriendlyFireWarning(victim, damageInfo);
            }
        }

        public virtual void Invoke_OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            logger.Log(
                $"{LogPrefix}OnPlayerTeamChanged: {player.DisplayName}, {oldTeam} -> {player.TeamId}");
            UpdateAllPlayerView();

            foreach (var callback in EventCallbacks)
            {
                var pmCallback = (PlayerManagerCallbackBase)callback;
                if (pmCallback) pmCallback.OnPlayerTeamChanged(player, oldTeam);
            }
        }

        public virtual void Invoke_OnPlayerReset(PlayerBase player)
        {
            logger.Log($"{LogPrefix}OnPlayerReset: {player.DisplayName}");
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

        #endregion
    }
}