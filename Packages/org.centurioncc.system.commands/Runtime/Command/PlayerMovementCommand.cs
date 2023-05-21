using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerMovementCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerMovement movement;

        public override string Label => "PlayerMovement";
        public override string[] Aliases => new[] { "Movement" };
        public override string Usage => "<command> <jump|run|walk|strafe|gravity|info|update|reset|apply> [value]";
        public override string Description => "Manipulate player's movement speed.";

        private void PrintInfo(NewbieConsole console)
        {
            console.Println(
                $"Jump: {movement.jumpImpulse}, " +
                $"Run: {movement.runSpeed}, " +
                $"Walk: {movement.walkSpeed}, " +
                $"Strafe: {movement.strafeSpeed}, " +
                $"Gravity: {movement.gravityStrength}");
        }

        private float HandleGravity(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                movement.gravityStrength = ConsoleParser.TryParseFloat(arguments[1]);
                movement.UpdateSetting();
            }

            console.Println($"Gravity: {movement.gravityStrength}");
            return movement.gravityStrength;
        }

        private float HandleJump(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                movement.jumpImpulse = ConsoleParser.TryParseFloat(arguments[1]);
                movement.UpdateSetting();
            }

            console.Println($"Jump: {movement.jumpImpulse}");
            return movement.jumpImpulse;
        }

        private float HandleStrafe(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                movement.strafeSpeed = ConsoleParser.TryParseFloat(arguments[1]);
                movement.UpdateSetting();
            }

            console.Println($"Strafe: {movement.strafeSpeed}");
            return movement.strafeSpeed;
        }

        private float HandleRun(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                movement.runSpeed = ConsoleParser.TryParseFloat(arguments[1]);
                movement.UpdateSetting();
            }

            console.Println($"Run: {movement.runSpeed}");
            return movement.runSpeed;
        }

        private float HandleWalk(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                movement.walkSpeed = ConsoleParser.TryParseFloat(arguments[1]);
                movement.UpdateSetting();
            }

            console.Println($"Walk: {movement.walkSpeed}");
            return movement.walkSpeed;
        }

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
            {
                PrintInfo(console);
                return ConsoleLiteral.GetNone();
            }

            switch (vars[0].ToLower())
            {
                case "j":
                case "jump":
                    return ConsoleLiteral.Of(HandleJump(console, vars));
                case "r":
                case "run":
                    return ConsoleLiteral.Of(HandleRun(console, vars));
                case "w":
                case "walk":
                    return ConsoleLiteral.Of(HandleWalk(console, vars));
                case "s":
                case "strafe":
                    return ConsoleLiteral.Of(HandleStrafe(console, vars));
                case "g":
                case "gravity":
                    return ConsoleLiteral.Of(HandleGravity(console, vars));
                case "reset":
                    movement.ResetSetting();
                    console.Println("Successfully reset settings");
                    return ConsoleLiteral.GetNone();
                case "u":
                case "update":
                    movement.UpdateSetting();
                    console.Println("Successfully updated settings");
                    return ConsoleLiteral.GetNone();
                case "a":
                case "apply":
                    if (!console.CurrentRole.HasPermission())
                    {
                        console.Println("Cannot access to this command unless you're moderator");
                        return ConsoleLiteral.GetNone();
                    }

                    movement.ApplySetting();
                    console.Println("Successfully applied settings");
                    return ConsoleLiteral.GetNone();
                default:
                    PrintInfo(console);
                    return ConsoleLiteral.GetNone();
            }
        }
    }
}