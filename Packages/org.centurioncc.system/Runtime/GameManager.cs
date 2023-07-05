using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
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
        private const string Prefix = "<color=yellow>GameManager</color>::";

        public UpdateManager updateManager;
        public PlayerManager players;
        public GunManager guns;
        public AudioManager audioManager;
        public PlayerMovement movement;
        public PrintableBase logger;
        public NotificationProvider notification;

        public float antiZombieTime = 5F;

        private DateTime _lastLocalPlayerHitTime;

        private void Start()
        {
            logger.Println(GetLicense());
            logger.Println($"Centurion System - v{GetVersion()}");
            logger.LogVerbose($"{Prefix}Subscribing event");
            if (players)
                players.SubscribeCallback(this);
            if (guns)
                guns.SubscribeCallback(this);

            logger.LogVerbose($"{Prefix}Start complete");
        }

        [PublicAPI]
        public static string GetLicense()
        {
            return "Centurion System © 2022 by Centurion Creative Connect is licensed under CC BY-NC 4.0";
        }

        [PublicAPI]
        public static string GetVersion()
        {
            return "0.5.1";
        }

        public bool IsInAntiZombieTime()
        {
            return DateTime.Now.Subtract(_lastLocalPlayerHitTime).TotalSeconds < antiZombieTime;
        }

        #region WatchdogProc

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }

        #endregion

        #region GunManagerCallbackBase

        public void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
        }

        public bool CanShoot()
        {
            if (!Networking.LocalPlayer.IsPlayerGrounded())
            {
                logger.LogVerbose($"{Prefix}CanShoot: cannot shoot because player is not grounded");
                return false;
            }

            if (IsInAntiZombieTime())
            {
                logger.LogVerbose($"{Prefix}CanShoot: cannot shoot because player has been hit");
                return false;
            }

            return true;
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
}