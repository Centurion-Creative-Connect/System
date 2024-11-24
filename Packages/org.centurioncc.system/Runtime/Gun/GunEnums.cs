using System;

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
        /// <summary>
        /// When GunState conversion (byte to GunState) failed.
        /// </summary>
        /// <seealso cref="Gun.State"/>
        /// <seealso cref="Gun.RawState"/>
        Unknown = 0xFF,

        /// <summary>
        /// When it's in idle state. With <see cref="Gun.HasBulletInChamber"/> and <see cref="Gun.HasCocked"/>, it should be able to shoot.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// When it's pulling state. For most of guns in this state should not be able to shoot.
        /// </summary>
        InCockingPull = 1,

        /// <summary>
        /// When it's pushing state. For most of guns in this state should not be able to shoot.
        /// </summary>
        InCockingPush = 2,

        /// <summary>
        /// When it's twisting state. For most of guns in this state should not be able to shoot.
        /// </summary>
        InCockingTwisting = 3
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
        /// This will return in condition like first and second shot of <see cref="FireMode.ThreeRoundsBurst"/>. third shot will be <see cref="Succeeded"/>.
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

    public enum MovementOption
    {
        Inherit,
        Direct,
        Multiply,
        Disable
    }

    public enum CombatTagOption
    {
        Inherit,
        Direct,
        Multiply,
        Disable
    }

    public enum GunManagerResetType
    {
        All,
        Unused
    }

    public static class GunStateHelper
    {
        public const byte MaxValue = (byte)GunState.InCockingTwisting;
        public const byte MinValue = (byte)GunState.Idle;

        public static string GetStateString(byte value)
        {
            return
                value == (int)GunState.Unknown ? "Undefined" :
                value == (int)GunState.Idle ? "Idle" :
                value == (int)GunState.InCockingPull ? "InCockingPull" :
                value == (int)GunState.InCockingPush ? "InCockingPush" :
                value == (int)GunState.InCockingTwisting ? "InCockingTwisting" :
                $"Unknown ({value})";
        }

        public static string GetStateString(this GunState state)
        {
            return GetStateString(Convert.ToByte(state));
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
}