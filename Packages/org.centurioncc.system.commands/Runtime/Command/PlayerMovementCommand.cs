using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerMovementCommand : NewbieConsoleCommandHandler
    {
        private PlayerMovement _movement;

        private void Start()
        {
            _movement = GameManagerHelper.GetGameManager().movement;
        }

        private void PrintInfo(NewbieConsole console)
        {
            console.Println(
                $"Jump: {_movement.jumpImpulse}, " +
                $"Run: {_movement.runSpeed}, " +
                $"Walk: {_movement.walkSpeed}, " +
                $"Strafe: {_movement.strafeSpeed}, " +
                $"Gravity: {_movement.gravityStrength}");
        }

        private float HandleGravity(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                _movement.gravityStrength = ConsoleParser.TryParseFloat(arguments[1]);
                _movement.UpdateSetting();
            }

            console.Println($"Gravity: {_movement.gravityStrength}");
            return _movement.gravityStrength;
        }

        private float HandleJump(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                _movement.jumpImpulse = ConsoleParser.TryParseFloat(arguments[1]);
                _movement.UpdateSetting();
            }

            console.Println($"Jump: {_movement.jumpImpulse}");
            return _movement.jumpImpulse;
        }

        private float HandleStrafe(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                _movement.strafeSpeed = ConsoleParser.TryParseFloat(arguments[1]);
                _movement.UpdateSetting();
            }

            console.Println($"Strafe: {_movement.strafeSpeed}");
            return _movement.strafeSpeed;
        }

        private float HandleRun(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                _movement.runSpeed = ConsoleParser.TryParseFloat(arguments[1]);
                _movement.UpdateSetting();
            }

            console.Println($"Run: {_movement.runSpeed}");
            return _movement.runSpeed;
        }

        private float HandleWalk(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                _movement.walkSpeed = ConsoleParser.TryParseFloat(arguments[1]);
                _movement.UpdateSetting();
            }

            console.Println($"Walk: {_movement.walkSpeed}");
            return _movement.walkSpeed;
        }

        public override string Label => "PlayerMovement";
        public override string[] Aliases => new[] { "Movement" };
        public override string Usage => "<command> <jump|run|walk|strafe|gravity|info|update|reset|apply> [value]";
        public override string Description => "Manipulate player's movement speed.";

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
                    _movement.ResetSetting();
                    console.Println("Successfully reset settings");
                    return ConsoleLiteral.GetNone();
                case "u":
                case "update":
                    _movement.UpdateSetting();
                    console.Println("Successfully updated settings");
                    return ConsoleLiteral.GetNone();
                case "a":
                case "apply":
                    if (!console.CurrentRole.HasPermission())
                    {
                        console.Println("Cannot access to this command unless you're moderator");
                        return ConsoleLiteral.GetNone();
                    }

                    _movement.ApplySetting();
                    console.Println("Successfully applied settings");
                    return ConsoleLiteral.GetNone();
                default:
                    PrintInfo(console);
                    return ConsoleLiteral.GetNone();
            }
        }
    }
}