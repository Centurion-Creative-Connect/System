using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gimmick.Shield
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Shield : ObjectMarkerBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        protected PlayerManagerBase playerManager;

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
        protected int teamId;

        public virtual bool CanShootWhileCarrying => canShootWhileCarrying;

        public virtual bool DropShieldOnHit => dropShieldOnHit;

        public virtual VRCPickup VrcPickup => pickup;

        public virtual int TeamId => teamId;

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

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateTeamId));
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

            if (context == DropContext.UserInput) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateTeamId));

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

        public void UpdateTeamId()
        {
            PlayerBase playerBase = null;
            // If pickup was held, get from current pickup player
            if (pickup.IsHeld && Utilities.IsValid(pickup.currentPlayer))
            {
                playerBase = playerManager.GetPlayerById(pickup.currentPlayer.playerId);
            }

            // If pickup was not held, get from last owner
            if (playerBase == null)
            {
                playerBase = playerManager.GetPlayerById(Networking.GetOwner(gameObject).playerId);
            }

            // If PlayerBase was not found for any cases, do not update team id
            if (playerBase == null)
            {
                return;
            }

            // Staff should not be able to occupy a shield as a team
            if (playerManager.IsStaffTeamId(playerBase.TeamId))
            {
                teamId = 0;
                return;
            }

            teamId = playerBase.TeamId;
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
