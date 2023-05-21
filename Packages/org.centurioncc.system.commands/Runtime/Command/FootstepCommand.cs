using System;
using CenturionCC.System.Audio;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [Obsolete("Use PlayerController with PlayerControllerCommand instead.")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FootstepCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private FootstepGenerator footstep;

        public override string Label => "Footstep";
        public override string Description => "Read/Writes footstep sound settings";

        public override string Usage => "<command> <enable|step|pace|slow> [value] or <command> <apply>";

        private float HandlePace(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                footstep.footstepLength = ConsoleParser.TryParseFloat(arguments[1]);

            console.Println($"Len(pace): {footstep.footstepLength}");
            return footstep.footstepLength;
        }

        private float HandleStep(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                footstep.footstepTime = ConsoleParser.TryParseFloat(arguments[1]);

            console.Println($"Time(step): {footstep.footstepTime}");
            return footstep.footstepTime;
        }

        private float HandleSlow(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                footstep.slowFootstepThreshold = ConsoleParser.TryParseFloat(arguments[1]);

            console.Println($"Slow: {footstep.slowFootstepThreshold}");
            return footstep.slowFootstepThreshold;
        }

        private bool HandleEnable(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
                footstep.PlayFootstep = ConsoleParser.TryParseBoolean(arguments[1], footstep.PlayFootstep);

            console.Println($"Enabled: {footstep.PlayFootstep}");
            return footstep.PlayFootstep;
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
                    footstep.Apply();
                    return ConsoleLiteral.GetNone();
                default:
                    return console.PrintUsage(this);
            }
        }
    }
}