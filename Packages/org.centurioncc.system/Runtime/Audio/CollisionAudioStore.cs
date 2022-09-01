using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CollisionAudioStore : UdonSharpBehaviour
    {
        [SerializeField]
        private AudioDataStore fallbackAudio;
        [SerializeField]
        private AudioDataStore woodAudio;
        [SerializeField]
        private AudioDataStore ironAudio;
        [SerializeField]
        private AudioDataStore clothAudio;

        public AudioDataStore FallbackAudio => fallbackAudio;
        public AudioDataStore WoodAudio => woodAudio;
        public AudioDataStore IronAudio => ironAudio;
        public AudioDataStore ClothAudio => clothAudio;
    }
}