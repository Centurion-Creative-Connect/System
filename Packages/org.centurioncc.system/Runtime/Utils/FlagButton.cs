using DerpyNewbie.Common;
using DerpyNewbie.Common.Invoker;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(AudioSource), typeof(SendVariableSyncEvent))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FlagButton : PickupEventSenderCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GameManager gameManager;
        public float delay = 2F;
        private AudioSource _audioSource;
        private float _lastPlayedTime;
        private SendVariableSyncEvent _variableEvent;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _variableEvent = GetComponent<SendVariableSyncEvent>();
            if (gameManager == null)
                gameManager = CenturionSystemReference.GetGameManager();

            _variableEvent.SetCallback(this, nameof(PlayFlagAudio));
        }

        public override void Interact()
        {
            _TryPlayFlagAudio();
        }

        public override void OnPickupUseDownRelayed()
        {
            _TryPlayFlagAudio();
        }

        [PublicAPI]
        public void _TryPlayFlagAudio()
        {
            if (gameManager.IsInAntiZombieTime())
            {
                gameManager.logger.LogWarn(
                    $"[Flag] flag button was pressed at {transform.parent.name} but i'm in anti-zombie time!");
                return;
            }

            if (_lastPlayedTime + delay > Time.timeSinceLevelLoad)
            {
                gameManager.logger.LogWarn(
                    $"[Flag] flag button was pressed at {transform.parent.name} but i'm in delay time!");
                return;
            }

            _variableEvent.Invoke();
        }

        public void PlayFlagAudio()
        {
            Internal_PlayFlagAudio();
        }

        private void Internal_PlayFlagAudio()
        {
            gameManager.logger.Log($"[Flag] flag at {transform.parent.name} is now playing!");
            _audioSource.Play();
            _lastPlayedTime = Time.timeSinceLevelLoad;
        }
    }
}