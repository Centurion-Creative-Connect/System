using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private AudioClip[] clips;

        [Header("Volume"), SerializeField, Range(0F, 1F)]
        private float maxVolume = 1;

        [SerializeField, Range(0F, 1F)]
        private float minVolume = 1;

        [Header("Pitch")]
        [SerializeField, Range(0F, 2F)]
        private float maxPitch = 1;

        [SerializeField, Range(0F, 2F)]
        private float minPitch = 1;

        [Header("3D Settings")]
        [SerializeField, Range(0F, 5F)]
        private float dopplerLevel = 1;

        [SerializeField, Range(0F, 360F)]
        private float spread = 0F;

        [SerializeField]
        private float minDistance = 0.5F;

        [SerializeField]
        private float maxDistance = 25F;

        [SerializeField]
        private int priority = 0;

        private int _cachedClipsLength;
        private bool _cachedIsClipsNull;

        private bool _hasCache;

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

                return _cachedIsClipsNull ? null : clips[Random.Range(0, _cachedClipsLength)];
            }
        }

        public float Volume => Random.Range(minVolume, maxVolume);
        public float Pitch => Random.Range(minPitch, maxPitch);
        public float DopplerLevel => dopplerLevel;
        public float Spread => spread;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        public int Priority => priority;
    }
}