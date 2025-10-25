using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Gun : GunBase
    {
        private const float FreeHandPickupProximity = .1F;
        private const float SwapHandDisallowPickupProximity = 0.15F;
        private const float DesktopPickupProximity = 2F;
        private const float DisallowPickupFromBelowRange = -0.1F;

        private int _lastShotCount;

        private float _nextMainHandlePickupableTime;
        private float _nextSubHandlePickupableTime;
        private long _shotTime;

        #region OverridenProperties
        public override GunVariantDataStore VariantData => variantData;
        #endregion

        protected override void Start()
        {
            base.Start();

            updateManager.SubscribeUpdate(this);
            updateManager.SubscribeSlowUpdate(this);
        }

        #region SerializeFields
        [SerializeField] protected GunVariantDataStore variantData;

        [SerializeField] [NewbieInject]
        protected UpdateManager updateManager;
        #endregion

        #region Internals
        protected void Internal_UpdateHandlePickupable()
        {
            // Only do complex update while picked up
            if (IsLocal && IsPickedUp)
            {
                var localPlayer = Networking.LocalPlayer;
                var leftHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                var rightHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                var currentTime = Time.time;

                MainHandle.SetPickupable(currentTime > _nextMainHandlePickupableTime &&
                                         GetPickupProximity(MainHandle, leftHand, rightHand, IsVR, false));
                if (SubHandle)
                    SubHandle.SetPickupable(CanBeTwoHanded && currentTime > _nextSubHandlePickupableTime &&
                                            GetPickupProximity(SubHandle, leftHand, rightHand, IsVR, false));
            }
            else
            {
                var localInVR = Networking.LocalPlayer.IsUserInVR();
                MainHandle.Proximity = localInVR ? FreeHandPickupProximity : DesktopPickupProximity;
                MainHandle.AdjustScaleForDesktop(localInVR);
                MainHandle.SetPickupable(true);
                if (SubHandle)
                    SubHandle.SetPickupable(CanBeTwoHanded && localInVR);
            }
        }

        protected bool GetPickupProximity(GunHandle handle, Vector3 leftHandPos, Vector3 rightHandPos, bool inVR,
                                          bool disallowFromBelow)
        {
            var handlePos = handle.transform.position;

            // We only want to interrupt proximity when the user is in VR mode
            if (!inVR)
                return true;

            if (handle.IsPickedUp)
            {
                // If already picked up, check the opposite hand's distance is not too close for being able to switch
                var freeHandDistance = Vector3.Distance(
                    handle.CurrentHand == VRC_Pickup.PickupHand.Left ? rightHandPos : leftHandPos,
                    handlePos
                );
                var result = freeHandDistance > FreeHandPickupProximity;
                if (result && !handle.IsPickupable)
                    Networking.LocalPlayer.PlayHapticEventInHand(
                        handle.CurrentHand == VRC_Pickup.PickupHand.Left
                            ? VRC_Pickup.PickupHand.Right
                            : VRC_Pickup.PickupHand.Left,
                        0.4F,
                        0.2F,
                        0.2F);
                return result;
            }

            // If not yet picked up, check the closest hand's distance to ensure that a handle is possible to pick up
            var lhDistance = Vector3.Distance(leftHandPos, handlePos);
            var rhDistance = Vector3.Distance(rightHandPos, handlePos);
            var closestHandDistance = Mathf.Min(lhDistance, rhDistance);

            var up = Target.up;
            var dot = Mathf.Max(Vector3.Dot(up, (leftHandPos - handlePos).normalized),
                Vector3.Dot(up, (rightHandPos - handlePos).normalized));

            return closestHandDistance < SwapHandDisallowPickupProximity &&
                   (!disallowFromBelow || dot > DisallowPickupFromBelowRange);
        }
        #endregion
    }
}
