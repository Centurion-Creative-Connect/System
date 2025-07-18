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
    /// <seealso cref="PlayerManagerBase.Subscribe" />
    /// <seealso cref="PlayerManagerBase.Unsubscribe" />
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
        /// Called when DamageInfo is about to be broadcasted.
        /// </summary>
        /// <param name="info">info to be broadcasted</param>
        /// <returns>true to interrupt and halt broadcasts, false to let info broadcasted.</returns>
        /// <remarks>
        /// This is called before <see cref="OnDamagePostBroadcast"/>.
        /// While this call is happening, The <paramref name="info"/> has not been broadcasted to other players yet.
        /// Returning true will prevent <see cref="PlayerBase.ApplyDamage"/> from being called.
        /// </remarks>
        public virtual bool OnDamagePreBroadcast(DamageInfo info)
        {
            return false;
        }

        /// <summary>
        /// Called when DamageInfo was broadcasted and is about to be applied.
        /// </summary>
        /// <param name="info">broadcasted info which is about to be applied</param>
        /// <returns>true to interrupt and halt appliance, false to let info be applied.</returns>
        /// <remarks>
        /// This is called after <see cref="OnDamagePreBroadcast"/>.
        /// Returning true will prevent <see cref="PlayerBase.ApplyDamage"/> from being called.
        /// </remarks>
        public virtual bool OnDamagePostBroadcast(DamageInfo info)
        {
            return false;
        }

        /// <summary>
        /// Called when the health of a player changes.
        /// </summary>
        /// <param name="player">The player whose health has changed.</param>
        /// <param name="previousHealth">The player's health value before the change.</param>
        public virtual void OnPlayerHealthChanged(PlayerBase player, float previousHealth)
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
        /// Called when a player's stats are changed.
        /// </summary>
        /// <param name="player">The player whose stats have been updated.</param>
        /// <remarks>
        /// This is called when Kills, Deaths or Score has been changed
        /// </remarks>
        public virtual void OnPlayerStatsChanged(PlayerBase player)
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

        /// <summary>
        /// Called when a player enters a specific area.
        /// </summary>
        /// <param name="player">The instance of <see cref="PlayerBase"/> representing the player who entered the area.</param>
        /// <param name="area">The instance of <see cref="PlayerAreaBase"/> representing the area the player entered.</param>
        public virtual void OnPlayerEnteredArea(PlayerBase player, PlayerAreaBase area)
        {
        }

        /// <summary>
        /// Called when a player exits a specific area.
        /// </summary>
        /// <param name="player">The instance of <see cref="PlayerBase"/> representing the player who exited the area.</param>
        /// <param name="area">The instance of <see cref="PlayerAreaBase"/> representing the area the player exited.</param>
        public virtual void OnPlayerExitedArea(PlayerBase player, PlayerAreaBase area)
        {
        }
    }
}