using CenturionCC.System.UI;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunManagerNotificationSender : GunManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;

        [Header("Messages")]
        [SerializeField]
        private TranslatableMessage onGunsResetMessage;

        [SerializeField]
        private TranslatableMessage onCantShootInSafeZone;

        [SerializeField]
        private TranslatableMessage onCantShootInWall;

        [SerializeField]
        private TranslatableMessage onCantShootWhenSelectorSafety;

        [SerializeField]
        private TranslatableMessage onCantShootBecauseCallback;

        [SerializeField]
        private TranslatableMessage onCantShootUnknown;

        [SerializeField]
        private TranslatableMessage onFireModeChangeFormatMessage;

        [SerializeField]
        private TranslatableMessage[] fireModeNames;

        [Header("Defaults")]
        public bool notifyGunsReset = true;

        public bool notifyCancelled = true;
        public bool notifyFireModeChange = true;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
        }

        public override void OnGunsReset()
        {
            if (!notifyGunsReset)
                return;

            SendNotification(onGunsResetMessage, false);
        }

        public override void OnShootFailed(GunBase instance, int reasonId)
        {
            SendCancelledOrFailedNotification(reasonId);
        }

        public override void OnShootCancelled(GunBase instance, int reasonId)
        {
            SendCancelledOrFailedNotification(reasonId);
        }

        public override void OnFireModeChanged(GunBase instance)
        {
            if (instance == null || !instance.IsLocal || !notifyFireModeChange)
                return;

            var fireMode = (int)instance.FireMode;
            TranslatableMessage fireModeName = null;
            if (fireMode >= 0 && fireMode < fireModeNames.Length)
                fireModeName = fireModeNames[fireMode];

            SendNotification2(onFireModeChangeFormatMessage, fireModeName);
        }

        private void SendCancelledOrFailedNotification(int reasonId)
        {
            // 10  = ShootNext flag is false
            // 11  = RemoteInstance is null
            // 12  = RemoteInstance.FireMode is 0 == safety
            // 100 = in wall
            // 101 = in safe zone
            // 200 = callback returned false
            if (!notifyCancelled)
                return;

            switch (reasonId)
            {
                case 12:
                    SendNotification(onCantShootWhenSelectorSafety, true);
                    break;
                case 100:
                    SendNotification(onCantShootInWall, true);
                    break;
                case 101:
                    SendNotification(onCantShootInSafeZone, true);
                    break;
                case 200:
                    SendNotification(onCantShootBecauseCallback, false);
                    break;
                default:
                    var cancelledMessage = gunManager.GetCancelledMessageOf(reasonId);
                    if (cancelledMessage != null) SendNotification(cancelledMessage, true);
                    else SendErrNotification(onCantShootUnknown, $"{reasonId}");
                    break;
            }
        }

        private void SendNotification(TranslatableMessage m, bool isWarn)
        {
            if (m == null)
                return;

            if (isWarn)
                notification.ShowWarn(m.Message);
            else
                notification.ShowInfo(m.Message);
        }

        private void SendNotification2(TranslatableMessage format, TranslatableMessage info)
        {
            if (format == null)
                return;

            var infoMsg = info == null ? "Unknown" : info.Message;

            notification.ShowInfo(string.Format(format.Message, infoMsg));
        }

        private void SendErrNotification(TranslatableMessage format, string info)
        {
            if (format == null)
                return;

            notification.ShowError(string.Format(format.Message, info));
        }
    }
}
