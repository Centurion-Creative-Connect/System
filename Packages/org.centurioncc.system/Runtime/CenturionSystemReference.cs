using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
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
        private const string GameManagerPath = "Logics/System/GameManager";
        private const string ConsolePath = "Logics/System/LogTablet/NewbieConsole";

        [PublicAPI]
        public static GameManager GetGameManager()
        {
            return GameObject.Find(GameManagerPath).GetComponent<GameManager>();
        }

        public static NewbieConsole GetConsole()
        {
            return GameObject.Find(ConsolePath).GetComponent<NewbieConsole>();
        }

        public static PrintableBase GetLogger()
        {
            return GetGameManager().logger;
        }

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