using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UnityEngine;

namespace CenturionCC.System
{
    /// <summary>
    /// Still same as GameManagerHelper, though on future this should be resolved automatically / rebind-able scene by scene.
    /// </summary>
    public static class CenturionSystemReference
    {
        /**
         * TODO: resolve path scene by scene
         * Maybe get or create new UdonSharpBehaviour called `CenturionSystemReferenceInternal`
         * Then make it always same path on build for easier resolving?
         */
        private const string CenturionSystemPath = "Logics/System/GameManager";

        private const string ConsolePath = "Logics/System/LogTablet/NewbieConsole";
        private const string PlayerControllerPath = "Logics/System/PlayerController";

        [PublicAPI]
        public static CenturionSystem GetCenturionSystem()
        {
            return GameObject.Find(CenturionSystemPath).GetComponent<CenturionSystem>();
        }

        public static NewbieConsole GetConsole()
        {
            return GameObject.Find(ConsolePath).GetComponent<NewbieConsole>();
        }

        public static PlayerController GetPlayerController()
        {
            var obj = GameObject.Find(PlayerControllerPath);
            return obj != null ? obj.GetComponent<PlayerController>() : null;
        }

        public static PrintableBase GetLogger()
        {
            return GetCenturionSystem().logger;
        }

        public static UpdateManager GetUpdateManager()
        {
            return GetCenturionSystem().updateManager;
        }

        [PublicAPI]
        public static PlayerManagerBase GetPlayerManager()
        {
            return GetCenturionSystem().players;
        }

        [PublicAPI]
        public static GunManagerBase GetGunManager()
        {
            return GetCenturionSystem().guns;
        }

        [PublicAPI]
        public static AudioManager GetAudioManager()
        {
            return GetCenturionSystem().audioManager;
        }

        public static NotificationProvider GetNotificationProvider()
        {
            return GetCenturionSystem().notification;
        }

        [PublicAPI] [Obsolete("Use GetNotificationProvider() instead")]
        public static NotificationUI GetNotificationUI()
        {
            return (NotificationUI)GetCenturionSystem().notification;
        }
    }
}
