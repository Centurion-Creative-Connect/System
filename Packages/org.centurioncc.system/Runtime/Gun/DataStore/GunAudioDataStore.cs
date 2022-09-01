using CenturionCC.System.Audio;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunAudioDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private CollisionAudioStore collisionAudio;

        [Header("Shooting")]
        [SerializeField]
        private AudioDataStore shootingAudio;
        [SerializeField]
        private AudioDataStore emptyShootingAudio;

        [Header("Cocking")]
        [SerializeField]
        private AudioDataStore cockingTwistAudio;
        [SerializeField]
        private AudioDataStore cockingPullAudio;
        [SerializeField]
        private AudioDataStore cockingReleaseAudio;

        public CollisionAudioStore Collision => collisionAudio;
        public AudioDataStore Shooting => shootingAudio;
        public AudioDataStore EmptyShooting => emptyShootingAudio;
        public AudioDataStore CockingTwist => cockingTwistAudio;
        public AudioDataStore CockingPull => cockingPullAudio;
        public AudioDataStore CockingRelease => cockingReleaseAudio;
    }
}