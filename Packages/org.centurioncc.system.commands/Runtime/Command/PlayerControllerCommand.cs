using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerControllerCommand : ActionCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerController pc;

        public override string Label => "PlayerController";
        public override string[] Aliases => new[] { "PC" };
        public override string Usage =>
            "<command> <walk|run|strafe|jump|gravity|maxWeight|useGunDir|gunDirUp|gunDirLow|groundSnap|groundSnapDistance|groundSnapForward> [value] OR <command> <info>";
        public override string Description => "Adjust PlayerControllers properties in runtime.";

        public override void OnActionCommand(NewbieConsole console, string label, ref string[] vars,
            ref string[] envVars)
        {
            if (vars.Length <= 0)
            {
                console.PrintUsage(this);
                return;
            }

            var subLabel = vars[0].ToLower();
            var hasValue = vars.Length >= 2;

            switch (subLabel)
            {
                // ReSharper disable StringLiteralTypo
                case "walk":
                case "run":
                case "strafe":
                case "jump":
                case "gravity":
                case "maxweight":
                case "gundirup":
                case "gundirlow":
                case "groundsnapdistance":
                case "groundsnapforward":
                {
                    OnFloatSetSubCommand(console, subLabel, hasValue, vars);
                    return;
                }
                case "usegundir":
                {
                    if (hasValue)
                    {
                        var value = ConsoleParser.TryParseBoolean(vars[1], pc.checkGunDirectionToAllowRunning);
                        pc.checkGunDirectionToAllowRunning = value;
                    }

                    console.Println($"Check Gun Direction to Allow Running: {pc.checkGunDirectionToAllowRunning}");
                    return;
                }
                case "groundsnap":
                {
                    if (hasValue)
                    {
                        var value = ConsoleParser.TryParseBoolean(vars[1], pc.snapPlayerToGroundOnSlopes);
                        pc.snapPlayerToGroundOnSlopes = value;
                    }

                    console.Println($"Snap Player To Ground On Slopes: {pc.snapPlayerToGroundOnSlopes}");
                    return;
                }
                // ReSharper restore StringLiteralTypo
                case "info":
                    console.Println(
                        "Base Params:\n" +
                        $"  WalkSpd  : {pc.BaseWalkSpeed}\n" +
                        $"  RunSpd   : {pc.BaseRunSpeed}\n" +
                        $"  StrafeSpd: {pc.BaseStrafeSpeed}\n" +
                        $"  JumpImpls: {pc.BaseJumpImpulse}\n" +
                        $"  Gravity  : {pc.BaseGravityStrength}");
                    console.Println(
                        "Actual (Effective) Params:\n" +
                        $"  WalkSpd  : {pc.ActualWalkSpeed}\n" +
                        $"  RunSpd   : {pc.ActualRunSpeed}\n" +
                        $"  StrafeSpd: {pc.ActualStrafeSpeed}\n" +
                        $"  JumpImpls: {pc.ActualJumpImpulse}\n" +
                        $"  Gravity  : {pc.ActualGravityStrength}");
                    console.Println(
                        "Internal Params:\n" +
                        $"  CanRun         : {pc.CanRun}\n" +
                        $"  CheckGunDir    : {pc.checkGunDirectionToAllowRunning}\n" +
                        $"  GunDirUpperBnd : {pc.gunDirectionUpperBound}\n" +
                        $"  GunDirLowerBnd : {pc.gunDirectionLowerBound}\n" +
                        $"  SnapPlToGndOnSl: {pc.snapPlayerToGroundOnSlopes}" +
                        $"  MaxCarryWeight : {pc.maximumCarryingWeightInKilogram}\n" +
                        $"  PlayerWeight   : {pc.PlayerWeight}\n" +
                        $"  EnvMultiplier  : {pc.EnvironmentEffectMultiplier}\n" +
                        $"  TotalMultiplier: {pc.TotalMultiplier}\n");
                    return;
                default:
                    console.PrintUsage(this);
                    return;
            }
        }

        private void OnFloatSetSubCommand(NewbieConsole console, string subLabel, bool hasValue, string[] vars)
        {
            var value = float.NaN;
            hasValue = hasValue && !float.IsNaN(ConsoleParser.TryParseFloat(vars[1]));
            if (hasValue)
                value = ConsoleParser.TryParseFloat(vars[1]);

            switch (subLabel)
            {
                case "walk":
                    if (hasValue)
                        pc.BaseWalkSpeed = value;
                    console.Println($"Base Walk Speed: {pc.BaseWalkSpeed}");
                    return;
                case "run":
                    if (hasValue)
                        pc.BaseRunSpeed = value;
                    console.Println($"Base Run Speed: {pc.BaseRunSpeed}");
                    return;
                case "strafe":
                    if (hasValue)
                        pc.BaseStrafeSpeed = value;
                    console.Println($"Base Strafe Speed: {pc.BaseStrafeSpeed}");
                    return;
                case "jump":
                    if (hasValue)
                        pc.BaseJumpImpulse = value;
                    console.Println($"Base Jump Impulse: {pc.BaseJumpImpulse}");
                    return;
                case "gravity":
                    if (hasValue)
                        pc.BaseGravityStrength = value;
                    console.Println($"Base Gravity Strength: {pc.BaseGravityStrength}");
                    return;
                case "maxweight":
                    if (hasValue)
                        pc.maximumCarryingWeightInKilogram = value;
                    console.Println($"Maximum Carrying Weight: {pc.maximumCarryingWeightInKilogram}");
                    return;
                case "gundirup":
                    if (hasValue)
                        pc.gunDirectionUpperBound = value;
                    console.Println($"Gun Direction Upper Bound: {pc.gunDirectionUpperBound}");
                    return;
                case "gundirlow":
                    if (hasValue)
                        pc.gunDirectionLowerBound = value;
                    console.Println($"Gun Direction Lower Bound: {pc.gunDirectionLowerBound}");
                    return;
                case "groundsnapdistance":
                    if (hasValue)
                        pc.groundSnapMaxDistance = value;
                    console.Println($"Ground Snap Max Distance: {pc.groundSnapMaxDistance}");
                    return;
                case "groundsnapforward":
                    if (hasValue)
                        pc.groundSnapForwardDistance = value;
                    console.Println($"Ground Snap Forward: {pc.groundSnapForwardDistance}");
                    return;
                default:
                    console.Println($"Unknown subcommand: {subLabel}");
                    return;
            }
        }
    }
}