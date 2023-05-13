using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WallCommand : BoolCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private WallManager wall;

        public override string Label => "WallManager";
        public override string[] Aliases => new[] { "Wall" };
        public override string Usage => "<command> <left|right|up|down> [value]";
        public override string Description => "Manipulates WallManager.";

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
            {
                console.PrintUsage(this);
                return false;
            }

            if (vars.Length >= 2 && !console.CurrentRole.HasPermission())
            {
                console.Println("You do not have permission to change state");
                return false;
            }

            switch (vars[0])
            {
                case "a00":
                case "l":
                case "left":
                {
                    if (vars.Length >= 2)
                    {
                        wall.A00IsActive = ConsoleParser.TryParseBoolean(vars[1], wall.A00IsActive);
                        wall.Sync();
                    }

                    console.Println($"left: {wall.A00IsActive}");
                    return wall.A00IsActive;
                }
                case "a01":
                case "r":
                case "right":
                {
                    if (vars.Length >= 2)
                    {
                        wall.A01IsActive = ConsoleParser.TryParseBoolean(vars[1], wall.A01IsActive);
                        wall.Sync();
                    }

                    console.Println($"right: {wall.A01IsActive}");
                    return wall.A01IsActive;
                }
                case "b00":
                case "u":
                case "up":
                {
                    if (vars.Length >= 2)
                    {
                        wall.B00IsActive = ConsoleParser.TryParseBoolean(vars[1], wall.B00IsActive);
                        wall.Sync();
                    }

                    console.Println($"up: {wall.B00IsActive}");
                    return wall.B00IsActive;
                }
                case "b01":
                case "d":
                case "down":
                {
                    if (vars.Length >= 2)
                    {
                        wall.B01IsActive = ConsoleParser.TryParseBoolean(vars[1], wall.B01IsActive);
                        wall.Sync();
                    }

                    console.Println($"down: {wall.B01IsActive}");
                    return wall.B01IsActive;
                }
                case "reload":
                case "refresh":
                {
                    wall.Refresh();
                    return true;
                }
                default:
                {
                    console.PrintUsage(this);
                    return false;
                }
            }
        }
    }
}