using UdonSharp;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunHaptic : UdonSharpBehaviour
    {
        public float duration;
        public float amplitude;
        public float frequency;

        public void Play()
        {
            _PlayInHand(VRC_Pickup.PickupHand.Left);
            _PlayInHand(VRC_Pickup.PickupHand.Right);
        }

        public void PlayLeftHand()
        {
            _PlayInHand(VRC_Pickup.PickupHand.Left);
        }

        public void PlayRightHand()
        {
            _PlayInHand(VRC_Pickup.PickupHand.Right);
        }

        public void PlayBothHand()
        {
            _PlayInHand(VRC_Pickup.PickupHand.Right);
            _PlayInHand(VRC_Pickup.PickupHand.Left);
        }

        public void PlayInHand(VRC_Pickup.PickupHand hand)
        {
            _PlayInHand(hand);
        }

        private void _PlayInHand(VRC_Pickup.PickupHand hand)
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
                return;
            Networking.LocalPlayer.PlayHapticEventInHand(hand, duration, amplitude, frequency);
        }
    }
}