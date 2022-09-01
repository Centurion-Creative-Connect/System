using CenturionCC.System.Utils;
using DerpyNewbie.Logger;
using UdonSharp;

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

        private EventLogger _eventLogger;

        private void Start()
        {
            _eventLogger = GameManagerHelper.GetGameManager().eventLogger;
        }

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
                        _eventLogger.PersistentHitLogData,
                        HitLogSuffix);

                    console.Println(msg);
                    return msg;
                }
                case "shot":
                {
                    var msg = string.Join("\n",
                        ShotLogPrefix,
                        _eventLogger.ShotLogData,
                        ShotLogSuffix);

                    console.Println(msg);
                    return msg;
                }
                case "all":
                {
                    var msg = string.Join("\n",
                        HitLogPrefix,
                        _eventLogger.PersistentHitLogData,
                        HitLogSuffix,
                        ShotLogPrefix,
                        _eventLogger.ShotLogData,
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
                    _eventLogger.ClearHitLog();
                    console.Println("<color=green>Cleared hit log</color>");
                    break;
                }
                case "shot":
                {
                    _eventLogger.ClearShotLog();
                    console.Println("<color=green>Cleared shot log</color>");
                    break;
                }
                case "all":
                {
                    _eventLogger.ClearHitLog();
                    _eventLogger.ClearShotLog();
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
                var request = ConsoleParser.TryParseBoolean(args[1], _eventLogger.ShouldVisualizeOnLog);
                _eventLogger.ShouldVisualizeOnLog = request;
            }

            console.Println($"Should Visualize On Log: {_eventLogger.ShouldVisualizeOnLog}");
            return _eventLogger.ShouldVisualizeOnLog;
        }

        public override string Label => "EventLogger";
        public override string Usage =>
            "<command> <write|clear> <hit|shot> or <command> <writeVisual|clearVisual> or <command> <visualizeOnHit> [true|false]";
        public override string Description => "Manipulate logs produced in EventLogger";

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
                    _eventLogger.Visualize();
                    return ConsoleLiteral.GetNone();
                case "cv":
                case "clearvisual":
                    _eventLogger.RemoveVisualization();
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