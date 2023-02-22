using System;
using CenturionCC.System.Audio;
using DerpyNewbie.Logger;
using UdonSharp;

namespace CenturionCC.System.Command
{
    [Obsolete("Use PlayerController with PlayerControllerCommand instead.")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FootstepCommand : NewbieConsoleCommandHandler
    {
        private FootstepGenerator _footstep;

        public override string Label => "Footstep";
        public override string Description => "Read/Writes footstep sound settings";

        public override string Usage => "<command> <enable|step|pace|slow> [value] or <command> <apply>";

        private void Start()
        {
            _footstep = CenturionSystemReference.GetGameManager().footstep;
        }

        private float HandlePace(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                _footstep.footstepLength = ConsoleParser.TryParseFloat(arguments[1]);

            console.Println($"Len(pace): {_footstep.footstepLength}");
            return _footstep.footstepLength;
        }

        private float HandleStep(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                _footstep.footstepTime = ConsoleParser.TryParseFloat(arguments[1]);

            console.Println($"Time(step): {_footstep.footstepTime}");
            return _footstep.footstepTime;
        }

        private float HandleSlow(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                _footstep.slowFootstepThreshold = ConsoleParser.TryParseFloat(arguments[1]);

            console.Println($"Slow: {_footstep.slowFootstepThreshold}");
            return _footstep.slowFootstepThreshold;
        }

        private bool HandleEnable(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                _footstep.PlayFootstep = ConsoleParser.TryParseBoolean(arguments[1], _footstep.PlayFootstep);

            console.Println($"Enabled: {_footstep.PlayFootstep}");
            return _footstep.PlayFootstep;
        }

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
                return console.PrintUsage(this);

            switch (vars[0].ToLower())
            {
                case "enable":
                    return ConsoleLiteral.Of(HandleEnable(console, vars));
                case "length":
                case "s":
                case "step":
                    return ConsoleLiteral.Of(HandleStep(console, vars));
                case "timer":
                case "p":
                case "pace":
                    return ConsoleLiteral.Of(HandlePace(console, vars));
                case "slow":
                case "sl":
                    return ConsoleLiteral.Of(HandleSlow(console, vars));
                case "apply":
                    _footstep.Apply();
                    return ConsoleLiteral.GetNone();
                default:
                    return console.PrintUsage(this);
            }
        }
    }
}