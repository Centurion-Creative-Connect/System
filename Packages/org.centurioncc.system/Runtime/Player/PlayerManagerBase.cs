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
    [RequireComponent(typeof(PlayerManagerEventHelper))]
    public abstract class PlayerManagerBase : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        protected PrintableBase logger;
        [SerializeField] [NewbieInject]
        protected PlayerManagerEventHelper eventHelper;

        public PlayerManagerEventHelper Event => eventHelper;
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

        #region PlayerManagerEvents
        [PublicAPI]
        public virtual void Subscribe(UdonSharpBehaviour callback)
        {
            Event.Subscribe(callback);
        }

        [PublicAPI]
        public virtual void Unsubscribe(UdonSharpBehaviour callback)
        {
            Event.Unsubscribe(callback);
        }
        #endregion
    }
}
