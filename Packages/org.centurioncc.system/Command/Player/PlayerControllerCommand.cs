using CenturionCC.System.Gun;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.PlayerLocomotion;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Command.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerControllerCommand : ActionCommandHandler
    {
        [SerializeField] [NewbieInject]
        private PlayerController pc;
        [SerializeField] [NewbieInject]
        private PlayerControllerGunExtension gunExt;

        public override string Label => "PlayerController";

        public override string[] Aliases => new[] { "PC" };

        public override string Usage =>
            "<command>\n" +
            "  walk [float]\n" +
            "  run [float]\n" +
            "  strafe [float]\n" +
            "  jump [float]\n" +
            "  gravity [float]\n" +
            "  maxWeight [float]\n" +
            "  groundSnap [bool]\n" +
            "  groundSnapDistance [float]\n" +
            "  groundSnapForward [float]\n" +
            "  useGunSprint [bool]\n" +
            "  gunSprintThreshold [float]\n" +
            "  gunSprintRun [float]\n" +
            "  gunSprintWalk [float]\n" +
            "  useCombatTag [bool]\n" +
            "  combatTagTime [float]\n" +
            "  combatTagSpeed [float]\n" +
            "  info\n";

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
                case "gunsprintdirectionthreshold": // proper
                case "gunsprintthreshold":
                case "dirthreshold":
                case "gundirup":
                case "gunsprintrunspeed": // proper
                case "gunsprintrun":
                case "gunsprintwalkspeed": // proper
                case "gunsprintwalk":
                case "groundsnapmaxdistance": // proper
                case "groundsnapdistance":
                case "groundsnapforwarddistance": // proper
                case "groundsnapforward":
                case "combattagspeedmultiplier": // proper
                case "combattagspeed":
                case "combattagspdmul":
                case "combattagtime": // proper
                {
                    OnFloatSetSubCommand(console, subLabel, hasValue, vars);
                    return;
                }
                case "usecombattag":
                case "gunsprint":
                case "usegunsprint":
                case "usegundir":
                case "groundsnap":
                {
                    OnBoolSetSubCommand(console, subLabel, hasValue, vars);
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
                        $"  SnapPlToGndOnSl: {pc.snapPlayerToGroundOnSlopes}\n" +
                        $"  MaxCarryWeight : {pc.maximumCarryingWeightInKilogram}\n" +
                        $"  PlayerWeight   : {pc.PlayerWeight}\n" +
                        $"  EnvMultiplier  : {pc.EnvironmentEffectMultiplier}\n" +
                        $"  TotalMultiplier: {pc.TotalMultiplier}\n" +
                        $"Gun Integration Params:\n" +
                        $"Base:\n" +
                        $"  UseGunSprint   : {gunExt.BaseUseGunSprint}\n" +
                        $"  GunSprintRun   : {gunExt.BaseGunSprintRunSpeed}\n" +
                        $"  GunSprintWalk  : {gunExt.BaseGunSprintWalkSpeed}\n" +
                        $"  DirThreshold   : {gunExt.BaseGunSprintDirectionThreshold}\n" +
                        $"  UseCombatTag   : {gunExt.BaseUseCombatTag}\n" +
                        $"  CombatTagTime  : {gunExt.BaseCombatTagTime}\n" +
                        $"  CombatTagSpdMul: {gunExt.BaseCombatTagSpeedMultiplier}\n" +
                        $"Actual:\n" +
                        $"  UseGunSprint   : {gunExt.ActualUseGunSprint}\n" +
                        $"  GunSprintRun   : {gunExt.ActualGunSprintRunSpeed}\n" +
                        $"  GunSprintWalk  : {gunExt.ActualGunSprintWalkSpeed}\n" +
                        $"  DirThreshold   : {gunExt.ActualGunSprintDirectionThreshold}\n" +
                        $"  UseCombatTag   : {gunExt.ActualUseCombatTag}\n" +
                        $"  CombatTagTime  : {gunExt.ActualCombatTagTime}\n" +
                        $"  CombatTagSpdMul: {gunExt.ActualCombatTagSpeedMultiplier}");
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
                case "gunsprintdirectionthreshold":
                case "gunsprintthreshold":
                case "dirthreshold":
                case "gundirup":
                    if (hasValue)
                        gunExt.BaseGunSprintDirectionThreshold = value;
                    console.Println($"Base Gun Sprint Direction Threshold: {gunExt.BaseGunSprintDirectionThreshold}");
                    return;
                case "gunsprintrunspeed":
                case "gunsprintrun":
                    if (hasValue)
                        gunExt.BaseGunSprintRunSpeed = value;
                    console.Println($"Base Gun Sprint Run Speed: {gunExt.BaseGunSprintRunSpeed}");
                    return;
                case "gunsprintwalkspeed":
                case "gunsprintwalk":
                    if (hasValue)
                        gunExt.BaseGunSprintWalkSpeed = value;
                    console.Println($"Base Gun Sprint Walk Speed: {gunExt.BaseGunSprintWalkSpeed}");
                    return;
                case "combattagtime":
                    if (hasValue)
                        gunExt.BaseCombatTagTime = value;
                    console.Println($"Base Combat Tag Time: {gunExt.BaseCombatTagTime}");
                    return;
                case "combattagspeedmultiplier":
                case "combattagspeed":
                case "combattagspdmul":
                    if (hasValue)
                        gunExt.BaseCombatTagSpeedMultiplier = value;
                    console.Println($"Base Combat Tag Speed Multiplier: {gunExt.BaseCombatTagSpeedMultiplier}");
                    return;
                case "groundsnapmaxdistance":
                case "groundsnapdistance":
                    if (hasValue)
                        pc.groundSnapMaxDistance = value;
                    console.Println($"Ground Snap Max Distance: {pc.groundSnapMaxDistance}");
                    return;
                case "groundsnapforwarddistance":
                case "groundsnapforward":
                    if (hasValue)
                        pc.groundSnapForwardDistance = value;
                    console.Println($"Ground Snap Forward Distance: {pc.groundSnapForwardDistance}");
                    return;
                default:
                    console.Println($"Unknown subcommand: {subLabel}");
                    return;
            }
        }

        private void OnBoolSetSubCommand(NewbieConsole console, string subLabel, bool hasValue, string[] vars)
        {
            switch (subLabel)
            {
                case "usecombattag":
                {
                    if (hasValue)
                    {
                        var value = ConsoleParser.TryParseBoolean(vars[1], gunExt.BaseUseCombatTag);
                        gunExt.BaseUseCombatTag = value;
                    }

                    console.Println($"Base Use Combat Tag: {gunExt.BaseUseCombatTag}");
                    return;
                }
                case "gunsprint":
                case "usegunsprint":
                case "usegundir":
                {
                    if (hasValue)
                    {
                        var value = ConsoleParser.TryParseBoolean(vars[1], gunExt.BaseUseGunSprint);
                        gunExt.BaseUseGunSprint = value;
                    }

                    console.Println($"Base Use Gun Sprint: {gunExt.BaseUseGunSprint}");
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
                default:
                    console.Println($"Unknown subcommand: {subLabel}");
                    return;
            }
        }
    }
}
