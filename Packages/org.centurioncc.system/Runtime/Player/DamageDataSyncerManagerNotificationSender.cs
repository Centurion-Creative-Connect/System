using CenturionCC.System.UI;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DamageDataSyncerManagerNotificationSender : DamageDataSyncerManagerCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private DamageDataSyncerManager manager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;

        [SerializeField] private float notificationDuration = 5F;

        [Header("Target")] [SerializeField] [Tooltip("Do notify hit cancellation when attacker was local player?")]
        public bool doNotifyLocalHitCancellation = true;

        [SerializeField] [Tooltip("Do notify hit cancellation when attacker was NOT local player?")]
        public bool doNotifyNonLocalHitCancellation = false;

        [Header("Type")]
        [SerializeField]
        [Tooltip("Do notify hit cancellation when it was caused by attacker already dead?")]
        public bool doNotifyAttackerDeadCancellation = true;

        [SerializeField] [Tooltip("Do notify hit cancellation when it was caused by victim already dead?")]
        public bool doNotifyVictimDeadCancellation = false;

        [Header("Messages")] [SerializeField] private TranslatableMessage onLocalHitCancelledAsAttackerDeadMessage;

        [SerializeField] private TranslatableMessage onLocalHitCancelledAsVictimDeadMessage;

        [SerializeField] private TranslatableMessage onNonLocalHitCancelledAsAttackerDeadMessage;

        [SerializeField] private TranslatableMessage onNonLocalHitCancelledAsVictimDeadMessage;

        private void Start()
        {
            manager.SubscribeCallback(this);
        }

        public override void OnSyncerReceived(DamageDataSyncer syncer)
        {
            if (syncer.Result != SyncResult.Cancelled) return;

            if (doNotifyLocalHitCancellation &&
                syncer.AttackerId == Networking.LocalPlayer.playerId)
            {
                switch (syncer.ResultContext)
                {
                    case SyncResultContext.AttackerAlreadyDead:
                        if (doNotifyAttackerDeadCancellation)
                        {
                            SendNotification(
                                1985120,
                                onLocalHitCancelledAsAttackerDeadMessage,
                                NewbieUtils.GetPlayerName(syncer.VictimId)
                            );
                        }

                        break;
                    case SyncResultContext.VictimAlreadyDead:
                        if (doNotifyVictimDeadCancellation)
                        {
                            SendNotification(
                                1985121,
                                onLocalHitCancelledAsVictimDeadMessage,
                                NewbieUtils.GetPlayerName(syncer.VictimId)
                            );
                        }

                        break;
                }

                return;
            }

            if (doNotifyNonLocalHitCancellation)
            {
                switch (syncer.ResultContext)
                {
                    case SyncResultContext.AttackerAlreadyDead:
                        if (doNotifyAttackerDeadCancellation)
                        {
                            SendNotification(
                                1985122,
                                onNonLocalHitCancelledAsAttackerDeadMessage,
                                NewbieUtils.GetPlayerName(syncer.AttackerId),
                                NewbieUtils.GetPlayerName(syncer.VictimId)
                            );
                        }

                        break;
                    case SyncResultContext.VictimAlreadyDead:
                        if (doNotifyVictimDeadCancellation)
                        {
                            SendNotification(
                                1985123,
                                onNonLocalHitCancelledAsVictimDeadMessage,
                                NewbieUtils.GetPlayerName(syncer.AttackerId),
                                NewbieUtils.GetPlayerName(syncer.VictimId)
                            );
                        }

                        break;
                }
            }
        }

        private void SendNotification(int id, [CanBeNull] TranslatableMessage message, params object[] values)
        {
            if (message == null || notification == null) return;
            notification.Show(NotificationLevel.Warn, string.Format(message.Message, values), notificationDuration, id);
        }
    }
}