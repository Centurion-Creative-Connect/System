using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Moderator;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System
{
    [DefaultExecutionOrder(-1000)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionSystem : UdonSharpBehaviour
    {
        private const string Prefix = "<color=yellow>CenturionSystem</color>::";

        [SerializeField] [NewbieInject]
        public PrintableBase logger;
        [SerializeField] [NewbieInject]
        public UpdateManager updateManager;
        [SerializeField] [NewbieInject]
        public PlayerManagerBase players;
        [SerializeField] [NewbieInject]
        public GunManagerBase guns;
        [SerializeField] [NewbieInject]
        public AudioManager audioManager;
        [SerializeField] [NewbieInject]
        public RoleProvider roleProvider;
        [SerializeField] [NewbieInject]
        public NotificationProvider notification;
        [SerializeField] [NewbieInject]
        public ModeratorTool moderatorTool;
        [SerializeField] [NewbieInject]
        public PlayerMovement movement;

        [SerializeField] [HideInInspector]
        private string version;
        [SerializeField] [HideInInspector]
        private string commitHash;
        [SerializeField] [HideInInspector]
        private string branch;
        [SerializeField] [HideInInspector]
        private string license;

        private void Start()
        {
            logger.Println(GetLicense());
            logger.Println($"Centurion System - v{GetVersion()} ({GetBranch()}@{GetShortCommitHash()})");
            logger.LogVerbose($"{Prefix}Start complete");
        }

        [PublicAPI]
        public string GetLicense()
        {
            return string.IsNullOrWhiteSpace(license) ? "Unknown" : license;
        }

        [PublicAPI]
        public string GetVersion()
        {
            return string.IsNullOrWhiteSpace(license) ? "Unknown" : version;
        }

        [PublicAPI]
        public string GetCommitHash()
        {
            return commitHash;
        }

        [PublicAPI]
        public string GetShortCommitHash()
        {
            return string.IsNullOrWhiteSpace(commitHash) || commitHash.Length < 7 ? "Unknown" : $"{commitHash.Substring(0, 7)}";
        }

        [PublicAPI]
        public string GetBranch()
        {
            return branch;
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }
    }
}
