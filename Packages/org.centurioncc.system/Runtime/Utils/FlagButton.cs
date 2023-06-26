using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Invoker;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(AudioSource), typeof(SendVariableSyncEvent))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FlagButton : PickupEventSenderCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PrintableBase logger;
        [SerializeField] [HideInInspector] [NewbieInject]
        private DamageDataResolver resolver;
        [SerializeField]
        private float resolverWaitDuration = 0.1F;
        public float delay = 2F;
        private AudioSource _audioSource;
        private DateTime _lastInteractTime;
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
            _lastInteractTime = Networking.GetNetworkDateTime();
            SendCustomEventDelayedSeconds(nameof(_TryPlayFlagAudio), resolverWaitDuration);
        }

        [PublicAPI]
        public void _TryPlayFlagAudio()
        {
            var diedTime = resolver.GetAssumedDiedTime(Networking.LocalPlayer.playerId);
            if (resolver.IsInInvincibleDuration(_lastInteractTime, diedTime))
            {
                logger.LogWarn(
                    $"[Flag] flag button was pressed at {transform.parent.name} but i'm in invincible duration!");
                return;
            }

            if (_lastPlayedTime + delay > Time.timeSinceLevelLoad)
            {
                logger.LogWarn($"[Flag] flag button was pressed at {transform.parent.name} but i'm in delay time!");
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