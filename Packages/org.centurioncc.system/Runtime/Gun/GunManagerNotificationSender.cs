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
        private TranslatableMessage onCantShootBecauseStateNotIdle;

        [SerializeField]
        private TranslatableMessage onCantShootBecauseNoBulletInChamber;

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
        private TranslatableMessage onCantShootBecauseMagazineNotInserted;

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
            gunManager.Subscribe(this);
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
            // See ShotReasonId for the meaning of each id
            if (!notifyCancelled)
                return;

            switch (reasonId)
            {
                case ShotReasonId.SelectorSafety:
                    SendNotification(onCantShootWhenSelectorSafety, true);
                    break;
                case ShotReasonId.StateNotIdle:
                    SendNotification(onCantShootBecauseStateNotIdle, true);
                    break;
                case ShotReasonId.NoBulletInChamber:
                    SendNotification(onCantShootBecauseNoBulletInChamber, true);
                    break;
                case ShotReasonId.NoMagazine:
                    SendNotification(onCantShootBecauseMagazineNotInserted, true);
                    break;
                case ShotReasonId.InWall:
                    SendNotification(onCantShootInWall, true);
                    break;
                case ShotReasonId.InSafeZone:
                    SendNotification(onCantShootInSafeZone, true);
                    break;
                case ShotReasonId.CallbackReturnedFalse:
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
