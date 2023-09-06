using System;
using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player
{
    /// <summary>
    /// <see cref="PlayerManager" /> event callback
    /// </summary>
    /// <seealso cref="PlayerManager" />
    /// <seealso cref="PlayerManager.SubscribeCallback" />
    /// <seealso cref="PlayerManager.UnsubscribeCallback" />
    [PublicAPI]
    public abstract class PlayerManagerCallbackBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Called when a VRCPlayerApi was added or removed from ShooterPlayer
        /// </summary>
        /// <param name="player">player who got affected</param>
        /// <param name="oldId">previous <see cref="VRC.SDKBase.VRCPlayerApi.playerId" />.</param>
        /// <param name="newId">added <see cref="VRC.SDKBase.VRCPlayerApi.playerId" />. -1 if removed</param>
        public virtual void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
        }

        /// <summary>
        /// Called when a <see cref="VRC.SDKBase.Networking.LocalPlayer" /> assigned <see cref="PlayerBase" /> was added or
        /// removed
        /// </summary>
        /// <remarks>
        /// Invokes after <see cref="OnPlayerChanged" />.
        /// </remarks>
        /// <param name="playerNullable"><see cref="PlayerBase" /> who got assigned. null if removed</param>
        /// <param name="index"><see cref="PlayerBase.Index" /> if assigned. -1 if removed</param>
        public virtual void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
        }

        /// <summary>
        /// Called when player was friendly fired
        /// </summary>
        /// <remarks>
        /// Invokes only when local player as <see cref="firedPlayer" /> fired to <see cref="hitPlayer" /> as same
        /// <see cref="PlayerBase.TeamId" />.
        /// Invoked even if <see cref="PlayerManager.AllowFriendlyFire" /> is <c>true</c>
        /// </remarks>
        /// <param name="firedPlayer">who fired at <see cref="hitPlayer" /></param>
        /// <param name="hitPlayer">who got shot by <see cref="firedPlayer" /></param>
        public virtual void OnFriendlyFire(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
        }

        /// <summary>
        /// Called when <see cref="VRC.SDKBase.Networking.LocalPlayer"/> has hit <see cref="PlayerCollider"/>
        /// </summary>
        /// <param name="playerCollider">which collider got hit by <see cref="DamageData"/></param>
        /// <param name="damageData">which damager was applied for <see cref="PlayerCollider"/></param>
        /// <param name="contactPoint">contact point of collision</param>
        public virtual void OnHitDetection(
            [CanBeNull] PlayerCollider playerCollider,
            [CanBeNull] DamageData damageData,
            Vector3 contactPoint)
        {
        }

        [Obsolete("Use OnKilled(PlayerBase, PlayerBase, KillType) callback instead.")]
        public virtual void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
        }

        /// <summary>
        /// Called when <paramref name="hitPlayer"/> has been killed by <paramref name="firedPlayer"/>
        /// </summary>
        /// <param name="firedPlayer">player who has attacked <paramref name="hitPlayer"/></param>
        /// <param name="hitPlayer">player who has been attacked by <paramref name="firedPlayer"/></param>
        /// <param name="type">kill type of this event</param>
        public virtual void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer, KillType type)
        {
#pragma warning disable CS0618
            OnKilled(firedPlayer, hitPlayer);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Called when <see cref="PlayerBase" /> changed their <see cref="PlayerBase.TeamId" />
        /// </summary>
        /// <param name="player">player who changed team</param>
        /// <param name="oldTeam">previous team which <see cref="player" /> was assigned</param>
        public virtual void OnTeamChanged(PlayerBase player, int oldTeam)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerManager.ShowTeamTag"/> was changed.
        /// </summary>
        /// <param name="type">type of tag which was changed</param>
        /// <param name="isOn">the state of <paramref name="type" /> tag shown</param>
        public virtual void OnPlayerTagChanged(TagType type, bool isOn)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerManager.MasterOnly_ResetAllPlayerStats"/> was called (Global)
        /// </summary>
        public virtual void OnResetAllPlayerStats()
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerBase.ResetStats"/> was called.
        /// </summary>
        /// <param name="player">a player which called <see cref="PlayerBase.ResetStats"/></param>
        public virtual void OnResetPlayerStats(PlayerBase player)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerBase.HasDied"/> was set to false.
        /// </summary>
        /// <param name="player">a player which changed <see cref="PlayerBase.HasDied"/></param>
        public virtual void OnPlayerRevived(PlayerBase player)
        {
        }
    }
}