using CenturionCC.System.UI;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils.Watchdog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CrashGlobalNotifier : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private RoleProvider roleProvider;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieLogger logger;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notificationProvider;
        [UdonSynced]
        private int _syncedErrorCode = -1;

        [UdonSynced]
        private int _syncedPlayerId = -1;

        public void NotifyGlobally(int code)
        {
            _syncedPlayerId = Networking.LocalPlayer.playerId;
            _syncedErrorCode = code;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            logger.LogError(
                $"[CrashNotifier] Player '{NewbieUtils.GetPlayerName(_syncedPlayerId)}' crashed with error code '{_syncedErrorCode}'!");

            if (roleProvider.GetPlayerRole().IsGameStaff())
            {
                notificationProvider.ShowError(
                    $"プレイヤー '{NewbieUtils.GetPlayerName(_syncedPlayerId)}' がエラーコード '{_syncedErrorCode}' を吐いてクラッシュしました\n(スタッフONLY通知)");
            }
        }
    }
}