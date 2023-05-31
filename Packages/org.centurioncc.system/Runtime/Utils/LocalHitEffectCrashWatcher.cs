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
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private HeadUILocalHitEffect instance;
        private int _duplicatedTimeCount;
        private DateTime _lastCapturedEndTime;

        private DateTime _lastCapturedPlayTime;

        private void Start()
        {
            playerManager.SubscribeCallback(this);
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (!hitPlayer.IsLocal)
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