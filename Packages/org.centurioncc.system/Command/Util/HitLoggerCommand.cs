using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Command.Util
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HitLoggerCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [NewbieInject]
        private HitLogger hitLogger;

        public override string Label => "HitLogger";
        public override string Description =>
            "Logs local player hits and may broadcast it's data depending on settings.";
        public override string Usage =>
            "<command> <debuglog|logOnHit|logOnKilled|syncGlobally|onlyShooterDetection|persistTime> [value]";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars.Length <= 0)
            {
                console.PrintUsage(this);
                return ConsoleLiteral.GetNone();
            }

            vars = vars.RemoveItem("-s", out var suppress);

            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "debuglog":
                    if (2 <= vars.Length)
                        hitLogger.logOnKilled =
                            ConsoleParser.TryParseBoolean(vars[1], hitLogger.logOnKilled);
                    if (!suppress)
                        console.Println($"DebugLog: {hitLogger.logOnKilled}");
                    return ConsoleLiteral.Of(hitLogger.logOnKilled);
                case "syncglobally":
                    if (2 <= vars.Length)
                        hitLogger.drawGlobally = ConsoleParser.TryParseBoolean(vars[1], hitLogger.drawGlobally);
                    if (!suppress)
                        console.Println($"Sync Globally: {hitLogger.drawGlobally}");
                    return ConsoleLiteral.Of(hitLogger.drawGlobally);
                case "logonkilled":
                    if (2 <= vars.Length)
                        hitLogger.drawOnKilled =
                            ConsoleParser.TryParseBoolean(vars[1], hitLogger.drawOnKilled);
                    if (!suppress)
                        console.Println($"Log On Killed: {hitLogger.drawOnKilled}");
                    return ConsoleLiteral.Of(hitLogger.drawOnKilled);
                case "logonhit":
                    if (2 <= vars.Length)
                        hitLogger.drawOnHitDetection =
                            ConsoleParser.TryParseBoolean(vars[1], hitLogger.drawOnHitDetection);
                    if (!suppress)
                        console.Println($"Log On Hit: {hitLogger.drawOnHitDetection}");
                    return ConsoleLiteral.Of(hitLogger.drawOnHitDetection);
                case "onlyshooterdetection":
                    if (2 <= vars.Length)
                        hitLogger.onlyShooterDetection =
                            ConsoleParser.TryParseBoolean(vars[1], hitLogger.onlyShooterDetection);
                    if (!suppress)
                        console.Println($"Only Shooter Detection: {hitLogger.onlyShooterDetection}");
                    return ConsoleLiteral.Of(hitLogger.onlyShooterDetection);
                case "persisttime":
                    if (2 <= vars.Length)
                        hitLogger.linePersistTime = ConsoleParser.TryParseFloat(vars[1]);
                    if (!suppress)
                        console.Println($"Persist Time: {hitLogger.linePersistTime}");
                    return ConsoleLiteral.Of(hitLogger.linePersistTime);
                default:
                    console.PrintUsage(this);
                    return ConsoleLiteral.GetNone();
            }
            // ReSharper restore StringLiteralTypo
        }
    }
}
