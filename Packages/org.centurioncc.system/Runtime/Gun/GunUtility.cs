using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    public static class GunUtility
    {
        public const string TriggerProgressParamName = "TriggerProgress";
        public const string CockingProgressParamName = "CockingProgress";
        public const string CockingTwistParamName = "CockingTwist";
        public const string IsPickedUpLocalParamName = "IsPickedUp";
        public const string IsPickedUpGlobalParamName = "IsPickedUpGlobal";
        public const string IsInSafeZoneParamName = "IsInSafeZone";
        public const string IsInWallParamName = "IsInWall";
        public const string IsLocalParamName = "IsLocal";
        public const string IsVRParamName = "IsVR";
        public const string HasBulletParamName = "HasBullet";
        public const string HasCockedParamName = "HasCocked";
        public const string IsShootingParamName = "IsShooting";
        public const string IsShootingEmptyParamName = "IsShootingEmpty";
        public const string SelectorTypeParamName = "SelectorType";
        public const string StateParamName = "State";
        public const string TriggerStateParamName = "Trigger";

        public static FireMode CycleFireMode(FireMode fireMode, FireMode[] allowedFireModes)
        {
            if (allowedFireModes == null || allowedFireModes.Length == 0)
                return fireMode;

            var index = allowedFireModes.FindItem(fireMode);
            if (index == -1 || index + 1 >= allowedFireModes.Length)
                return allowedFireModes[0];

            return allowedFireModes[index + 1];
        }

        public static bool IsValidFireMode(FireMode mode, FireMode[] allowedFireModes)
        {
            foreach (var fireMode in allowedFireModes)
                if (fireMode == mode)
                    return true;
            return false;
        }

        public static GunState UpdateStateBoltAction(
            GunBase target, GunCockingHapticDataStore hapticData, VRC_Pickup.PickupHand hand,
            float progressNormalized, float minMargin, float maxMargin, float twistNormalized, float twistMaxMargin
        )
        {
            var curr = target.State;
            var next = curr;

            if (progressNormalized < minMargin)
            {
                if (curr != GunState.Idle && twistNormalized < twistMaxMargin)
                {
                    next = GunState.Idle;
                    if (hapticData && hapticData.Done)
                        hapticData.Done.PlayInHand(hand);
                }
                else if (curr != GunState.InCockingTwisting && twistNormalized > twistMaxMargin)
                {
                    next = GunState.InCockingTwisting;
                    if (hapticData && hapticData.Twist)
                        hapticData.Twist.PlayInHand(hand);
                }
            }
            else if (progressNormalized <= maxMargin && progressNormalized >= minMargin)
            {
                if (curr != GunState.InCockingPush && curr != GunState.InCockingPull)
                    next = GunState.InCockingPull;

                if (hapticData && hapticData.InBetween)
                    hapticData.InBetween.PlayHapticOnHand(hand, progressNormalized);
            }
            else if (progressNormalized > maxMargin)
            {
                if (curr != GunState.InCockingPush)
                {
                    next = GunState.InCockingPush;
                    target._LoadBullet();
                    target.HasCocked = true;
                    if (hapticData && hapticData.Pull)
                        hapticData.Pull.PlayInHand(hand);
                }
            }

            var anim = target.AnimationHelper;
            if (anim)
            {
                anim._SetCockingProgress(progressNormalized);
                anim._SetTwistingProgress(twistNormalized);
            }

            if (curr != next)
            {
                Debug.Log(
                    $"[GunHelper] Changing {target.name} state {curr.GetStateString()} to {next.GetStateString()} at {progressNormalized}");
                target.State = next;
            }

            return next;
        }

        public static GunState UpdateStateStraightPull(
            GunBase target, GunCockingHapticDataStore hapticData, VRC_Pickup.PickupHand hand,
            float progressNormalized, float minMargin, float maxMargin
        )
        {
            var curr = target.State;
            var next = curr;

            if (progressNormalized < minMargin &&
                curr != GunState.Idle)
            {
                next = GunState.Idle;
                if (hapticData && hapticData.Done)
                    hapticData.Done.PlayInHand(hand);
            }
            else if (progressNormalized <= maxMargin && progressNormalized >= minMargin)
            {
                if (curr != GunState.InCockingPush && curr != GunState.InCockingPull)
                    next = GunState.InCockingPull;

                if (hapticData && hapticData.InBetween)
                    hapticData.InBetween.PlayHapticOnHand(hand, progressNormalized);
            }
            else if (progressNormalized > maxMargin && curr != GunState.InCockingPush)
            {
                next = GunState.InCockingPush;
                target._LoadBullet();
                target.HasCocked = true;
                if (hapticData && hapticData.Pull)
                    hapticData.Pull.PlayInHand(hand);
            }

            var anim = target.AnimationHelper;
            if (anim)
            {
                anim._SetCockingProgress(progressNormalized);
            }

            if (curr != next)
            {
                Debug.Log(
                    $"[GunHelper] Changing {target.name} state {curr.GetStateString()} to {next.GetStateString()} at {progressNormalized}");
                target.State = next;
            }

            return next;
        }
    }
}
