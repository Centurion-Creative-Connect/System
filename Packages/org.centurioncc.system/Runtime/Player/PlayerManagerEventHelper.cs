using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class PlayerManagerEventHelper : UdonSharpBehaviour
    {
        private const string Prefix = "[<color=cyan>PlayerManagerEvent</color>] ";

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;
        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        private int _callbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[5];

        [PublicAPI]
        public bool Subscribe(UdonSharpBehaviour callback)
        {
            return CallbackUtil.AddBehaviour(callback, ref _callbackCount, ref _eventCallbacks);
        }

        [PublicAPI]
        public bool Unsubscribe(UdonSharpBehaviour callback)
        {
            return CallbackUtil.RemoveBehaviour(callback, ref _callbackCount, ref _eventCallbacks);
        }

        public void Invoke_OnPlayerAdded(PlayerBase player)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerAdded: Player is null"))
                return;

            logger.Log($"{Prefix}OnPlayerAdded: {player.GetDisplayName(true)}");
            playerManager.UpdateAllPlayerView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerAdded(player);
            }
        }

        public void Invoke_OnPlayerRemoved(PlayerBase player)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerRemoved: Player is null"))
                return;

            logger.Log($"{Prefix}OnPlayerRemoved: {player.GetDisplayName(true)}");
            playerManager.UpdateAllPlayerView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerRemoved(player);
            }
        }

        public bool Invoke_OnDamagePreBroadcast(DamageInfo info)
        {
            if (ErrorDiagnostic.Assert(info != null, "PlayerManagerEventHelper:OnDamagePreBroadcast: DamageInfo is null"))
                return true;

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnDamagePreBroadcast: {playerManager.GetPlayerById(info.AttackerId()).GetDisplayName(true)} -> {playerManager.GetPlayerById(info.VictimId()).GetDisplayName(true)}");
#endif
            var result = false;

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) result |= pmCallback.OnDamagePreBroadcast(info);
            }

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnDamagePreBroadcast-result: {result}");
#endif
            return result;
        }

        public bool Invoke_OnDamagePostBroadcast(DamageInfo info)
        {
            if (ErrorDiagnostic.Assert(info != null, "PlayerManagerEventHelper:OnDamagePostBroadcast: DamageInfo is null"))
                return true;

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnDamagePostBroadcast: {playerManager.GetPlayerById(info.AttackerId()).GetDisplayName(true)} -> {playerManager.GetPlayerById(info.VictimId()).GetDisplayName(true)}");
#endif
            var result = false;

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) result |= pmCallback.OnDamagePostBroadcast(info);
            }

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnDamagePostBroadcast-result: {result}");
#endif
            return result;
        }

        public void Invoke_OnPlayerHealthChanged(PlayerBase player, float previousHealth)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerHealthChanged: Player is null"))
                return;

            logger.Log($"{Prefix}OnPlayerHealthChanged: {player.GetDisplayName(true)}, {previousHealth:F2} -> {player.Health:F2}");
            player.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerHealthChanged(player, previousHealth);
            }
        }

        public void Invoke_OnPlayerRevived(PlayerBase player)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerRevived: Player is null"))
                return;

            logger.Log($"{Prefix}OnPlayerRevived: {player.GetDisplayName(true)}");
            player.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerRevived(player);
            }
        }

        public void Invoke_OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (ErrorDiagnostic.Assert(attacker, "PlayerManagerEventHelper:OnPlayerKilled: Attacker is null") |
                ErrorDiagnostic.Assert(victim, "PlayerManagerEventHelper:OnPlayerKilled: Victim is null"))
                return;

            logger.Log($"{Prefix}OnPlayerKilled: {type.ToEnumName()}, {attacker.GetDisplayName(true)} -> {victim.GetDisplayName(true)}");
            attacker.UpdateView();
            victim.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerKilled(attacker, victim, type);
            }
        }

        public void Invoke_OnPlayerFriendlyFireWarning(PlayerBase victim, DamageInfo damageInfo)
        {
            if (ErrorDiagnostic.Assert(victim, "PlayerManagerEventHelper:OnPlayerFriendlyFireWarning: Victim is null") |
                ErrorDiagnostic.Assert(damageInfo != null, "PlayerManagerEventHelper:OnPlayerFriendlyFireWarning: DamageInfo is null"))
                return;

            logger.Log($"{Prefix}OnPlayerFriendlyFireWarning: {victim.GetDisplayName(true)}, {damageInfo.DamageType()}");

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerFriendlyFireWarning(victim, damageInfo);
            }
        }

        public void Invoke_OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerTeamChanged: Player is null"))
                return;

            logger.Log($"{Prefix}OnPlayerTeamChanged: {player.GetDisplayName(true)}, {oldTeam} -> {player.TeamId}");
            if (player.IsLocal)
            {
                playerManager.UpdateAllPlayerView();
            }
            else
            {
                player.UpdateView();
            }

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerTeamChanged(player, oldTeam);
            }
        }

        public void Invoke_OnPlayerStatsChanged(PlayerBase player)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerStatsChanged: Player is null"))
                return;

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnPlayerStatsChanged: {player.GetDisplayName(true)}");
#endif
            player.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerStatsChanged(player);
            }
        }

        public void Invoke_OnPlayerReset(PlayerBase player)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerReset: Player is null"))
                return;

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnPlayerReset: {player.GetDisplayName(true)}");
#endif
            player.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerReset(player);
            }
        }

        public void Invoke_OnPlayerTagChanged(TagType type, bool isOn)
        {
            logger.Log($"{Prefix}OnPlayerTagChanged: {type.ToEnumName()}, {isOn}");
            playerManager.UpdateAllPlayerView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerTagChanged(type, isOn);
            }
        }

        public void Invoke_OnFriendlyFireModeChanged(FriendlyFireMode previousMode)
        {
            logger.Log($"{Prefix}OnFriendlyFireModeChanged: {previousMode.ToEnumName()} -> {playerManager.FriendlyFireMode.ToEnumName()}");

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnFriendlyFireModeChanged(previousMode);
            }
        }

        public void Invoke_OnDebugModeChanged(bool isOn)
        {
            logger.Log($"{Prefix}OnDebugModeChanged: {isOn}");
            playerManager.UpdateAllPlayerView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnDebugModeChanged(isOn);
            }
        }

        public void Invoke_OnPlayerEnteredArea(PlayerBase player, PlayerAreaBase area)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerEnteredArea: Player is null") |
                ErrorDiagnostic.Assert(area, "PlayerManagerEventHelper:OnPlayerEnteredArea: Area is null"))
                return;

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnPlayerEnteredArea: {player.GetDisplayName(true)}, {area.AreaName} ({player.IsInSafeZone})");
#endif
            player.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerEnteredArea(player, area);
            }
        }

        public void Invoke_OnPlayerExitedArea(PlayerBase player, PlayerAreaBase area)
        {
            if (ErrorDiagnostic.Assert(player, "PlayerManagerEventHelper:OnPlayerExitedArea: Player is null") |
                ErrorDiagnostic.Assert(area, "PlayerManagerEventHelper:OnPlayerExitedArea: Area is null"))
                return;

#if CENTURIONSYSTEM_PLAYER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnPlayerExitedArea: {player.GetDisplayName(true)}, {area.AreaName} ({player.IsInSafeZone})");
#endif
            player.UpdateView();

            for (var i = 0; i < _callbackCount; i++)
            {
                var pmCallback = (PlayerManagerCallbackBase)_eventCallbacks[i];
                if (pmCallback) pmCallback.OnPlayerExitedArea(player, area);
            }
        }
    }
}
