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
        public virtual void OnPlayerChanged(ShooterPlayer player, int oldId, int newId)
        {
        }

        /// <summary>
        /// Called when a <see cref="VRC.SDKBase.Networking.LocalPlayer" /> assigned <see cref="ShooterPlayer" /> was added or
        /// removed
        /// </summary>
        /// <remarks>
        /// Invokes after <see cref="OnPlayerChanged" />.
        /// </remarks>
        /// <param name="playerNullable"><see cref="ShooterPlayer" /> who got assigned. null if removed</param>
        /// <param name="index"><see cref="ShooterPlayer.Index" /> if assigned. -1 if removed</param>
        public virtual void OnLocalPlayerChanged(ShooterPlayer playerNullable, int index)
        {
        }

        /// <summary>
        /// Called when player was friendly fired
        /// </summary>
        /// <remarks>
        /// Invokes only when local player as <see cref="firedPlayer" /> fired to <see cref="hitPlayer" /> as same
        /// <see cref="ShooterPlayer.Team" />.
        /// Invoked even if <see cref="PlayerManager.AllowFriendlyFire" /> is <c>true</c>
        /// </remarks>
        /// <param name="firedPlayer">who fired at <see cref="hitPlayer" /></param>
        /// <param name="hitPlayer">who got shot by <see cref="firedPlayer" /></param>
        public virtual void OnFriendlyFire(ShooterPlayer firedPlayer, ShooterPlayer hitPlayer)
        {
        }

        /// <summary>
        /// Called when <see cref="VRC.SDKBase.Networking.LocalPlayer" /> has damaged <see cref="PlayerCollider" />
        /// </summary>
        /// <param name="playerCollider">which collider got hit by <see cref="DamageData" /></param>
        /// <param name="damageData">which damager was applied for <see cref="PlayerCollider" /></param>
        /// <param name="contactPoint">contact point of collision</param>
        /// <param name="isShooterDetection">if collision was detected by damager holder. should always be true</param>
        public virtual void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
        }


        /// <summary>
        /// Called when <see cref="ShooterPlayer"/>'s <see cref="PlayerStats.Kill"/> and <see cref="PlayerStats.Death"/> were changed.
        /// </summary>
        /// <param name="firedPlayer">Player who updated <see cref="PlayerStats.Kill"/></param>
        /// <param name="hitPlayer">Player who updated <see cref="PlayerStats.Death"/></param>
        public virtual void OnKilled(ShooterPlayer firedPlayer, ShooterPlayer hitPlayer)
        {
        }

        /// <summary>
        /// Called when <see cref="ShooterPlayer" /> changed their <see cref="ShooterPlayer.Team" />
        /// </summary>
        /// <param name="player">player who changed team</param>
        /// <param name="oldTeam">previous team which <see cref="player" /> was assigned</param>
        public virtual void OnTeamChanged(ShooterPlayer player, int oldTeam)
        {
        }

        /// <summary>
        /// Called when either <see cref="PlayerManager.ShowTeamTag" /> or <see cref="ShooterPlayer.PlayerTag" /> was
        /// changed.
        /// </summary>
        /// <param name="player">
        /// player who changed <see cref="ShooterPlayer.PlayerTag" />. null if
        /// <see cref="PlayerManager.ShowTeamTag" /> was changed
        /// </param>
        /// <param name="type">type of tag which was changed</param>
        /// <param name="isOn">the state of <paramref name="type" /> tag shown</param>
        public virtual void OnPlayerTagChanged(ShooterPlayer player, TagType type, bool isOn)
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerManager.MasterOnly_ResetAllPlayerStats"/> was called (Global)
        /// </summary>
        public virtual void OnResetAllPlayerStats()
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerStats.ResetStats"/> was called.
        /// </summary>
        /// <param name="player">a player which called <see cref="PlayerStats.ResetStats"/></param>
        public virtual void OnResetPlayerStats(ShooterPlayer player)
        {
        }
    }
}