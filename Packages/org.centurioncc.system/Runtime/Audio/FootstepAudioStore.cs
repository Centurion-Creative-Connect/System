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

        [SerializeField]
        private AudioDataStore ironAudio;
        [SerializeField]
        private AudioDataStore slowIronAudio;

        public AudioDataStore FallbackAudio => fallbackAudio;
        public AudioDataStore SlowFallbackAudio => slowFallbackAudio != null ? slowFallbackAudio : FallbackAudio;

        public AudioDataStore GroundAudio => groundAudio != null ? groundAudio : FallbackAudio;
        public AudioDataStore SlowGroundAudio => slowGroundAudio != null ? slowGroundAudio : SlowFallbackAudio;

        public AudioDataStore WoodAudio => woodAudio != null ? woodAudio : FallbackAudio;
        public AudioDataStore SlowWoodAudio => slowWoodAudio != null ? slowWoodAudio : SlowFallbackAudio;

        public AudioDataStore IronAudio => ironAudio != null ? ironAudio : FallbackAudio;
        public AudioDataStore SlowIronAudio => slowIronAudio != null ? slowIronAudio : SlowFallbackAudio;
    }
}