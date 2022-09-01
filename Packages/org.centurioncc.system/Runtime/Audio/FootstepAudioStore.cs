using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FootstepAudioStore : UdonSharpBehaviour
    {
        [SerializeField]
        private AudioDataStore fallbackAudio;
        [SerializeField]
        private AudioDataStore slowFallbackAudio;
        [SerializeField]
        private AudioDataStore groundAudio;
        [SerializeField]
        private AudioDataStore slowGroundAudio;
        [SerializeField]
        private AudioDataStore woodAudio;
        [SerializeField]
        private AudioDataStore slowWoodAudio;

        public AudioDataStore FallbackAudio => fallbackAudio;
        public AudioDataStore SlowFallbackAudio => slowFallbackAudio;
        public AudioDataStore GroundAudio => groundAudio;
        public AudioDataStore SlowGroundAudio => slowGroundAudio;
        public AudioDataStore WoodAudio => woodAudio;
        public AudioDataStore SlowWoodAudio => slowWoodAudio;
    }
}