using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
namespace CenturionCC.System.Editor.EditorWindow
{
    public class ResolverLogAnalyzer : UnityEditor.EditorWindow
    {

        public enum LogDisplayType
        {
            FullResolverLog,
            WeaponStatisticLog,
            PlayerStatisticLog
        }
        private readonly Color32 _evenColor = new Color32(30, 30, 30, 255);
        private readonly GUILayoutOption _longWidth = GUILayout.Width(170);
        private readonly Color32 _oddColor = new Color32(35, 35, 35, 255);
        private readonly GUILayoutOption _pidWidth = GUILayout.Width(70);
        private readonly GUILayoutOption _strWidth = GUILayout.Width(120);
        private int _lastLogStringHash;
        private int _lastParseErrorType;

        private string _logString = "";
        private LogDisplayType _logType = LogDisplayType.FullResolverLog;
        private DataToken _parsedDataToken;
        private Dictionary<int, PlayerStatistic> _playerStatistics;
        private Vector2 _scrollPos;
        private Dictionary<string, WeaponStatistic> _weaponStatistics;

        private void OnEnable()
        {
            ParseLog();
        }

        private void OnGUI()
        {
            _logString = EditorGUILayout.TextArea(_logString, GUILayout.Height(50));
            var hasLogStringUpdated = false;
            var hash = _logString.GetHashCode();
            if (_lastLogStringHash != hash || GUILayout.Button("Refresh"))
            {
                hasLogStringUpdated = true;
                _lastLogStringHash = hash;
            }

            if (hasLogStringUpdated)
            {
                _lastParseErrorType = ParseLog();
            }

            if (string.IsNullOrWhiteSpace(_logString))
            {
                EditorGUILayout.HelpBox("Please input a resolver log", MessageType.Info);
                return;
            }

            switch (_lastParseErrorType)
            {
                case 0:
                    _logType = (LogDisplayType)EditorGUILayout.EnumPopup(_logType);
                    switch (_logType)
                    {
                        default:
                        case LogDisplayType.FullResolverLog:
                            DrawLog();
                            break;
                        case LogDisplayType.WeaponStatisticLog:
                            DrawWeaponStatistics();
                            break;
                        case LogDisplayType.PlayerStatisticLog:
                            DrawPlayerStatistics();
                            break;
                    }

                    return;
                default:
                    EditorGUILayout.HelpBox($"Could not parse string: {(DataError)_lastParseErrorType}",
                        MessageType.Error);
                    return;
            }
        }

        [MenuItem("Centurion System/Utils/Resolver Log Analyzer")]
        public static void Open()
        {
            var window = GetWindow<ResolverLogAnalyzer>();
            window.titleContent.text = "Resolver Log Analyzer";
            window.Show();
        }

        private int ParseLog()
        {
            var success = VRCJson.TryDeserializeFromJson(_logString, out var result);
            if (!success)
            {
                return (int)result.Error;
            }

            _parsedDataToken = result;

            _weaponStatistics = new Dictionary<string, WeaponStatistic>();
            _playerStatistics = new Dictionary<int, PlayerStatistic>();

            Vector3 FromDict(DataDictionary vecDict)
            {
                return new Vector3(
                    (float)vecDict["x"].Double,
                    (float)vecDict["y"].Double,
                    (float)vecDict["z"].Double
                );
            }

            void AddWeaponStatistics(DataDictionary value)
            {
                var dmgData = value["damageData"].DataDictionary;
                var weaponName = dmgData["weaponName"].String;
                if (!_weaponStatistics.ContainsKey(weaponName))
                    _weaponStatistics.Add(weaponName, new WeaponStatistic());

                var weaponStats = _weaponStatistics[weaponName];
                weaponStats.HitsRecorded++;

                var hitPos = FromDict(dmgData["hitPosition"].DataDictionary);
                var activatedPos = FromDict(dmgData["activatedPosition"].DataDictionary);

                var distance = Vector3.Distance(hitPos, activatedPos);

                if (weaponStats.LongestShotDistance < distance)
                    weaponStats.LongestShotDistance = distance;

                if (weaponStats.ShortestShotDistance > distance)
                    weaponStats.ShortestShotDistance = distance;

                weaponStats.SumOfDistance += distance;
            }

            void AddPlayerStatistics(DataDictionary value)
            {
                var dmgData = value["damageData"].DataDictionary;
                var hitPos = FromDict(dmgData["hitPosition"].DataDictionary);
                var activatedPos = FromDict(dmgData["activatedPosition"].DataDictionary);
                var distance = Vector3.Distance(hitPos, activatedPos);

                var attackerId = (int)value["attackerId"].Double;
                if (!_playerStatistics.ContainsKey(attackerId))
                    _playerStatistics.Add(attackerId, new PlayerStatistic());
                var attackerStats = _playerStatistics[attackerId];

                attackerStats.Kill++;
                if (attackerStats.LongestKillDistance < distance)
                    attackerStats.LongestKillDistance = distance;
                if (attackerStats.ShortestKillDistance > distance)
                    attackerStats.ShortestKillDistance = distance;
                attackerStats.SumOfKillDistance += distance;

                var weaponName = dmgData["weaponName"].String;
                if (!attackerStats.UsedWeaponCount.ContainsKey(weaponName))
                    attackerStats.UsedWeaponCount.Add(weaponName, 0);
                attackerStats.UsedWeaponCount[weaponName]++;

                var victimId = (int)value["victimId"].Double;
                if (!_playerStatistics.ContainsKey(victimId))
                    _playerStatistics.Add(victimId, new PlayerStatistic());
                var victimStats = _playerStatistics[victimId];

                victimStats.Death++;
                if (victimStats.LongestDeathDistance < distance)
                    victimStats.LongestDeathDistance = distance;
                if (victimStats.ShortestDeathDistance > distance)
                    victimStats.ShortestDeathDistance = distance;
                victimStats.SumOfDeathDistance += distance;
            }

            var dict = _parsedDataToken.DataDictionary;
            DataDictionary lastCheckedDict = null;
            foreach (var kvp in dict)
            {
                var vDict = kvp.Value.DataDictionary;
                if (CheckDuplicate(lastCheckedDict, vDict))
                    continue;

                if (vDict["result"] != "Hit")
                    continue;

                AddWeaponStatistics(vDict);
                AddPlayerStatistics(vDict);

                lastCheckedDict = vDict;
            }

            return 0;
        }

        private void DrawLog()
        {
            if (_lastParseErrorType != 0)
                return;


            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Order", _pidWidth);
                GUILayout.Label("EventId", _longWidth);
                GUILayout.Label("AttackerId", _pidWidth);
                GUILayout.Label("VictimId", _pidWidth);
                GUILayout.Label("Request", _strWidth);
                GUILayout.Label("Result", _strWidth);
                GUILayout.Label("Type", _strWidth);
                GUILayout.Label("Activated Time", _strWidth);
                GUILayout.Label("Time Took To Hit", _strWidth);
                GUILayout.Label("Hit Time", _strWidth);
                GUILayout.Label("Diff Since Last Hit", _strWidth);
                GUILayout.Label("Weapon", _longWidth);
            }

            var count = 0;
            var duplicateCount = 0;

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)))
            {
                _scrollPos = scrollView.scrollPosition;
                var dict = _parsedDataToken.DataDictionary;
                var duplicateColor = new Color32(80, 30, 30, 255);
                var richTextLabel = new GUIStyle(EditorStyles.label) { richText = true };
                var lastHitTime = DateTime.MinValue;

                DataDictionary lastDict = null;

                foreach (var kvp in dict)
                {
                    using (var scope = new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.DrawRect(scope.rect, count % 2 == 0 ? _evenColor : _oddColor);
                        if (CheckDuplicate(lastDict, kvp.Value.DataDictionary))
                        {
                            ++duplicateCount;
                            EditorGUI.DrawRect(scope.rect, duplicateColor);
                        }

                        GUILayout.Label($"{count}", _pidWidth);
                        GUILayout.Label(kvp.Key.String, _longWidth);
                        var valueDict = kvp.Value.DataDictionary;

                        GUILayout.Label($"{valueDict["attackerId"].Double:F0}", _pidWidth);
                        GUILayout.Label($"{valueDict["victimId"].Double:F0}", _pidWidth);

                        GUILayout.Label($"{valueDict["request"].String}", _strWidth);
                        GUILayout.Label($"{valueDict["result"].String}", _strWidth);
                        GUILayout.Label($"{valueDict["type"].String}", _strWidth);

                        var dmgData = valueDict["damageData"].DataDictionary;
                        var activatedTime =
                            TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(dmgData["activatedTime"].String));
                        var hitTime = TimeZone.CurrentTimeZone.ToLocalTime(DateTime.Parse(dmgData["hitTime"].String));

                        GUILayout.Label($"{activatedTime:T}", _strWidth);
                        GUILayout.Label($"{ColoredDiff(hitTime.Subtract(activatedTime))}", richTextLabel, _strWidth);

                        GUILayout.Label($"{hitTime:T}", _strWidth);
                        GUILayout.Label($"{ColoredDiff(hitTime.Subtract(lastHitTime))}", richTextLabel, _strWidth);

                        GUILayout.Label($"{dmgData["weaponName"]}", _longWidth);

                        lastHitTime = hitTime;
                        lastDict = valueDict;
                    }

                    ++count;
                }
            }

            EditorGUILayout.LabelField($"Total: {count}, Duplicates: {duplicateCount}");
        }

        private void DrawWeaponStatistics()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Weapon Name", _longWidth);
                GUILayout.Label("Hits", _pidWidth);
                GUILayout.Label("Avg Shot Distance", _longWidth);
                GUILayout.Label("Longest Shot Distance", _longWidth);
                GUILayout.Label("Shortest Shot Distance", _longWidth);
            }

            var totalHits = 0;
            var totalDistance = 0F;

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)))
            {
                _scrollPos = scrollView.scrollPosition;
                var count = 0;
                foreach (var kvp in _weaponStatistics)
                {
                    using (var scope = new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.DrawRect(scope.rect, count % 2 == 0 ? _evenColor : _oddColor);

                        GUILayout.Label(kvp.Key, _longWidth);
                        GUILayout.Label($"{kvp.Value.HitsRecorded}", _pidWidth);
                        GUILayout.Label($"{kvp.Value.SumOfDistance / kvp.Value.HitsRecorded:F2}m", _longWidth);
                        GUILayout.Label($"{kvp.Value.LongestShotDistance:F2}m", _longWidth);
                        GUILayout.Label($"{kvp.Value.ShortestShotDistance:F2}m", _longWidth);

                        totalHits += kvp.Value.HitsRecorded;
                        totalDistance += kvp.Value.SumOfDistance;
                        ++count;
                    }
                }
            }

            EditorGUILayout.LabelField($"Total Hits: {totalHits}, Total Avg Distance: {totalDistance / totalHits:F2}m");
        }

        private void DrawPlayerStatistics()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Player Id", _pidWidth);
                GUILayout.Label("Kills", _pidWidth);
                GUILayout.Label("Deaths", _pidWidth);
                GUILayout.Label("Avg Kill Distance", _longWidth);
                GUILayout.Label("Avg Death Distance", _longWidth);
                GUILayout.Label("Most Killed Weapon", _longWidth);
                GUILayout.Label("Weapon Kills", _pidWidth);
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)))
            {
                _scrollPos = scrollView.scrollPosition;
                var count = 0;
                foreach (var kvp in _playerStatistics.OrderBy(pair => pair.Key))
                {
                    using (var scope = new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.DrawRect(scope.rect, count % 2 == 0 ? _evenColor : _oddColor);

                        GUILayout.Label($"{kvp.Key}", _pidWidth);
                        GUILayout.Label($"{kvp.Value.Kill}", _pidWidth);
                        GUILayout.Label($"{kvp.Value.Death}", _pidWidth);
                        GUILayout.Label($"{kvp.Value.SumOfKillDistance / kvp.Value.Kill:F2}m", _longWidth);
                        GUILayout.Label($"{kvp.Value.SumOfDeathDistance / kvp.Value.Death:F2}m", _longWidth);

                        var mostKilledWeapon = kvp.Value.UsedWeaponCount.Aggregate((a, b) => a.Value > b.Value ? a : b);
                        GUILayout.Label($"{mostKilledWeapon.Key}", _longWidth);
                        GUILayout.Label($"{mostKilledWeapon.Value}", _pidWidth);

                        ++count;
                    }
                }
            }
        }

        private static bool CheckDuplicate(DataDictionary a, DataDictionary b)
        {
            if (a == null || b == null)
                return false;

            var sameAttacker = $"{a["attackerId"].Double:F0}" == $"{b["attackerId"].Double:F0}";
            var sameVictim = $"{a["victimId"].Double:F0}" == $"{b["victimId"].Double:F0}";
            var bothHit = a["result"] == "Hit" && b["result"] == "Hit";

            return sameAttacker && sameVictim && bothHit;
        }

        private static string ColoredDiff(TimeSpan diff)
        {
            const string pos = "<color=green>";
            const string neg = "<color=red>";
            const string neu = "<color=white>";
            const string end = "</color>";
            var diffSeconds = diff.TotalSeconds;

            return
                diffSeconds > 86000 ? "N/A" :
                diffSeconds > 0 ? $"{pos}+{diffSeconds}{end}" :
                diffSeconds < 0 ? $"{neg}{diffSeconds}{end}" :
                $"{neu}{diffSeconds}{end}";
        }

        private class WeaponStatistic
        {
            public int HitsRecorded { get; set; }
            public float LongestShotDistance { get; set; }
            public float ShortestShotDistance { get; set; } = float.MaxValue;
            public float SumOfDistance { get; set; }
        }

        private class PlayerStatistic
        {
            public int Kill { get; set; }
            public int Death { get; set; }

            public float LongestKillDistance { get; set; }
            public float ShortestKillDistance { get; set; }
            public float SumOfKillDistance { get; set; }

            public float LongestDeathDistance { get; set; }
            public float ShortestDeathDistance { get; set; }
            public float SumOfDeathDistance { get; set; }

            public Dictionary<string, int> UsedWeaponCount { get; set; } = new Dictionary<string, int>
            {
                { "None", 0 }
            };
        }
    }
}
