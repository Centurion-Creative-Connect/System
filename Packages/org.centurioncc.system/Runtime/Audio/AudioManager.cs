using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Audio
{
    [SelectionBase] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioManager : UdonSharpBehaviour
    {
        [SerializeField]
        private AudioSource[] sources;
        private int _lastPickedSource;

        [PublicAPI]
        public void PlayAudioAtTransform(AudioClip clip, Transform position, float volume, float pitch = 1)
        {
            if (clip == null || position == null)
            {
                Debug.LogError("[AudioManager] Illegal Argument: clip or position is null. dropping");
                return;
            }

            if (AudioHelper.CanHearAudioAtPosition(position.position))
            {
                var source = GetAudioSource();

                var sourceTransform = source.transform;

                sourceTransform.SetParent(position);
                sourceTransform.localPosition = Vector3.zero;

                AudioHelper.PlayAudioSource(source, clip, volume, pitch);
            }
            else
            {
                Debug.Log(
                    $"[AudioManager] dropping audio:{clip.name} playing at {position.position} because it's too far away");
            }
        }

        [PublicAPI]
        public void PlayAudioAtPosition(AudioClip clip, Vector3 position, float volume, float pitch = 1)
        {
            if (clip == null)
            {
                Debug.LogError("[AudioSource] Illegal Argument: clip is null. dropping");
                return;
            }

            if (AudioHelper.CanHearAudioAtPosition(position))
            {
                var source = GetAudioSource();

                source.transform.position = position;
                AudioHelper.PlayAudioSource(source, clip, volume, pitch);
            }
            else
            {
                Debug.Log(
                    $"[AudioManager] dropping audio:{clip.name} playing at {position} because it's too far away");
            }
        }

        private AudioSource GetAudioSource()
        {
            if (++_lastPickedSource >= sources.Length)
                _lastPickedSource = 0;
            return sources[_lastPickedSource];
        }
    }

    public static class AudioHelper
    {
        public static void PlayAudioSource(AudioSource source, AudioClip clip, float vol, float pit)
        {
            source.clip = clip;
            source.pitch = pit;
            source.volume = vol;
            source.Play();
        }

        public static bool CanHearAudioAtPosition(Vector3 pos)
        {
            return Vector3.Distance(Networking.LocalPlayer.GetPosition(), pos) < 30F;
        }
    }
}