using CenturionCC.System.Gun.GunCamera;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunCameraCommand : NewbieConsoleCommandHandler
    {
        private const string ResultString = "{0}: {1}";
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunCamera instance;

        public override string Label => "GunCamera";
        public override string[] Aliases => new[] { "GunCam" };
        public override string Usage => "<command> <enable|visible|invert|offset|pickup|update>";
        public override string Description => "Enable or disable GunCamera, Customize them how you like.";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return console.PrintUsage(this);
            switch (vars[0])
            {
                case "en":
                case "enabled":
                case "enable":
                {
                    if (vars.Length >= 2)
                        instance.IsOn = ConsoleParser.TryParseBoolean(vars[1], instance.IsOn);

                    console.Println(string.Format(ResultString, "isEnabled", instance.IsOn));
                    return ConsoleLiteral.Of(instance.IsOn);
                }
                case "v":
                case "visible":
                case "visibility":
                {
                    if (vars.Length >= 2)
                        instance.IsVisible = ConsoleParser.TryParseBoolean(vars[1], instance.IsVisible);

                    console.Println(string.Format(ResultString, "isVisible", instance.IsVisible));
                    return ConsoleLiteral.Of(instance.IsVisible);
                }
                case "i":
                case "invert":
                {
                    if (vars.Length >= 2)
                        instance.OffsetIndex = ConsoleParser.TryParseBoolAsInt(vars[1], instance.OffsetIndex == 1);

                    console.Println(string.Format(ResultString, "isInverted", instance.OffsetIndex == 1));
                    return ConsoleLiteral.Of(instance.OffsetIndex);
                }
                case "o":
                case "offset":
                {
                    if (vars.Length >= 2)
                        instance.OffsetIndex = ConsoleParser.TryParseInt(vars[1]);

                    console.Println(string.Format(ResultString, "offset", instance.OffsetIndex));
                    return ConsoleLiteral.Of(instance.OffsetIndex);
                }
                case "p":
                case "pickup":
                {
                    if (vars.Length >= 2)
                        instance.IsPickupable = ConsoleParser.TryParseBoolean(vars[1], instance.IsPickupable);

                    console.Println(string.Format(ResultString, "pickup", instance.IsPickupable));
                    return ConsoleLiteral.Of(instance.IsPickupable);
                }
                case "u":
                case "update":
                {
                    instance.UpdateGunCameraPosition();
                    console.Println("Updated gun camera position and rotation");
                    return ConsoleLiteral.GetNone();
                }
                default:
                    return console.PrintUsage(this);
            }
        }
    }
}