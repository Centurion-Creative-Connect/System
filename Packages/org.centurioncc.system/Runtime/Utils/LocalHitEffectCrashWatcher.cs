using System;
using CenturionCC.System.Player;
using CenturionCC.System.UI.HeadUI;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(int.MaxValue)]
    public class LocalHitEffectCrashWatcher : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private HeadUILocalHitEffect instance;

        private int _duplicatedTimeCount;
        private DateTime _lastCapturedEndTime;

        private DateTime _lastCapturedPlayTime;

        private void Start()
        {
            playerManager.Subscribe(this);
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!victim.IsLocal)
                return;

            if (_lastCapturedPlayTime == instance.lastHitEffectPlayBeginTime ||
                _lastCapturedEndTime == instance.lastHitEffectPlayEndTime)
                ++_duplicatedTimeCount;
            else
                _duplicatedTimeCount = 0;

            _lastCapturedPlayTime = instance.lastHitEffectPlayBeginTime;
            _lastCapturedEndTime = instance.lastHitEffectPlayEndTime;

            if (_duplicatedTimeCount >= 2)
                WatchdogProc.TryNotifyCrash(943); // sum of chars "local_hit"
        }
    }
}