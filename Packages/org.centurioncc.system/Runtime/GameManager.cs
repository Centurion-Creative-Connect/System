using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Moderator;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System
{
    [DefaultExecutionOrder(-1000)] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GameManager : UdonSharpBehaviour
    {
        public UpdateManager updateManager;
        public LocalHitEffect hitEffect;
        public PlayerManager players;
        public GunManager guns;
        public WallManager wall;
        public AudioManager audioManager;
        public RoleProvider roleProvider;
        public PlayerMovement movement;
        public PrintableBase logger;
        public EventLogger eventLogger;
        public FootstepGenerator footstep;
        public ModeratorTool moderatorTool;
        public NotificationProvider notification;

        public bool logHitLocation = true;
        public bool logShotLocation = true;

        public float localPlayerHitDuration = 1F;
        public float antiZombieTime = 5F;

        private readonly string _prefix = "<color=yellow>GameManager</color>::";

        private DateTime _lastLocalPlayerHitTime;

        private void Start()
        {
            logger.Println(GetLicense());
            logger.Println($"Centurion System - v{GetVersion()}");
            logger.LogVerbose($"{_prefix}Subscribing event");
            if (players)
                players.SubscribeCallback(this);
            if (guns)
                guns.SubscribeCallback(this);

            logger.LogVerbose($"{_prefix}Start complete");
        }

        public static string GetLicense()
        {
            return "Centurion System © 2022 by Centurion Creative Connect is licensed under CC BY-NC 4.0";
        }

        public static string GetVersion()
        {
            return "0.5.1";
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }

        [Obsolete("Use PlayerBase.OnDeath() directly.")]
        public void PlayHitLocal(PlayerBase player)
        {
            PlayOnDeath(player);
        }

        [Obsolete("Use PlayerBase.OnDeath() directly.")]
        public void PlayHitRemote(PlayerBase player)
        {
            PlayOnDeath(player);
        }

        [Obsolete("Use PlayerBase.OnDeath() directly.")]
        private void PlayOnDeath(PlayerBase player)
        {
            logger.LogVerbose(
                $"{_prefix}PlayOnDeath: {(player != null ? NewbieUtils.GetPlayerName(player.VrcPlayer) : "Dummy (shooter player null)")}");
            if (player != null)
                player.OnDeath();
        }

        public bool CanShoot()
        {
            if (!Networking.LocalPlayer.IsPlayerGrounded())
            {
                logger.LogVerbose($"{_prefix}CanShoot: cannot shoot because player is not grounded");
                return false;
            }

            if (IsInAntiZombieTime())
            {
                logger.LogVerbose($"{_prefix}CanShoot: cannot shoot because player has been hit");
                return false;
            }

            return true;
        }

        public bool IsInAntiZombieTime()
        {
            return DateTime.Now.Subtract(_lastLocalPlayerHitTime).TotalSeconds < antiZombieTime;
        }

        [Obsolete("Use NewbieUtils.GetPlayerName() instead")]
        public static string GetPlayerName(VRCPlayerApi api)
        {
            return NewbieUtils.GetPlayerName(api);
        }

        [Obsolete("Use NewbieUtils.GetPlayerName() instead")]
        public static string GetPlayerNameById(int playerId)
        {
            return NewbieUtils.GetPlayerName(playerId);
        }

        #region GunManagerCallbackBase

        public void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
            if (eventLogger && logShotLocation)
                eventLogger.LogShot(instance, projectile);
        }

        #endregion

        #region PlayerManagerCallbackBase

        public void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
        }

        public void OnLocalPlayerChanged(PlayerBase playerNullable, int index)
        {
        }

        public void OnFriendlyFire(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
        }

        public void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
            if (eventLogger && logHitLocation)
                eventLogger.LogHitDetection(playerCollider, damageData, contactPoint, isShooterDetection);
        }

        public void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (hitPlayer.IsLocal)
                _lastLocalPlayerHitTime = DateTime.Now;
        }

        public void OnTeamChanged(PlayerBase player, int oldTeam)
        {
        }

        public void OnPlayerTagChanged(TagType type, bool isOn)
        {
        }

        #endregion
    }

    // Obsolete from v0.2
    [Obsolete("Use CenturionSystemReference instead.")]
    public static class GameManagerHelper
    {
        [PublicAPI] [Obsolete(
            "Do not reference the direct path. Use CenturionSystemReference.GetGameManager() instead.")]
        public const string GameManagerPath = "Logics/System/GameManager";
        [PublicAPI] [Obsolete("Do not reference the direct path. Use CenturionSystemReference.GetConsole() instead.")]
        public const string ConsolePath = "Logics/System/LogTablet/NewbieConsole";
        [PublicAPI] [Obsolete("Do not reference the direct path. Use CenturionSystemReference.GetLogger() instead.")]
        public const string LoggerPath = "Logics/System/LogTablet/NewbieLogger";

        [PublicAPI]
        public static GameManager GetGameManager()
        {
            return GameObject.Find(GameManagerPath).GetComponent<GameManager>();
        }

        [PublicAPI]
        public static PrintableBase GetLogger()
        {
            return GetGameManager().logger;
        }

        [PublicAPI]
        public static UpdateManager GetUpdateManager()
        {
            return GetGameManager().updateManager;
        }

        [PublicAPI]
        public static PlayerManager GetPlayerManager()
        {
            return GetGameManager().players;
        }

        [PublicAPI]
        public static GunManager GetGunManager()
        {
            return GetGameManager().guns;
        }

        [PublicAPI]
        public static AudioManager GetAudioManager()
        {
            return GetGameManager().audioManager;
        }

        [PublicAPI]
        public static NotificationUI GetNotificationUI()
        {
            return (NotificationUI)GetGameManager().notification;
        }
    }
}