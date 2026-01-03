using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gun.Centurion
{
    public class CenturionRicochetHandler : RicochetHandlerBase
    {
        private const float RicochetVolumeSpeedCoefficient = 3F;
        private const float MinCollisionMagnitude = 0.05F;
        private const float MaxAudioDistance = 10F;

        [SerializeField]
        private AudioDataStore ricochetAudioData;

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
            if (Vector3.Distance(contact.point, _localPlayer.GetPosition()) > MaxAudioDistance)
                return;

            if (collision.impulse.magnitude < MinCollisionMagnitude)
                return;

            var objMarker = collision.gameObject.GetComponent<ObjectMarkerBase>();
            if (objMarker != null && objMarker.Tags.ContainsString("NoRicochetAudio"))
                return;

            audioManager.PlayAudioAtPosition(
                ricochetAudioData.Clip,
                contact.point,
                ricochetAudioData.Volume * Mathf.Clamp01(collision.impulse.magnitude / RicochetVolumeSpeedCoefficient),
                ricochetAudioData.Pitch,
                ricochetAudioData.DopplerLevel,
                ricochetAudioData.Spread,
                ricochetAudioData.MinDistance,
                ricochetAudioData.MaxDistance
            );
        }
    }
}
