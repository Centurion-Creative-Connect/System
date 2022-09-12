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
        public NotificationUI notification;

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
            return "0.1.1";
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }

        public void PlayHitLocal(ShooterPlayer shooterPlayer)
        {
            logger.LogVerbose(
                $"{_prefix}PlayHitLocal: {(shooterPlayer != null ? GetPlayerName(shooterPlayer.VrcPlayer) : "Dummy (shooter player null)")}");
            if (shooterPlayer != null) shooterPlayer.PlayHit();

            if (DateTime.Now.Subtract(_lastLocalPlayerHitTime).TotalSeconds > localPlayerHitDuration)
            {
                hitEffect.Play();
                _lastLocalPlayerHitTime = DateTime.Now;
            }
        }

        public void PlayHitRemote(ShooterPlayer shooterPlayer)
        {
            logger.LogVerbose(
                $"{_prefix}PlayHitRemote: {(shooterPlayer != null ? GetPlayerName(shooterPlayer.VrcPlayer) : "Dummy (shooter player null)")}");
            if (shooterPlayer != null) shooterPlayer.PlayHit();
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

        public static string GetPlayerName(VRCPlayerApi api)
        {
            return _GetPlayerName(api, api != null ? api.playerId : -1);
        }

        public static string GetPlayerNameById(int playerId)
        {
            return _GetPlayerName(VRCPlayerApi.GetPlayerById(playerId), playerId);
        }

        private static string _GetPlayerName(VRCPlayerApi api, int id)
        {
            return api == null ? $"{id}:InvalidPlayer" : $"{api.playerId}:{api.displayName}";
        }

        #region ModeratorStuff

        [Obsolete("This method is no longer supported.")]
        public bool GetModeratorMode()
        {
            return false;
        }

        [Obsolete("This method is obsolete. Use RoleManager::GetRole() instead.")]
        public bool IsModerator()
        {
            return roleProvider.GetPlayerRole().HasPermission();
        }

        #endregion

        #region PlayerManagerCallbackBase

        public void OnPlayerChanged(ShooterPlayer player, int oldId, int newId)
        {
        }

        public void OnLocalPlayerChanged(ShooterPlayer playerNullable, int index)
        {
        }

        public void OnFriendlyFire(ShooterPlayer firedPlayer, ShooterPlayer hitPlayer)
        {
        }

        public void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
            if (eventLogger && logHitLocation)
                eventLogger.LogHitDetection(playerCollider, damageData, contactPoint, isShooterDetection);
        }

        public void OnKilled(ShooterPlayer firedPlayer, ShooterPlayer hitPlayer)
        {
            if (hitPlayer.IsLocal)
                PlayHitLocal(hitPlayer);

            PlayHitRemote(hitPlayer);
        }

        public void OnTeamChanged(ShooterPlayer player, int oldTeam)
        {
        }

        public void OnPlayerTagChanged(ShooterPlayer player, TagType type, bool isOn)
        {
        }

        #endregion

        #region GunManagerCallbackBase

        public void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
            logger.LogVerbose(
                $"{_prefix}OnShoot: {(instance != null ? instance.name : "null")}, {(projectile != null ? projectile.name : "null")}");
            if (eventLogger && logShotLocation)
                eventLogger.LogShot(instance, projectile);
        }

        #endregion
    }

    public static class GameManagerHelper
    {
        [PublicAPI]
        public const string GameManagerPath = "Logics/System/GameManager";
        [PublicAPI]
        public const string ConsolePath = "Logics/System/LogTablet/NewbieConsole";
        [PublicAPI]
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
            return GetGameManager().notification;
        }
    }
}