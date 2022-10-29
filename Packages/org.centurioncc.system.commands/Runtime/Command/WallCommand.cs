using CenturionCC.System.Utils;
using DerpyNewbie.Logger;
using UdonSharp;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WallCommand : BoolCommandHandler
    {
        private WallManager _wall;

        public override string Label => "WallManager";
        public override string[] Aliases => new[] { "Wall" };
        public override string Usage => "<command> <left|right|up|down> [value]";
        public override string Description => "Manipulates WallManager.";

        private void Start()
        {
            _wall = CenturionSystemReference.GetGameManager().wall;
        }

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
                        _wall.A00IsActive = ConsoleParser.TryParseBoolean(vars[1], _wall.A00IsActive);
                        _wall.Sync();
                    }

                    console.Println($"left: {_wall.A00IsActive}");
                    return _wall.A00IsActive;
                }
                case "a01":
                case "r":
                case "right":
                {
                    if (vars.Length >= 2)
                    {
                        _wall.A01IsActive = ConsoleParser.TryParseBoolean(vars[1], _wall.A01IsActive);
                        _wall.Sync();
                    }

                    console.Println($"right: {_wall.A01IsActive}");
                    return _wall.A01IsActive;
                }
                case "b00":
                case "u":
                case "up":
                {
                    if (vars.Length >= 2)
                    {
                        _wall.B00IsActive = ConsoleParser.TryParseBoolean(vars[1], _wall.B00IsActive);
                        _wall.Sync();
                    }

                    console.Println($"up: {_wall.B00IsActive}");
                    return _wall.B00IsActive;
                }
                case "b01":
                case "d":
                case "down":
                {
                    if (vars.Length >= 2)
                    {
                        _wall.B01IsActive = ConsoleParser.TryParseBoolean(vars[1], _wall.B01IsActive);
                        _wall.Sync();
                    }

                    console.Println($"down: {_wall.B01IsActive}");
                    return _wall.B01IsActive;
                }
                case "reload":
                case "refresh":
                {
                    _wall.Refresh();
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