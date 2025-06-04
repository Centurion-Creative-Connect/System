using System;
using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player
{
    /// <summary>
    /// <see cref="PlayerManagerBase" /> event callback
    /// </summary>
    /// <seealso cref="PlayerManagerBase" />
    /// <seealso cref="PlayerManagerBase.SubscribeCallback" />
    /// <seealso cref="PlayerManagerBase.UnsubscribeCallback" />
    [PublicAPI]
    public abstract class PlayerManagerCallbackBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Called when PlayerBase was added.
        /// </summary>
        /// <param name="player">a joined player's instance of PlayerBase</param>
        public virtual void OnPlayerAdded(PlayerBase player)
        {
        }

        /// <summary>
        /// Called when PlayerBase was removed.
        /// </summary>
        /// <param name="player">a left player's instance of PlayerBase</param>
        public virtual void OnPlayerRemoved(PlayerBase player)
        {
        }

        /// <summary>
        /// Called when <see cref="VRC.SDKBase.Networking.LocalPlayer"/> has hit <see cref="PlayerColliderBase"/>
        /// </summary>
        /// <param name="playerCollider">which collider got hit by <see cref="DamageData"/></param>
        /// <param name="damageData">which damager was applied for <see cref="PlayerColliderBase"/></param>
        /// <param name="contactPoint">contact point of collision</param>
        public virtual void OnPlayerHitDetection(
            [CanBeNull] PlayerColliderBase playerCollider,
            [CanBeNull] DamageData damageData,
            Vector3 contactPoint)
        {
        }

        /// <summary>
        /// Called when <paramref name="victim"/> has been killed by <paramref name="attacker"/>
        /// </summary>
        /// <param name="attacker">player who has attacked <paramref name="victim"/></param>
        /// <param name="victim">player who has been attacked by <paramref name="attacker"/></param>
        /// <param name="type">kill type of this event</param>
        public virtual void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerBase.IsDead"/> was set to false.
        /// </summary>
        /// <param name="player">a player which changed <see cref="PlayerBase.IsDead"/></param>
        public virtual void OnPlayerRevived(PlayerBase player)
        {
        }

        /// <summary>
        /// Called when player was friendly fired
        /// </summary>
        /// <remarks>
        /// Invokes only when local player as <see cref="attacker" /> fired to <see cref="victim" /> as same
        /// <see cref="PlayerBase.TeamId" />.
        /// </remarks>
        /// <param name="attacker">who fired at <see cref="victim" /></param>
        /// <param name="victim">who got shot by <see cref="attacker" /></param>
        public virtual void OnPlayerFriendlyFire(PlayerBase attacker, PlayerBase victim)
        {
        }

        /// <summary>
        /// Called when local player should be warned by friendly fire
        /// </summary>
        /// <param name="victim">victim of friendly fire</param>
        /// <param name="damageInfo">cause of the friendly fire</param>
        public virtual void OnPlayerFriendlyFireWarning(PlayerBase victim, DamageInfo damageInfo)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerBase" /> changed their <see cref="PlayerBase.TeamId" />
        /// </summary>
        /// <param name="player">player who changed team</param>
        /// <param name="oldTeam">previous team which <see cref="player" /> was assigned</param>
        public virtual void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerBase.ResetToDefault"/> was called.
        /// </summary>
        /// <param name="player">a player which called <see cref="PlayerBase.ResetToDefault"/></param>
        public virtual void OnPlayerReset(PlayerBase player)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerManagerBase.ShowTeamTag"/> was changed.
        /// </summary>
        /// <param name="type">type of tag which was changed</param>
        /// <param name="isOn">the state of <paramref name="type" /> tag shown</param>
        public virtual void OnPlayerTagChanged(TagType type, bool isOn)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerManagerBase.FriendlyFireMode"/> was changed.
        /// </summary>
        /// <param name="previousMode">Previously active <see cref="FriendlyFireMode"/></param>
        public virtual void OnFriendlyFireModeChanged(FriendlyFireMode previousMode)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerManagerBase.IsDebug"/> was changed.
        /// </summary>
        /// <param name="isOn"></param>
        public virtual void OnDebugModeChanged(bool isOn)
        {
        }
    }
}