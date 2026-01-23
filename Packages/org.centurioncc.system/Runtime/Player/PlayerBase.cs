using CenturionCC.System.Utils;
using DerpyNewbie.Common.Role;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class PlayerBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Update visuals of player.
        /// </summary>
        [PublicAPI]
        public abstract void UpdateView();

        /// <summary>
        /// Resets everything to default values.
        /// </summary>
        [PublicAPI]
        public abstract void ResetToDefault();

        /// <summary>
        /// Resets the player's statistics to 0.
        /// </summary>
        public abstract void ResetStats();

        /// <summary>
        /// Sets current team.
        /// </summary>
        /// <param name="teamId"></param>
        [PublicAPI]
        public abstract void SetTeam(int teamId);

        /// <summary>
        /// Sets current health.
        /// </summary>
        /// <param name="health"></param>
        [PublicAPI]
        public abstract void SetHealth(float health);

        /// <summary>
        /// Sets max health.
        /// </summary>
        /// <param name="maxHealth"></param>
        [PublicAPI]
        public abstract void SetMaxHealth(float maxHealth);

        /// <summary>
        /// Change colliders active state.
        /// </summary>
        /// <remarks>
        /// Only affects the local view of collider. Not synced!
        /// </remarks>
        /// <param name="isActive">Should the colliders be active?</param>
        public abstract void SetCollidersActive(bool isActive);

        /// <summary>
        /// Called by PlayerColliderBase when it has collided with DamageData.
        /// </summary>
        /// <param name="playerCollider"></param>
        /// <param name="data"></param>
        /// <param name="contactPoint"></param>
        [PublicAPI]
        public abstract void OnLocalHit(PlayerColliderBase playerCollider, DamageData data, Vector3 contactPoint);

        /// <summary>
        /// Subtract current health by DamageInfo.
        /// </summary>
        /// <param name="info"></param>
        [PublicAPI]
        public abstract void ApplyDamage(DamageInfo info);

        /// <summary>
        /// Set current health to 0.
        /// </summary>
        [PublicAPI]
        public abstract void Kill();

        /// <summary>
        /// Set current health to max health.
        /// </summary>
        [PublicAPI]
        public abstract void Revive();

        #region Properties
        [PublicAPI]
        public abstract float Health { get; }

        [PublicAPI]
        public abstract float MaxHealth { get; }

        [PublicAPI]
        public abstract int TeamId { get; }

        [PublicAPI]
        public abstract int Kills { get; protected set; }

        [PublicAPI]
        public abstract int Deaths { get; protected set; }

        [PublicAPI]
        public abstract int Score { get; set; }

        [PublicAPI]
        public abstract int KillStreak { get; protected set; }

        // UdonSharp does not support merge conditional expr
        // ReSharper disable once MergeConditionalExpression
        [PublicAPI]
        public virtual int PlayerId => VrcPlayer != null ? VrcPlayer.playerId : -1;

        [PublicAPI]
        public virtual bool IsLocal => PlayerId == Networking.LocalPlayer.playerId;

        [PublicAPI]
        public virtual bool IsDead => Health <= 0;

        [PublicAPI]
        public abstract bool IsInSafeZone { get; }

        [PublicAPI] [CanBeNull]
        public abstract VRCPlayerApi VrcPlayer { get; }

        [PublicAPI] [CanBeNull]
        public abstract RoleData[] Roles { get; }

        [PublicAPI]
        public virtual DamageInfo LastDamageInfo { get; protected set; }

        [PublicAPI]
        public virtual string DisplayName => VrcPlayer.SafeGetDisplayName();
        #endregion

        #region PlayerArea
        [PublicAPI]
        public abstract void OnAreaEnter(PlayerAreaBase area);

        [PublicAPI]
        public abstract void OnAreaExit(PlayerAreaBase area);

        [PublicAPI]
        public abstract PlayerAreaBase[] GetCurrentPlayerAreas();
        #endregion
    }
}
