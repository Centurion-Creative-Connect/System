using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private AudioClip[] clips;
        [Header("Volume")]
        [SerializeField, Range(0F, 1F)]
        private float maxVolume;
        [SerializeField, Range(0F, 1F)]
        private float minVolume;
        [Header("Pitch")]
        [SerializeField, Range(0F, 2F)]
        private float maxPitch;
        [SerializeField, Range(0F, 2F)]
        private float minPitch;

        private bool _hasCache;
        private bool _cachedIsClipsNull;
        private int _cachedClipsLength;

        public AudioClip Clip
        {
            get
            {
                if (!_hasCache)
                {
                    _cachedIsClipsNull = clips.Length == 0 || clips == null;
                    _cachedClipsLength = clips.Length;
                    _hasCache = true;
                }

                return _cachedIsClipsNull ? null : clips[Random.Range(0, _cachedClipsLength - 1)];
            }
        }
        public float Volume => Random.Range(minVolume, maxVolume);
        public float Pitch => Random.Range(minPitch, maxPitch);
    }
}