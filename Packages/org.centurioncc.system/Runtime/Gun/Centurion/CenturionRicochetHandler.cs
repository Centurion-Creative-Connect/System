using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gun.Centurion
{
    public class CenturionRicochetHandler : RicochetHandlerBase
    {
        [SerializeField]
        private AudioDataStore ricochetAudioData;

        [SerializeField]
        private float magnitudeVolumeMultiplier = .65f;

        [SerializeField]
        private float minCollisionMagnitude = 0.1f;

        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;

        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }

        public override void OnRicochet(ProjectileBase projectileBase, Collision collision)
        {
            var contact = collision.GetContact(0);
            if (Vector3.Distance(contact.point, _localPlayer.GetPosition()) > ricochetAudioData.MaxDistance)
                return;

            if (collision.impulse.magnitude < minCollisionMagnitude)
                return;

            var objMarker = collision.gameObject.GetComponent<ObjectMarkerBase>();
            if (objMarker != null && objMarker.Tags.ContainsString("NoRicochetAudio"))
                return;

            audioManager.PlayAudioAtPosition(
                ricochetAudioData.Clip,
                contact.point,
                ricochetAudioData.Volume * Mathf.Clamp01(collision.impulse.magnitude * magnitudeVolumeMultiplier),
                ricochetAudioData.Pitch,
                ricochetAudioData.DopplerLevel,
                ricochetAudioData.Spread,
                ricochetAudioData.MinDistance,
                ricochetAudioData.MaxDistance
            );
        }
    }
}
