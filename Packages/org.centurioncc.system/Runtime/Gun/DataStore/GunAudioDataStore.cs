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

        [Header("Reload")]
        [SerializeField]
        private AudioDataStore magazineReleasedAudio;
        [SerializeField]
        private Vector3 magazineReleasedOffset;
        [SerializeField]
        private AudioDataStore magazineInsertedAudio;
        [SerializeField]
        private Vector3 magazineInsertedOffset;


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
        public AudioDataStore MagazineReleased => magazineReleasedAudio;
        public Vector3 MagazineReleasedOffset => magazineReleasedOffset;
        public AudioDataStore MagazineInserted => magazineInsertedAudio;
        public Vector3 MagazineInsertedOffset => magazineInsertedOffset;

        // NOTE: for compatibility
        public bool UseSecondTwistAudio => useSecondTwistAudio;
    }
}
