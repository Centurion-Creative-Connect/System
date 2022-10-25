using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    public static class GunUtility
    {
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

        public static GunState UpdateStateBoltAction(GunBase target, GunCockingHapticDataStore hapticData,
            VRC_Pickup.PickupHand hand,
            float progressNormalized, float minMargin, float maxMargin, float twistNormalized, float twistMaxMargin)
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
                if (curr != GunState.InCockingPush && curr != GunState.Idle)
                {
                    next = GunState.InCockingPush;
                    target.LoadBullet();
                    target.HasCocked = true;
                    if (hapticData && hapticData.Pull)
                        hapticData.Pull.PlayInHand(hand);
                }
            }

            var anim = target.TargetAnimator;
            if (anim)
            {
                anim.SetFloat(CockingProgressParameter(), progressNormalized);
                anim.SetFloat(CockingTwistParameter(), twistNormalized);
                anim.SetInteger(StateParameter(), (int)next);
                anim.SetBool(HasBulletParameter(), target.HasBulletInChamber);
                anim.SetBool(HasCockedParameter(), next == GunState.Idle && target.HasBulletInChamber);
            }

            if (curr != next)
            {
                Debug.Log(
                    $"[GunHelper] Changing {target.name} state {curr.GetStateString()} to {next.GetStateString()} at {progressNormalized}");
                target.State = next;
            }

            return next;
        }

        public static GunState UpdateStateStraightPull(GunBase target, GunCockingHapticDataStore hapticData,
            VRC_Pickup.PickupHand hand,
            float progressNormalized, float minMargin, float maxMargin)
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
            else if (progressNormalized > maxMargin &&
                     curr != GunState.InCockingPush && curr != GunState.Idle)
            {
                next = GunState.InCockingPush;
                target.LoadBullet();
                target.HasCocked = true;
                if (hapticData && hapticData.Pull)
                    hapticData.Pull.PlayInHand(hand);
            }

            var anim = target.TargetAnimator;
            if (anim)
            {
                anim.SetFloat(CockingProgressParameter(), progressNormalized);
                anim.SetInteger(StateParameter(), (int)next);
                anim.SetBool(HasBulletParameter(), target.HasBulletInChamber);
                anim.SetBool(HasCockedParameter(), target.HasBulletInChamber && next == GunState.Idle);
            }

            if (curr != next)
            {
                Debug.Log(
                    $"[GunHelper] Changing {target.name} state {curr.GetStateString()} to {next.GetStateString()} at {progressNormalized}");
                target.State = next;
            }

            return next;
        }

        public static int TriggerProgressParameter()
        {
            return Animator.StringToHash("TriggerProgress");
        }

        public static int CockingProgressParameter()
        {
            return Animator.StringToHash("CockingProgress");
        }

        public static int CockingTwistParameter()
        {
            return Animator.StringToHash("CockingTwist");
        }

        public static int IsPickedUpLocallyParameter()
        {
            return Animator.StringToHash("IsPickedUp");
        }

        public static int HasBulletParameter()
        {
            return Animator.StringToHash("HasBullet");
        }

        public static int HasCockedParameter()
        {
            return Animator.StringToHash("HasCocked");
        }

        public static int IsShootingParameter()
        {
            return Animator.StringToHash("IsShooting");
        }

        public static int IsShootingEmptyParameter()
        {
            return Animator.StringToHash("IsShootingEmpty");
        }

        public static int SelectorTypeParameter()
        {
            return Animator.StringToHash("SelectorType");
        }

        public static int StateParameter()
        {
            return Animator.StringToHash("State");
        }
    }
}