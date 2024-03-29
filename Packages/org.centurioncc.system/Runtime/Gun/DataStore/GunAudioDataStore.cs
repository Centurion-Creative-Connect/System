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
        private Vector3 shootingOffset;
        [SerializeField]
        private AudioDataStore emptyShootingAudio;
        [SerializeField]
        private Vector3 emptyShootingOffset;

        [Header("Cocking")]
        [SerializeField]
        private AudioDataStore cockingTwistAudio;
        [SerializeField]
        private Vector3 cockingTwistOffset;
        [SerializeField]
        private AudioDataStore cockingSecondTwistAudio;
        [SerializeField]
        private Vector3 cockingSecondTwistOffset;
        [SerializeField]
        private AudioDataStore cockingPullAudio;
        [SerializeField]
        private Vector3 cockingPullOffset;
        [SerializeField]
        private AudioDataStore cockingReleaseAudio;
        [SerializeField]
        private Vector3 cockingReleaseOffset;

        [Header("Options")]
        [SerializeField]
        private bool useSecondTwistAudio = false;

        public CollisionAudioStore Collision => collisionAudio;
        public AudioDataStore Shooting => shootingAudio;
        public Vector3 ShootingOffset => shootingOffset;
        public AudioDataStore EmptyShooting => emptyShootingAudio;
        public Vector3 EmptyShootingOffset => emptyShootingOffset;
        public AudioDataStore CockingTwist => cockingTwistAudio;
        public Vector3 CockingTwistOffset => cockingTwistOffset;
        public AudioDataStore CockingSecondTwist => cockingSecondTwistAudio;
        public Vector3 CockingSecondTwistOffset => cockingSecondTwistOffset;
        public AudioDataStore CockingPull => cockingPullAudio;
        public Vector3 CockingPullOffset => cockingPullOffset;
        public AudioDataStore CockingRelease => cockingReleaseAudio;
        public Vector3 CockingReleaseOffset => cockingReleaseOffset;

        // NOTE: for compatibility
        public bool UseSecondTwistAudio => useSecondTwistAudio;
    }
}