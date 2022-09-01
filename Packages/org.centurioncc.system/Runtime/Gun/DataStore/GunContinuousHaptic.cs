using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunContinuousHaptic : UdonSharpBehaviour
    {
        [Header("Frequency")]
        [Range(0, 1)]
        public float maxFrequency;
        [Range(0, 1)]
        public float minFrequency;

        [Header("Amplitude")]
        [Range(0, 1)]
        public float maxAmplitude;
        [Range(0, 1)]
        public float minAmplitude;

        public void PlayHapticOnHand(VRC_Pickup.PickupHand hand, float progressNormalized)
        {
            var expectedFreq = Mathf.Lerp(minFrequency, maxFrequency, progressNormalized);
            var expectedAmp = Mathf.Lerp(minAmplitude, maxAmplitude, progressNormalized);
            Networking.LocalPlayer.PlayHapticEventInHand(hand, .01F, expectedAmp, expectedFreq);
        }
    }
}