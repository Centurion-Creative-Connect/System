using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Audio
{
    [SelectionBase] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioManager : UdonSharpBehaviour
    {
        private const string Prefix = "[AudioManager] ";

        private const string AudioDataStoreNullError =
            Prefix + "AudioDataStore is null. dropping!";

        private const string ClipOrPositionNullError =
            Prefix + "Clip or Position is null. dropping!";

        private const string DroppingAudioBecauseTooFar =
            Prefix + "Audio clip `{0}` playing at `{1}` ({2}m) is too far away. dropping!";

        [SerializeField]
        private AudioSource[] sources;

        private int _lastPickedSource;

        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }

        [PublicAPI]
        public void PlayAudioAtTransform([CanBeNull] AudioClip clip, [CanBeNull] Transform t, Vector3 offset,
            float volume, float pitch = 1F, float dopplerLevel = 1F, float spread = 0F,
            float minDistance = 0.5F, float maxDistance = 25F, int priority = 0)
        {
            if (clip == null || t == null)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.LogError(ClipOrPositionNullError);
#endif
                return;
            }

            var dist = Vector3.Distance(t.position, _localPlayer.GetPosition());
            if (dist > maxDistance + 10F)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.Log(string.Format(DroppingAudioBecauseTooFar, clip.name, t.position, dist));
#endif
                return;
            }

            var source = GetAudioSource();
            var sourceTransform = source.transform;

            sourceTransform.SetParent(t);
            sourceTransform.localPosition = offset;

            AudioHelper.PlayAudioSource(source, clip, volume, pitch, dopplerLevel, spread,
                minDistance, maxDistance, priority);
        }

        [PublicAPI]
        public void PlayAudioAtTransform([CanBeNull] AudioClip clip, [CanBeNull] Transform t,
            float volume, float pitch = 1F, float dopplerLevel = 1F, float spread = 0F,
            float minDistance = 0.5F, float maxDistance = 25F, int priority = 0)
        {
            PlayAudioAtTransform(clip, t, Vector3.zero, volume, pitch, dopplerLevel, spread,
                minDistance, maxDistance, priority);
        }

        [PublicAPI]
        public void PlayAudioAtPosition([CanBeNull] AudioClip clip, Vector3 position, float volume, float pitch = 1,
            float dopplerLevel = 1F, float spread = 0F, float minDistance = 0.5F, float maxDistance = 25F,
            int priority = 0)
        {
            if (clip == null)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.LogError(ClipOrPositionNullError);
#endif
                return;
            }

            var dist = Vector3.Distance(position, _localPlayer.GetPosition());
            if (dist > maxDistance + 10F)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.Log(string.Format(DroppingAudioBecauseTooFar, clip.name, position, dist));
#endif
                return;
            }

            var source = GetAudioSource();
            var sourceTransform = source.transform;

            sourceTransform.SetParent(transform);
            sourceTransform.position = position;

            AudioHelper.PlayAudioSource(source, clip, volume, pitch, dopplerLevel, spread,
                minDistance, maxDistance, priority);
        }

        [PublicAPI]
        public void PlayAudioAtTransform([CanBeNull] AudioDataStore dataStore, [CanBeNull] Transform t)
        {
            if (dataStore == null)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.LogError(AudioDataStoreNullError);
#endif
                return;
            }

            PlayAudioAtTransform(dataStore.Clip, t, dataStore.Volume, dataStore.Pitch,
                dataStore.DopplerLevel, dataStore.Spread, dataStore.MinDistance, dataStore.MaxDistance,
                dataStore.Priority);
        }

        [PublicAPI]
        public void PlayAudioAtTransform([CanBeNull] AudioDataStore dataStore, [CanBeNull] Transform t, Vector3 offset)
        {
            if (dataStore == null)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.LogError(AudioDataStoreNullError);
#endif
                return;
            }

            PlayAudioAtTransform(dataStore.Clip, t, offset, dataStore.Volume, dataStore.Pitch,
                dataStore.DopplerLevel, dataStore.Spread, dataStore.MinDistance, dataStore.MaxDistance,
                dataStore.Priority);
        }

        [PublicAPI]
        public void PlayAudioAtPosition([CanBeNull] AudioDataStore dataStore, Vector3 position)
        {
            if (dataStore == null)
            {
#if CENTURIONSYSTEM_VERBOSE_LOGGING || CENTURIONSYSTEM_AUDIO_LOGGING
                Debug.LogError(AudioDataStoreNullError);
#endif
                return;
            }

            PlayAudioAtPosition(dataStore.Clip, position, dataStore.Volume, dataStore.Pitch,
                dataStore.DopplerLevel, dataStore.Spread, dataStore.MinDistance, dataStore.MaxDistance,
                dataStore.Priority);
        }

        private AudioSource GetAudioSource()
        {
            if (++_lastPickedSource >= sources.Length)
                _lastPickedSource = 0;
            var source = sources[_lastPickedSource];
            return source.isPlaying ? FindUnusedOrLowestPrioritySource() : source;
        }

        private AudioSource FindUnusedOrLowestPrioritySource()
        {
            // Use last picked source as initial source so we can re-use oldest among same priority
            var lowestPrioritySource = sources[_lastPickedSource];
            foreach (var source in sources)
            {
                // We can freely use its source
                if (!source.isPlaying)
                {
                    return source;
                }

                // We search for the lowest priority source
                if (lowestPrioritySource.priority > source.priority)
                {
                    lowestPrioritySource = source;
                }
            }

            return lowestPrioritySource;
        }
    }

    public static class AudioHelper
    {
        private const float DelayMultiplier = 3F;

        public static void PlayAudioSource(AudioSource source, AudioClip clip, float vol, float pit,
            float doppler, float spread, float minDist, float maxDist, int priority = 0)
        {
            source.clip = clip;
            source.pitch = pit;
            source.volume = vol;
            source.dopplerLevel = doppler;
            source.spread = spread;
            source.minDistance = minDist;
            source.maxDistance = maxDist;
            source.priority = priority;

            // Delaying needed to stop AudioSource's cracking sound by re-using them
            source.PlayDelayed(Time.inFixedTimeStep ? 0.1F : Time.smoothDeltaTime * DelayMultiplier);
        }
    }
}