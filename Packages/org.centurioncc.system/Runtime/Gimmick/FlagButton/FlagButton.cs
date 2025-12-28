using CenturionCC.System.Player;
using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Invoker;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.FlagButton
{
    [RequireComponent(typeof(AudioSource), typeof(SendVariableSyncEvent))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FlagButton : PickupEventSenderCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PrintableBase logger;

        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField]
        private float resolverWaitDuration = 0.1F;

        public float delay = 2F;
        private AudioSource _audioSource;
        private float _lastPlayedTime;
        private SendVariableSyncEvent _variableEvent;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _variableEvent = GetComponent<SendVariableSyncEvent>();
            _variableEvent.SetCallback(this, nameof(PlayFlagAudio));
        }

        public override void Interact()
        {
            _DelayedExecute();
        }

        public override void OnPickupUseDownRelayed()
        {
            _DelayedExecute();
        }

        private void _DelayedExecute()
        {
            if (_CheckCooldown()) return;

            SendCustomEventDelayedSeconds(nameof(_TryPlayFlagAudio), resolverWaitDuration);
        }

        private bool _CheckCooldown()
        {
            if (_lastPlayedTime + delay > Time.timeSinceLevelLoad)
            {
                logger.LogWarn($"[Flag] flag button was pressed at {transform.parent.name} but i'm in delay time!");
                notification.ShowInfo($"フラッグのクールダウン中: 残り{delay - (Time.timeSinceLevelLoad - _lastPlayedTime):F1}秒", 5F,
                    1058150);
                return true;
            }

            return false;
        }

        [PublicAPI]
        public void _TryPlayFlagAudio()
        {
            var localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer == null)
            {
                notification.ShowWarn("ゲームに入っていない状態でフラッグを鳴らすことはできません");
                return;
            }

            if (localPlayer.IsDead)
            {
                // TODO: make it translatable message
                notification.ShowWarn("ヒット中にフラッグを鳴らすことはできません");
                return;
            }

            if (_CheckCooldown())
            {
                return;
            }

            _variableEvent.Invoke();
        }

        public void PlayFlagAudio()
        {
            logger.Log($"[Flag] flag at {transform.parent.name} is now playing!");
            _audioSource.Play();
            _lastPlayedTime = Time.timeSinceLevelLoad;
        }
    }
}
