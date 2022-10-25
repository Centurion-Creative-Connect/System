using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Moderator
{
    [DefaultExecutionOrder(10000)] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModeratorTool : UdonSharpBehaviour
    {
        [Header("Anti-Cheat")]
        [Header("Anti-Cheat Base")]
        public float detectionCounterResetTimeInSeconds = 5F;

        [Header("Anti-Cheat Zombie")]
        public float zombieDetectionTime = 10F;
        public float zombieDetectionTimeCutoff = 5F;
        public int zombieDetectionWarnCount = 2;

        [Header("Anti-Cheat Pitch")]
        public float pitchDetection = -10F;
        public int pitchDetectionWarnCount = 20;
        private GunManager _gunManager;


        private bool _isModeratorMode;
        private NotificationUI _notification;
        private PlayerManager _playerManager;
        private RoleProvider _roleManager;

        public bool IsModeratorMode
        {
            get => _isModeratorMode;
            // Ensure non-moderator cannot enable moderator mode
            set => _isModeratorMode = _roleManager.GetPlayerRole().HasPermission() && value;
        }

        private void Start()
        {
            var game = GameManagerHelper.GetGameManager();
            _playerManager = game.players;
            _gunManager = game.guns;
            _notification = game.notification;
            _roleManager = game.roleProvider;

            // _gunManager.SubscribeCallback(this);
            _playerManager.SubscribeCallback(this);
        }

        private void CheckShot(ManagedGun instance)
        {
            if (!IsModeratorMode) return;

            var holder = instance.CurrentHolder;
            if (holder == null)
                return;

            var holderPlayerId = holder.playerId;
            var player = _playerManager.GetPlayerById(holderPlayerId);

            if (player == null)
                return;

            // TODO: fix this
            // // Pitch Check
            // var pitch = instance.Target.rotation.GetRoll();
            // if (pitch < pitchDetection)
            // {
            //     player.PlayerStats.AntiCheatSuspicionLevel++;
            //     if (player.PlayerStats.AntiCheatSuspicionLevel > pitchDetectionWarnCount)
            //         _notification.ShowWarn(
            //             $"{GameManager.GetPlayerName(player.VrcPlayer)} が曲射撃ちしてるかも!: {pitch:F1} ({player.PlayerStats.AntiCheatSuspicionLevel})");
            // }
            //
            // // Zombie Check
            // var hitTimeDiff = DateTime.Now.Subtract(player.PlayerStats.LastHitTime).TotalSeconds;
            // if (hitTimeDiff > zombieDetectionTimeCutoff && hitTimeDiff < zombieDetectionTime)
            // {
            //     player.PlayerStats.AntiCheatSuspicionLevel++;
            //     if (player.PlayerStats.AntiCheatSuspicionLevel > zombieDetectionWarnCount)
            //         _notification.ShowWarn(
            //             $"{GameManager.GetPlayerName(player.VrcPlayer)} がゾンビしてるかも!: {hitTimeDiff:F1} ({player.PlayerStats.AntiCheatSuspicionLevel})");
            // }
            //
            // // Reset Detection Count if not warned for period
            // if (DateTime.Now.Subtract(player.PlayerStats.AntiCheatLastSuspicionChangedTime).TotalSeconds >
            //     detectionCounterResetTimeInSeconds)
            //     player.PlayerStats.AntiCheatSuspicionLevel = 0;
        }

        // public void OnShoot(ManagedGun instance, ProjectileBase projectile)
        // {
        //     // CheckShot(instance);
        // }

        public void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (!IsModeratorMode) return;

            var firedVrcPlayer = firedPlayer.VrcPlayer;

            if (firedVrcPlayer == null) return;

            var damageType = "Unknown";

            foreach (var gun in _gunManager.ManagedGunInstances)
            {
                if (gun != null && gun.CurrentHolder != null && gun.CurrentHolder.playerId == firedPlayer.PlayerId)
                    damageType = gun.WeaponName;
            }

            if (damageType == "Unknown")
            {
                var leftPickup = firedVrcPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Left);
                if (leftPickup != null)
                {
                    var dmgData = leftPickup.GetComponentInChildren<DamageData>();
                    if (dmgData != null) damageType = dmgData.DamageType;
                }

                var rightPickup = firedVrcPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right);
                if (rightPickup != null)
                {
                    var dmgData = rightPickup.GetComponentInChildren<DamageData>();
                    if (dmgData != null) damageType = dmgData.DamageType;
                }
            }

            _notification.ShowInfo(string.Format
            (
                "Staff Only: Hit Info\n{0} => {1}: {2}",
                NewbieUtils.GetPlayerName(firedVrcPlayer),
                NewbieUtils.GetPlayerName(hitPlayer.VrcPlayer),
                damageType
            ));
        }
    }
}