using System;
using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    public enum FireMode
    {
        Unknown = 0xFF,
        Safety = 0,
        SemiAuto = 1,
        FullAuto = 2,
        TwoRoundsBurst = 3,
        ThreeRoundsBurst = 4,
        FourRoundsBurst = 5,
        FiveRoundsBurst = 6
    }

    public enum HandleType
    {
        None = 0,
        MainHandle = 1,
        SubHandle = 2,
        CustomHandle = 3
    }

    public enum GunState
    {
        Unknown = 0xFF,
        Idle = 0,
        ReadyToShoot = 1,
        Pulling = 2,
        PullingWithBullet = 3,
        Pushing = 4,
        PushingWithBullet = 5,
        Twisting = 6,
        TwistingWithBullet = 7,
        IdleWithBullet = 8,
        IdleWithCocked = 9
    }

    public enum TriggerState
    {
        /// <summary>
        /// Trigger when it's in safety.
        /// </summary>
        Idle = 0,
        /// <summary>
        /// Trigger when it's movable.
        /// </summary>
        Armed = 1,
        /// <summary>
        /// Trigger when it's pulled, and should be firing.
        /// </summary>
        Firing = 2,
        /// <summary>
        /// Trigger when it's pulled, and should not be firing until released again.
        /// </summary>
        Fired = 3
    }

    public enum ShotResult
    {
        /// <summary>
        /// When shot was succeeded.
        /// </summary>
        Succeeded,
        /// <summary>
        /// When shot was succeeded and should continue to shoot next possible frame.
        /// </summary>
        /// <remarks>
        /// This will return in condition like 1st and 2nd shot of <see cref="FireMode.ThreeRoundsBurst"/>. 3rd shot of that will be <see cref="Succeeded"/>.
        /// </remarks>
        SucceededContinuously,
        /// <summary>
        /// When shot was paused until the Gun is able to shoot next possible frame.
        /// </summary>
        Paused,
        /// <summary>
        /// When shot was cancelled due to custom game rule such as no firing inside safezone. will make no sound at all.
        /// </summary>
        Cancelled,
        /// <summary>
        /// When shot was failed due to mechanical issue such as fire mode being in safety. will make a trigger sound.
        /// </summary>
        Failed,
    }

    public static class GunStateHelper
    {
        public const byte MaxValue = (byte)GunState.IdleWithCocked;
        public const byte MinValue = (byte)GunState.Idle;

        public static string GetStateString(byte value)
        {
            return
                value == (int)GunState.Unknown ? "Undefined" :
                value == (int)GunState.Idle ? "Idle" :
                value == (int)GunState.ReadyToShoot ? "ReadyToShoot" :
                value == (int)GunState.Pulling ? "Pulling" :
                value == (int)GunState.PullingWithBullet ? "PullingWithBullet" :
                value == (int)GunState.Pushing ? "Pushing" :
                value == (int)GunState.PushingWithBullet ? "PushingWithBullet" :
                value == (int)GunState.Twisting ? "Twisting" :
                value == (int)GunState.TwistingWithBullet ? "TwistingWithBullet" :
                value == (int)GunState.IdleWithCocked ? "IdleWithCocked" :
                $"Unknown ({value})";
        }

        public static string GetStateString(this GunState state)
        {
            return GetStateString(Convert.ToByte(state));
        }

        public static bool IsIdleState(this GunState state)
        {
            switch (state)
            {
                case GunState.Idle:
                case GunState.IdleWithBullet:
                case GunState.IdleWithCocked:
                case GunState.ReadyToShoot:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPullingState(this GunState state)
        {
            switch (state)
            {
                case GunState.Pulling:
                case GunState.PullingWithBullet:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPushingState(this GunState state)
        {
            switch (state)
            {
                case GunState.Pushing:
                case GunState.PushingWithBullet:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsPullingOrPushingState(this GunState state)
        {
            return state.IsPullingState() || state.IsPushingState();
        }

        public static bool IsTwistingState(this GunState state)
        {
            switch (state)
            {
                case GunState.Twisting:
                case GunState.TwistingWithBullet:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsBulletInChamber(this GunState state)
        {
            switch (state)
            {
                case GunState.ReadyToShoot:
                case GunState.PullingWithBullet:
                case GunState.PushingWithBullet:
                case GunState.TwistingWithBullet:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsCocked(this GunState state)
        {
            switch (state)
            {
                case GunState.IdleWithCocked:
                case GunState.ReadyToShoot:
                case GunState.Pushing:
                case GunState.PushingWithBullet:
                case GunState.PullingWithBullet:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsReadyToShoot(this GunState state)
        {
            return state == GunState.ReadyToShoot;
        }

        public static bool IsHandleIdleState(int state)
        {
            return state == (int)GunState.Idle || state == (int)GunState.IdleWithBullet ||
                   state == (int)GunState.ReadyToShoot;
        }

        public static bool IsHandlePullingState(int state)
        {
            return state == (int)GunState.Pulling || state == (int)GunState.PullingWithBullet;
        }

        public static bool IsHandlePushingState(int state)
        {
            return state == (int)GunState.Pushing || state == (int)GunState.PushingWithBullet;
        }

        public static bool IsHandleTwistingState(int state)
        {
            return state == (int)GunState.Twisting || state == (int)GunState.TwistingWithBullet;
        }

        public static bool HasBulletInChamber(int state)
        {
            return state == (int)GunState.ReadyToShoot || state == (int)GunState.PullingWithBullet ||
                   state == (int)GunState.PushingWithBullet || state == (int)GunState.TwistingWithBullet;
        }

        public static bool HasCocked(int state)
        {
            return state == (int)GunState.IdleWithCocked ||
                   state == (int)GunState.ReadyToShoot ||
                   state == (int)GunState.Pushing ||
                   state == (int)GunState.PushingWithBullet ||
                   state == (int)GunState.PullingWithBullet;
        }
    }

    public static class FireModeHelper
    {
        public static bool IsBurstMode(this FireMode mode)
        {
            switch (mode)
            {
                case FireMode.TwoRoundsBurst:
                case FireMode.ThreeRoundsBurst:
                case FireMode.FourRoundsBurst:
                case FireMode.FiveRoundsBurst:
                    return true;
                case FireMode.Unknown:
                case FireMode.Safety:
                case FireMode.SemiAuto:
                case FireMode.FullAuto:
                default:
                    return false;
            }
        }

        public static bool HasFiredEnough(this FireMode mode, int burstCount)
        {
            switch (mode)
            {
                case FireMode.Unknown:
                case FireMode.SemiAuto:
                case FireMode.Safety:
                    return true;
                case FireMode.TwoRoundsBurst:
                    return burstCount >= 2;
                case FireMode.ThreeRoundsBurst:
                    return burstCount >= 3;
                case FireMode.FourRoundsBurst:
                    return burstCount >= 4;
                case FireMode.FiveRoundsBurst:
                    return burstCount >= 5;
                case FireMode.FullAuto:
                default:
                    return false;
            }
        }

        public static bool ShouldStopOnTriggerUp(this FireMode mode)
        {
            switch (mode)
            {
                case FireMode.TwoRoundsBurst:
                case FireMode.ThreeRoundsBurst:
                case FireMode.FourRoundsBurst:
                case FireMode.FiveRoundsBurst:
                case FireMode.SemiAuto:
                case FireMode.Unknown:
                    return false;
                case FireMode.Safety:
                case FireMode.FullAuto:
                default:
                    return true;
            }
        }

        public static FireMode CycleFireMode(FireMode fireMode, FireMode[] allowedFireModes)
        {
            if (allowedFireModes == null || allowedFireModes.Length == 0)
                return fireMode;

            var index = allowedFireModes.FindItem(fireMode);
            if (index == -1 || index + 1 >= allowedFireModes.Length)
                return allowedFireModes[0];

            return allowedFireModes[index + 1];
        }

        public static bool IsValidFireMode(this FireMode mode, FireMode[] allowedFireModes)
        {
            foreach (var fireMode in allowedFireModes)
                if (fireMode == mode)
                    return true;
            return false;
        }

        public static string GetStateString(this FireMode mode)
        {
            switch (mode)
            {
                case FireMode.Safety:
                    return "Safety";
                case FireMode.SemiAuto:
                    return "SemiAuto";
                case FireMode.FullAuto:
                    return "FullAuto";
                case FireMode.TwoRoundsBurst:
                    return "TwoRoundsBurst";
                case FireMode.ThreeRoundsBurst:
                    return "ThreeRoundsBurst";
                case FireMode.FourRoundsBurst:
                    return "FourRoundsBurst";
                case FireMode.FiveRoundsBurst:
                    return "FiveRoundsBurst";
                case FireMode.Unknown:
                default:
                    return $"Unknown:{mode}";
            }
        }
    }

    public static class TriggerStateHelper
    {
        public static string GetStateString(this TriggerState state)
        {
            switch (state)
            {
                case TriggerState.Idle:
                    return "Idle";
                case TriggerState.Armed:
                    return "Armed";
                case TriggerState.Firing:
                    return "Firing";
                case TriggerState.Fired:
                    return "Fired";
                default:
                    return $"UnknownState:{state}";
            }
        }
    }

    public static class ShotResultHelper
    {
        // public static bool IsSuccess(this ShotResult result)
        // {
        //     switch (result)
        //     {
        //         case ShotResult.Success:
        //             return true;
        //         case ShotResult.ContinuousSuccess:
        //         case ShotResult.Fail:
        //         case ShotResult.FailByFireMode:
        //         case ShotResult.FailByBehaviour:
        //         case ShotResult.FailByCustomCheck:
        //         case ShotResult.Paused:
        //         default:
        //             return false;
        //     }
        // }

        public static string GetStateString(this ShotResult result)
        {
            switch (result)
            {
                case ShotResult.Succeeded:
                    return "Succeeded";
                case ShotResult.SucceededContinuously:
                    return "SucceededContinuously";
                case ShotResult.Paused:
                    return "Paused";
                case ShotResult.Failed:
                    return "Failed";
                default:
                    return $"UnknownState:{result}";
            }
        }
    }

    public static class GunHelper
    {
        public static GunState UpdateStateBoltAction(GunBase target, GunCockingHapticDataStore hapticData,
            VRC_Pickup.PickupHand hand,
            float progressNormalized, float minMargin, float maxMargin, float twistNormalized, float twistMaxMargin)
        {
            var curr = target.State;
            var next = curr;

            if (progressNormalized < minMargin)
            {
                if (!curr.IsIdleState() && twistNormalized < twistMaxMargin)
                {
                    next = curr.IsBulletInChamber() ? GunState.ReadyToShoot : GunState.IdleWithCocked;
                    if (hapticData && hapticData.Done)
                        hapticData.Done.PlayInHand(hand);
                }
                else if (!curr.IsTwistingState() && twistNormalized > twistMaxMargin)
                {
                    next = curr.IsBulletInChamber() ? GunState.TwistingWithBullet : GunState.Twisting;
                    if (hapticData && hapticData.Twist)
                        hapticData.Twist.PlayInHand(hand);
                }
            }
            else if (progressNormalized <= maxMargin && progressNormalized >= minMargin)
            {
                if (!curr.IsPullingOrPushingState())
                    next = curr.IsBulletInChamber() ? GunState.PullingWithBullet : GunState.Pulling;

                if (hapticData && hapticData.InBetween)
                    hapticData.InBetween.PlayHapticOnHand(hand, progressNormalized);
            }
            else if (progressNormalized > maxMargin)
            {
                if (!curr.IsPushingState() && curr != GunState.Idle)
                {
                    next = GunState.PushingWithBullet;
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
                anim.SetBool(HasBulletParameter(), next.IsBulletInChamber());
                anim.SetBool(HasCockedParameter(), next.IsCocked());
            }

            if (curr != next)
            {
                Debug.Log($"[GunHelper] Changing {target.name} state {curr} to {next} at {progressNormalized}");
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

            if (progressNormalized < minMargin)
            {
                if (!curr.IsIdleState())
                {
                    next = curr.IsBulletInChamber() ? GunState.ReadyToShoot : GunState.IdleWithCocked;
                    if (hapticData && hapticData.Done)
                        hapticData.Done.PlayInHand(hand);
                }
            }
            else if (progressNormalized <= maxMargin && progressNormalized >= minMargin)
            {
                if (!curr.IsPullingOrPushingState())
                    next = curr.IsBulletInChamber() ? GunState.PullingWithBullet : GunState.Pulling;

                if (hapticData && hapticData.InBetween)
                    hapticData.InBetween.PlayHapticOnHand(hand, progressNormalized);
            }
            else if (progressNormalized > maxMargin)
            {
                if (!curr.IsPushingState() && curr != GunState.Idle)
                {
                    next = GunState.PushingWithBullet;
                    if (hapticData && hapticData.Pull)
                        hapticData.Pull.PlayInHand(hand);
                }
            }

            var anim = target.TargetAnimator;
            if (anim)
            {
                anim.SetFloat(CockingProgressParameter(), progressNormalized);
                anim.SetInteger(StateParameter(), (int)next);
                anim.SetBool(HasBulletParameter(), next.IsBulletInChamber());
                anim.SetBool(HasCockedParameter(), next.IsCocked());
            }

            if (curr != next)
            {
                Debug.Log($"[GunHelper] Changing {target.name} state {curr} to {next} at {progressNormalized}");
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