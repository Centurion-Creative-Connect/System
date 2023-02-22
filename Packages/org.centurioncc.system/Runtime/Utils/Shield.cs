using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Shield : UdonSharpBehaviour
    {
        [SerializeField]
        private VRCPickup pickup;
        [SerializeField]
        private Transform pickupReference;
        [SerializeField]
        private Transform leftHandedReference;
        [SerializeField]
        private Transform rightHandedReference;

        private bool isHeldLocally;

        private void OnCollisionEnter(Collision collision)
        {
            if (isHeldLocally)
                Networking.LocalPlayer.PlayHapticEventInHand(pickup.currentHand, 0.1F, 2F, 1F);
        }

        private void OnCollisionStay(Collision collisionInfo)
        {
            if (isHeldLocally)
                Networking.LocalPlayer.PlayHapticEventInHand(pickup.currentHand, .2F, .02F, .1F);
        }

        public override void OnPickup()
        {
            var refTransform = pickup.currentHand == VRC_Pickup.PickupHand.Left
                ? leftHandedReference
                : rightHandedReference;

            pickupReference.localPosition = refTransform.localPosition;
            pickupReference.localRotation = refTransform.localRotation;

            isHeldLocally = true;
            pickup.pickupable = false;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override void OnDrop()
        {
            isHeldLocally = false;
            pickup.pickupable = true;
        }
    }
}