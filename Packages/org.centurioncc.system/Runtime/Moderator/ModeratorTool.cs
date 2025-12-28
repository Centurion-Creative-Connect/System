using System;
using CenturionCC.System.Gun;
using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace CenturionCC.System.Moderator
{
    [DefaultExecutionOrder(10000)] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModeratorTool : GunManagerCallbackBase
    {
        [Header("Anti-Cheat")]
        [Header("Anti-Cheat Base")]
        public float detectionCounterResetTimeInSeconds = 5F;

        [Header("Anti-Cheat Pitch")]
        public float pitchDetection = -10F;

        public int pitchDetectionWarnCount = 20;

        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;

        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleProvider roleManager;

        private readonly DataDictionary _suspicionDict = new DataDictionary();

        [Obsolete("no-op")]
        public bool IsModeratorMode { get; set; }

        private void OnEnable()
        {
            if (roleManager.GetPlayerRole().IsGameStaff())
                gunManager.SubscribeCallback(this);
        }

        private void OnDisable()
        {
            gunManager.UnsubscribeCallback(this);
        }

        public override void OnShoot(GunBase instance, ProjectileBase projectile)
        {
            CheckShot(instance);
        }

        private void CheckShot(GunBase instance)
        {
            var holder = instance.CurrentHolder;
            if (holder == null)
                return;

            var pitch = instance.Target.rotation.GetRoll();
            if (pitch < pitchDetection)
            {
                var susLevel = GetPlayerSuspicionLevel(holder.playerId) + 1;
                SetPlayerSuspicionLevel(holder.playerId, susLevel);
                if (susLevel > pitchDetectionWarnCount)
                    notification.ShowWarn(
                        $"STAFF ONLY: {NewbieUtils.GetPlayerName(holder)} が曲射撃ちしてるかも!: {pitch:F1} ({susLevel})",
                        5F,
                        1804983 + holder.playerId
                    );
            }

            if (Time.timeSinceLevelLoad - GetPlayerSuspicionLastUpdated(holder.playerId) >
                detectionCounterResetTimeInSeconds)
                SetPlayerSuspicionLevel(holder.playerId, 0);
        }

        private int GetPlayerSuspicionLevel(int playerId)
        {
            return GetPlayerSuspicionDict(playerId)["suspicionLevel"].Int;
        }
        
        private void SetPlayerSuspicionLevel(int playerId, int level)
        {
            var dict = GetPlayerSuspicionDict(playerId);
            dict["suspicionLevel"] = level;
            dict["lastUpdated"] = Time.timeSinceLevelLoad;
        }

        private float GetPlayerSuspicionLastUpdated(int playerId)
        {
            return GetPlayerSuspicionDict(playerId)["lastUpdated"].Float;
        }

        private DataDictionary GetPlayerSuspicionDict(int playerId)
        {
            if (_suspicionDict.ContainsKey(playerId))
                return _suspicionDict[playerId].DataDictionary;

            var dict = new DataDictionary();
            dict.Add("suspicionLevel", 0);
            dict.Add("lastUpdated", 0.0F);
            
            _suspicionDict.Add(playerId, dict);
            return dict;
        }
    }
}
