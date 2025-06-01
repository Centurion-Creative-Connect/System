using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    public abstract class PlayerManager : UdonSharpBehaviour
    {
        private const string Prefix = "[PlayerManager] ";

        [SerializeField] [NewbieInject]
        protected RoleProvider roleProvider;

        [SerializeField] [NewbieInject]
        protected AudioManager audioManager;

        [SerializeField] [NewbieInject]
        protected FootstepAudioStore footstepAudioStore;

        [SerializeField] [NewbieInject]
        protected PrintableBase logger;

        public abstract bool IsDebug { get; set; }
        public abstract bool ShowTeamTag { get; protected set; }
        public abstract bool ShowStaffTag { get; protected set; }
        public abstract bool ShowCreatorTag { get; protected set; }
        public abstract FriendlyFireMode FriendlyFireMode { get; protected set; }

        public virtual int PlayerCount => VRCPlayerApi.GetPlayerCount();

        public virtual int ModeratorPlayerCount
        {
            get
            {
                var players = GetPlayers();
                var result = 0;
                foreach (var player in players)
                {
                    if (player.Role.IsGameStaff())
                        ++result;
                }

                return result;
            }
        }

        public abstract PlayerBase GetLocalPlayer();
        public abstract PlayerBase GetPlayer(VRCPlayerApi player);
        public abstract PlayerBase GetPlayerById(int vrcPlayerId);
        public abstract PlayerBase[] GetPlayers();
        public abstract void SetPlayerTag(TagType type, bool isOn);
        public abstract void SetFriendlyFireMode(FriendlyFireMode mode);

        [PublicAPI]
        public virtual int GetTeamPlayerCount(int teamId, bool includeStaff = true)
        {
            var players = GetPlayers();
            var result = 0;
            foreach (var player in players)
            {
                if (player.TeamId == teamId && (!includeStaff || player.Role.IsGameStaff()))
                    ++result;
            }

            return result;
        }

        [PublicAPI]
        public abstract Color GetTeamColor(int teamId);

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
        public bool IsFriendly(PlayerBase lhs, PlayerBase rhs)
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

        private int _callbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[5];


        public virtual void SubscribeCallback(UdonSharpBehaviour callback)
        {
            CallbackUtil.AddBehaviour(callback, ref _callbackCount, ref _eventCallbacks);
        }

        public virtual void UnsubscribeCallback(UdonSharpBehaviour callback)
        {
            CallbackUtil.RemoveBehaviour(callback, ref _callbackCount, ref _eventCallbacks);
        }

        public void Invoke_OnResetAllPlayerStats()
        {
            logger.Log($"{Prefix}Invoke_OnResetAllPlayerStats");

            foreach (var player in GetPlayers())
                if (player)
                    player.ResetStats();

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnResetAllPlayerStats();
            }
        }

        public void Invoke_OnResetPlayerStats(PlayerBase player)
        {
            if (!player)
            {
                logger.LogWarn($"{Prefix}Invoke_OnResetPlayerStats called with player null");
                return;
            }

            logger.Log(
                $"{Prefix}Invoke_OnResetAllPlayerStats: {player.name}, {NewbieUtils.GetPlayerName(player.PlayerId)}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnResetPlayerStats(player);
            }
        }

        public void Invoke_OnPlayerChanged(PlayerBase player,
            int lastId, bool lastIsMod, bool lastActive)
        {
            if (!player)
            {
                logger.LogWarn($"{Prefix}Invoke_OnPlayerChanged called with player null");
                return;
            }

            if (lastId == player.PlayerId)
            {
                logger.LogWarn($"{Prefix}Invoke_OnPlayerChanged called without actual player id not changed");
                return;
            }

            if (Networking.LocalPlayer == null)
            {
                logger.LogError($"{Prefix}Invoke_OnPlayerChanged during world unload");
                return;
            }

            logger.Log(
                $"{Prefix}Invoke_OnPlayerChanged: {player.name}, {NewbieUtils.GetPlayerName(lastId)}, {NewbieUtils.GetPlayerName(player.PlayerId)}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnPlayerChanged(player, lastId, player.PlayerId);
            }

            if (player.PlayerId == Networking.LocalPlayer.playerId)
                Invoke_OnLocalPlayerChanged(player, player.Index);

            if (lastId == Networking.LocalPlayer.playerId) Invoke_OnLocalPlayerChanged(player, -1);
        }

        public void Invoke_OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
            logger.Log(
                $"{Prefix}Invoke_OnLocalPlayerChanged: {(playerNullable ? playerNullable.name : "null")}. {index}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnLocalPlayerChanged(playerNullable, index);
            }
        }

        public void Invoke_OnTeamChanged(PlayerBase player, int lastTeam)
        {
            if (lastTeam == player.TeamId)
            {
                logger.LogWarn($"{Prefix}Invoke_OnTeamChanged called without actual team not changed");
                return;
            }

            logger.Log(
                $"{Prefix}Invoke_OnTeamChanged: {player.name}, {NewbieUtils.GetPlayerName(player.PlayerId)}, {lastTeam}, {player.TeamId}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnTeamChanged(player, lastTeam);
            }
        }

        public void Invoke_OnFriendlyFire(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            logger.Log(
                $"{Prefix}Invoke_OnFriendlyFire: {NewbieUtils.GetPlayerName(firedPlayer.PlayerId)}, {hitPlayer.TeamId}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnFriendlyFire(firedPlayer, hitPlayer);
            }
        }

        public void Invoke_OnFriendlyFireWarning(PlayerBase victim, DamageData damageData, Vector3 contactPoint)
        {
            logger.Log(
                $"{Prefix}Invoke_OnFriendlyFireWarning: {NewbieUtils.GetPlayerName(victim.PlayerId)}, {contactPoint.ToString("F2")}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnFriendlyFireWarning(victim, damageData, contactPoint);
            }
        }

        public void Invoke_OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint)
        {
            logger.Log($"{Prefix}Invoke_OnHitDetection: " +
                       $"{(playerCollider ? playerCollider.name : "null")}, " +
                       $"{(damageData ? damageData.DamageType : "null")}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnHitDetection(
                    playerCollider,
                    damageData,
                    contactPoint
                );
            }
        }

        public void Invoke_OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer) =>
            Invoke_OnKilled(firedPlayer, hitPlayer, KillType.Default);

        public void Invoke_OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer, KillType type)
        {
            if (!firedPlayer || !hitPlayer)
            {
                logger.LogWarn($"{Prefix}Invoke_OnKilled called without actual player.");
                return;
            }

            logger.Log(
                $"{Prefix}Invoke_OnKilled: {NewbieUtils.GetPlayerName(firedPlayer.VrcPlayer)}, {NewbieUtils.GetPlayerName(hitPlayer.VrcPlayer)}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;

                ((PlayerManagerCallbackBase)callback).OnKilled(firedPlayer, hitPlayer, type);
            }
        }

        public void Invoke_OnPlayerTagChanged(TagType type, bool isOn)
        {
            logger.Log(
                $"{Prefix}Invoke_OnPlayerTagChanged: {type}, {isOn}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnPlayerTagChanged(type, isOn);
            }
        }

        public void Invoke_OnFriendlyFireModeChanged(FriendlyFireMode previousMode)
        {
            logger.Log($"{Prefix}Invoke_OnFriendlyFireModeChanged: {previousMode}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnFriendlyFireModeChanged(previousMode);
            }
        }

        public void Invoke_OnDebugModeChanged(bool isOn)
        {
            logger.Log($"{Prefix}Invoke_OnDebugModeChanged: {isOn}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;
                ((PlayerManagerCallbackBase)callback).OnDebugModeChanged(isOn);
            }
        }

        public void Invoke_OnPlayerRevived(PlayerBase revivedPlayer)
        {
            if (!revivedPlayer)
            {
                logger.LogWarn($"{Prefix}Invoke_OnPlayerRevived called without actual player.");
                return;
            }

            logger.Log($"{Prefix}Invoke_OnPlayerRevived: {NewbieUtils.GetPlayerName(revivedPlayer.VrcPlayer)}");

            foreach (var callback in _eventCallbacks)
            {
                if (!callback) continue;

                ((PlayerManagerCallbackBase)callback).OnPlayerRevived(revivedPlayer);
            }
        }

        #endregion

        #region Obsoletes

        [Obsolete("directly inject reference PrintableBase with NewbieInject instead")]
        public virtual PrintableBase Logger => logger;

        [Obsolete("directly inject reference AudioManager with NewbieInject instead")]
        public virtual AudioManager AudioManager => audioManager;

        [Obsolete("directly inject reference RoleProvider with NewbieInject instead")]
        public virtual RoleProvider RoleManager => roleProvider;

        [Obsolete("directly inject reference FootstepAudioStore with NewbieInject instead")]
        public virtual FootstepAudioStore FootstepAudio => footstepAudioStore;

        [Obsolete("no-op")]
        public virtual bool UseLightweightCollider { get; set; } = false;

        [Obsolete("no-op")]
        public virtual bool AlwaysUseLightweightCollider { get; set; } = false;

        [Obsolete("no-op")]
        public virtual bool UseBaseCollider { get; set; } = true;

        [Obsolete("no-op")]
        public virtual bool UseAdditionalCollider { get; set; } = true;

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int NoneTeamPlayerCount => GetTeamPlayerCount(0);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int NoneTeamModeratorPlayerCount => NoneTeamPlayerCount - GetTeamPlayerCount(0, false);

        [Obsolete("no-op")]
        public virtual int UndefinedTeamPlayerCount => 0;

        [Obsolete("no-op")]
        public virtual int UndefinedTeamModeratorPlayerCount => 0;

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int YellowTeamPlayerCount => GetTeamPlayerCount(2);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int YellowTeamModeratorPlayerCount => YellowTeamPlayerCount - GetTeamPlayerCount(2, false);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int GreenTeamPlayerCount => GetTeamPlayerCount(3);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int GreenTeamModeratorPlayerCount => GreenTeamPlayerCount - GetTeamPlayerCount(4, false);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int RedTeamPlayerCount => GetTeamPlayerCount(1);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int RedTeamModeratorPlayerCount => RedTeamPlayerCount - GetTeamPlayerCount(1, false);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int BlueTeamPlayerCount => GetTeamPlayerCount(4);

        [Obsolete("Use GetTeamPlayerCount(int teamId) instead")]
        public virtual int BlueTeamModeratorPlayerCount => BlueTeamPlayerCount - GetTeamPlayerCount(4, false);


        [Obsolete("no-op. LocalPlayer always exists")]
        public virtual bool HasLocalPlayer() => true;

        [Obsolete]
        public string GetTeamColorString(int teamId)
        {
            return ToHtmlStringRGBA(GetTeamColor(teamId));
        }

        [Obsolete]
        public string GetTeamColoredName(PlayerBase player)
        {
            if (!player) return "Invalid Player (null)";
            return
                $"<color=#{GetTeamColorString(player.TeamId)}>{NewbieUtils.GetPlayerName(player.VrcPlayer)}</color>";
        }

        [Obsolete]
        public string GetHumanFriendlyColoredName(PlayerBase player, string fallbackName = "???")
        {
            if (!player) return fallbackName;
            return
                $"<color=#{GetTeamColorString(player.TeamId)}>{player.VrcPlayer.SafeGetDisplayName(fallbackName)}</color>";
        }

        // From UnityEngine.ColorUtility
        // ReSharper disable once InconsistentNaming
        [Obsolete]
        private static string ToHtmlStringRGBA(Color color)
        {
            var color32 = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(color.r * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * byte.MaxValue), 0, byte.MaxValue));
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
        }

        #endregion
    }
}