using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Logger;
using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Command.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EditableGunBulletDataStoreCommand : NewbieConsoleCommandHandler
    {
        [SerializeField]
        private EditableGunBulletDataStore targetDataStore;
        [SerializeField]
        private string customLabel;

        public override string Label => string.IsNullOrWhiteSpace(customLabel) ? name : customLabel;
        public override string Description =>
            $"Edits ProjectileDataProvider data of {(targetDataStore != null ? targetDataStore.name : "NULL!!!")}";
        public override string Usage => "<command> <projectileCount|speed|drag|hopUp|trailTime|sync> [value]";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars.Length == 0) return console.PrintUsage(this);
            var hasValue = vars.Length >= 2;
            var value = hasValue ? vars[1] : "";

            switch (vars[0].ToLower())
            {
                case "projectilecount":
                case "pcount":
                case "count":
                case "c":
                {
                    if (hasValue) targetDataStore.projectileCount = ConsoleParser.TryParseInt(value);
                    console.Println($"ProjectileCount: {targetDataStore.projectileCount}");
                    return ConsoleLiteral.Of(targetDataStore.projectileCount);
                }
                case "speed":
                case "spd":
                {
                    return HandleFloatCommand(
                        console,
                        ref targetDataStore.speed,
                        "Speed",
                        value,
                        hasValue
                    );
                }
                case "drag":
                case "d":
                {
                    return HandleFloatCommand(
                        console,
                        ref targetDataStore.drag,
                        "Drag",
                        value,
                        hasValue
                    );
                }
                case "hopupstrength":
                case "hopup":
                case "h":
                {
                    return HandleFloatCommand(
                        console,
                        ref targetDataStore.hopUpStrength,
                        "HopUpStrength",
                        value,
                        hasValue
                    );
                }
                case "trailtime":
                case "trail":
                case "t":
                {
                    return HandleFloatCommand(
                        console,
                        ref targetDataStore.trailTime,
                        "TrailTime",
                        value,
                        hasValue
                    );
                }
                case "sync":
                {
                    targetDataStore.Sync();
                    console.Println("Syncing!");
                    return ConsoleLiteral.GetNone();
                }
            }

            return console.PrintUsage(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string HandleFloatCommand(PrintableBase printable, ref float target, string name, string value,
                                                 bool hasValue)
        {
            if (hasValue) target = ConsoleParser.TryParseFloat(value);
            printable.Println($"{name}: {target}");
            return ConsoleLiteral.Of(target);
        }
    }
}
