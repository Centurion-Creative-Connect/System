using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlayer : UdonSharpBehaviour
    {
        public AudioClip[] clips;
        public int currentClipIndex;

        public float endDelay = 1F;
        public float startDelay;

        private AudioSource _audioSource;
        private bool _canInterrupt;
        private AudioClip _currentClip;
        private bool _isPlaying;
        private float _timeSinceEnd;

        private void Start()
        {
            if (currentClipIndex < 0 || clips.Length <= currentClipIndex)
                currentClipIndex = 0;

            _currentClip = clips[currentClipIndex];
            _audioSource = GetComponent<AudioSource>();
        }

        private void FixedUpdate()
        {
            if (_isPlaying && !_audioSource.isPlaying)
            {
                _timeSinceEnd += Time.deltaTime;
                if (_timeSinceEnd > endDelay)
                    _isPlaying = false;
            }
        }

        public void Play(bool allowInterrupt)
        {
            Debug.Log($"AudioPlayerPlay{currentClipIndex}");
            if (!_canInterrupt && _isPlaying)
                return;

            _canInterrupt = allowInterrupt;
            _timeSinceEnd = 0F;
            _isPlaying = true;
            _audioSource.clip = _currentClip;
            _audioSource.PlayDelayed(startDelay);
        }

        public void PlayGlobal(bool allowInterrupt)
        {
            Debug.Log("AudioPlayerPlayGlobal");
            if (allowInterrupt)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayAllowInterrupt));
            else
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayDisallowInterrupt));
        }

        public void PlayAllowInterrupt()
        {
            Play(true);
        }

        public void PlayDisallowInterrupt()
        {
            Play(false);
        }

        public void ChangeClip(int i)
        {
            if (i < 0 || clips.Length <= i)
                return;

            currentClipIndex = i;
            _currentClip = clips[currentClipIndex];
        }

        public AudioClip GetCurrentClip()
        {
            return _currentClip;
        }

        public int GetCurrentClipIndex()
        {
            return currentClipIndex;
        }
    }
}