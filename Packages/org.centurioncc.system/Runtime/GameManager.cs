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
        [Obsolete]
        public LocalHitEffect hitEffect;
        public PlayerManager players;
        public GunManager guns;
        public WallManager wall;
        public AudioManager audioManager;
        public RoleProvider roleProvider;
        public PlayerMovement movement;
        public PrintableBase logger;
        [Obsolete]
        public EventLogger eventLogger;
        [Obsolete]
        public FootstepGenerator footstep;
        public ModeratorTool moderatorTool;
        public NotificationProvider notification;
        [NewbieInject]
        public DamageDataResolver resolver;
        [NewbieInject]
        public DamageDataSyncerManager syncer;

        private readonly string _prefix = "<color=yellow>GameManager</color>::";

        private void Start()
        {
            logger.Println(GetLicense());
            logger.Println($"Centurion System - v{GetVersion()}");
            logger.LogVerbose($"{_prefix}Start complete");
        }

        public static string GetLicense()
        {
            return "Centurion System © 2022 by Centurion Creative Connect is licensed under CC BY-NC 4.0";
        }

        public static string GetVersion()
        {
            return "0.6.0-rc.5";
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }

        [Obsolete("Use PlayerBase.IsDead instead")]
        public bool IsInAntiZombieTime()
        {
            var localPlayer = players.GetLocalPlayer();
            return localPlayer != null && localPlayer.IsDead;
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