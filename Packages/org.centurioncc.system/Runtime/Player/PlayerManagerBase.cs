using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
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

        [PublicAPI]
        public abstract PlayerBase GetLocalPlayer();

        [PublicAPI]
        public abstract PlayerBase GetPlayer(VRCPlayerApi player);

        [PublicAPI]
        public abstract PlayerBase GetPlayerById(int vrcPlayerId);

        [PublicAPI]
        public abstract PlayerBase[] GetPlayers();

        [PublicAPI]
        public abstract void SetPlayerTag(TagType type, bool isOn);

        [PublicAPI]
        public abstract void SetFriendlyFireMode(FriendlyFireMode mode);

        [PublicAPI]
        public abstract Color GetTeamColor(int teamId);

        [PublicAPI]
        public virtual int GetTeamPlayerCount(int teamId, bool includeStaff = true)
        {
            var players = GetPlayers();
            var result = 0;
            foreach (var player in players)
            {
                if (player.TeamId == teamId && (!includeStaff || player.Roles.IsGameStaff()))
                    ++result;
            }

            return result;
        }

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