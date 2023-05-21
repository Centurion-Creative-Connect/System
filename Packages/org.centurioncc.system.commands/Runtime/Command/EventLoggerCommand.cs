using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EventLoggerCommand : NewbieConsoleCommandHandler
    {
        private const string ShotLogPrefix = "-- Begin Shot Log --";
        private const string ShotLogSuffix = "-- End Shot Log --";

        private const string HitLogPrefix = "-- Begin Hit Log --";
        private const string HitLogSuffix = "-- End Hit Log --";

        private const string AvailableSubOptions = "<color=red>Available Sub Options:\n" +
                                                   "   hit\n" +
                                                   "   shot\n" +
                                                   "   all</color>";

        [SerializeField] [HideInInspector] [NewbieInject]
        private EventLogger eventLogger;

        public override string Label => "EventLogger";
        public override string Usage =>
            "<command> <write|clear> <hit|shot> or <command> <writeVisual|clearVisual> or <command> <visualizeOnHit> [true|false]";
        public override string Description => "Manipulate logs produced in EventLogger.";

        private string HandleWrite(NewbieConsole console, string[] args)
        {
            if (args.Length <= 1)
            {
                console.Println(AvailableSubOptions);
                return "";
            }

            switch (args[1].ToLower())
            {
                case "hit":
                {
                    var msg = string.Join("\n",
                        HitLogPrefix,
                        eventLogger.PersistentHitLogData,
                        HitLogSuffix);

                    console.Println(msg);
                    return msg;
                }
                case "shot":
                {
                    var msg = string.Join("\n",
                        ShotLogPrefix,
                        eventLogger.ShotLogData,
                        ShotLogSuffix);

                    console.Println(msg);
                    return msg;
                }
                case "all":
                {
                    var msg = string.Join("\n",
                        HitLogPrefix,
                        eventLogger.PersistentHitLogData,
                        HitLogSuffix,
                        ShotLogPrefix,
                        eventLogger.ShotLogData,
                        ShotLogSuffix);

                    console.Println(msg);
                    return msg;
                }
                default:
                {
                    console.Println(AvailableSubOptions);
                    return ConsoleLiteral.GetNone();
                }
            }
        }

        private void HandleClear(NewbieConsole console, string[] args)
        {
            if (args.Length <= 1)
            {
                console.Println(AvailableSubOptions);
                return;
            }

            switch (args[1].ToLower())
            {
                case "hit":
                {
                    eventLogger.ClearHitLog();
                    console.Println("<color=green>Cleared hit log</color>");
                    break;
                }
                case "shot":
                {
                    eventLogger.ClearShotLog();
                    console.Println("<color=green>Cleared shot log</color>");
                    break;
                }
                case "all":
                {
                    eventLogger.ClearHitLog();
                    eventLogger.ClearShotLog();
                    console.Println("<color=green>Cleared all event log</color>");
                    break;
                }
                default:
                {
                    console.Println(AvailableSubOptions);
                    break;
                }
            }
        }

        private bool HandleVisualizeOnHit(NewbieConsole console, string[] args)
        {
            if (args.Length >= 2)
            {
                var request = ConsoleParser.TryParseBoolean(args[1], eventLogger.ShouldVisualizeOnLog);
                eventLogger.ShouldVisualizeOnLog = request;
            }

            console.Println($"Should Visualize On Log: {eventLogger.ShouldVisualizeOnLog}");
            return eventLogger.ShouldVisualizeOnLog;
        }

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
                return ConsoleLiteral.GetNone();
            switch (vars[0].ToLower())
            {
                // ReSharper disable StringLiteralTypo
                case "w":
                case "write":
                    return ConsoleLiteral.Of(HandleWrite(console, vars));
                case "c":
                case "clear":
                    HandleClear(console, vars);
                    return ConsoleLiteral.GetNone();
                case "wv":
                case "writevisual":
                    eventLogger.Visualize();
                    return ConsoleLiteral.GetNone();
                case "cv":
                case "clearvisual":
                    eventLogger.RemoveVisualization();
                    return ConsoleLiteral.GetNone();
                case "v":
                case "visualizeonhit":
                    return ConsoleLiteral.Of(HandleVisualizeOnHit(console, vars));

                default:
                    console.Println("<color=red>Available Options:\n" +
                                    "   write\n" +
                                    "   clear\n" +
                                    "   writeVisual\n" +
                                    "   clearVisual\n" +
                                    "   visualizeOnHit</color>");
                    return ConsoleLiteral.GetNone();
                // ReSharper restore StringLiteralTypo
            }
        }
    }
}