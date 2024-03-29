using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Shield : ObjectMarkerBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        protected ShieldManager shieldManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        protected PlayerController playerController;
        [SerializeField]
        private VRCPickup pickup;
        [SerializeField]
        private Transform pickupReference;
        [SerializeField]
        private Transform leftHandedReference;
        [SerializeField]
        private Transform rightHandedReference;
        [SerializeField]
        private bool canShootWhileCarrying = true;
        [SerializeField]
        private bool dropShieldOnHit;

        protected DropContext context;
        protected HandType currentRef;
        protected bool isHeldLocally;

        public virtual bool CanShootWhileCarrying => canShootWhileCarrying;

        public virtual bool DropShieldOnHit => dropShieldOnHit;

        public virtual VRCPickup VrcPickup => pickup;

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
            context = DropContext.UserInput;

            // Process ObjectMarker logic before anything
            playerController.AddHoldingObject(this);

            var invokeResult = shieldManager.Invoke_OnShieldPickup(this);
            if (!invokeResult)
            {
                context = DropContext.PickupCancelled;
                pickup.Drop();
                return;
            }

            var refTransform = pickup.currentHand == VRC_Pickup.PickupHand.Left
                ? leftHandedReference
                : rightHandedReference;

            SetPickupRef(refTransform);
            currentRef = pickup.currentHand == VRC_Pickup.PickupHand.Left ? HandType.LEFT : HandType.RIGHT;

            isHeldLocally = true;
            pickup.pickupable = false;

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override void OnPickupUseDown()
        {
            currentRef = currentRef == HandType.LEFT ? HandType.RIGHT : HandType.LEFT;
            SetPickupRef(currentRef == HandType.LEFT ? leftHandedReference : rightHandedReference);
        }

        public override void OnDrop()
        {
            playerController.RemoveHoldingObject(this);

            isHeldLocally = false;
            pickup.pickupable = true;

            shieldManager.Invoke_OnShieldDrop(this, context);
        }

        public void DropByHit()
        {
            context = DropContext.Hit;
            pickup.Drop();
        }

        private void SetPickupRef(Transform t)
        {
            pickupReference.localPosition = t.localPosition;
            pickupReference.localRotation = t.localRotation;
        }

        #region ObjectMarkerBase

        [Header("Object Marker Properties")]
        [SerializeField]
        private ObjectType objectType;
        [SerializeField]
        private float objectWeight;
        [SerializeField]
        private string[] tags = { "NoCollisionAudio" };

        public override ObjectType ObjectType => objectType;
        public override float ObjectWeight => objectWeight;
        public override float WalkingSpeedMultiplier => 1;
        public override string[] Tags => tags;

        #endregion
    }
}