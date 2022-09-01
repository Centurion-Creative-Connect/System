using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MiniMapCommand : BoolCommandHandler
    {
        [SerializeField]
        private GameObject top;
        [SerializeField]
        private GameObject a00;
        [SerializeField]
        private GameObject a01;
        [SerializeField]
        private GameObject b00;
        [SerializeField]
        private GameObject b01;

        private static bool Result(NewbieConsole console, string fieldName, GameObject target, string[] args)
        {
            return PrintResult(
                console,
                fieldName,
                args.Length >= 2
                    ? ToggleObject(target, args[1])
                    : target.activeSelf
            );
        }

        private static bool ToggleObject(GameObject obj, string next)
        {
            obj.SetActive(ConsoleParser.TryParseBoolean(next, obj.activeSelf));
            return obj.activeSelf;
        }

        private static bool PrintResult(NewbieConsole console, string field, bool result)
        {
            console.Println($"{field}: {result}");
            return result;
        }

        public override string Label => "MiniMap";
        public override string Usage => "<command> <top|a00|a01|b00|b01> [value]";

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return false;
            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "top":
                    return Result(console, "top", top, vars);
                case "a00":
                    return Result(console, "a00", a00, vars);
                case "a01":
                    return Result(console, "a01", a01, vars);
                case "b00":
                    return Result(console, "b00", b00, vars);
                case "b01":
                    return Result(console, "b01", b01, vars);
                default:
                {
                    console.PrintUsage(this);
                    return false;
                }
            }
        }
    }
}