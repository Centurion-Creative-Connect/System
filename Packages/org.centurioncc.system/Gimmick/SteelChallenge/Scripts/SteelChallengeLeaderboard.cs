using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.SteelChallenge
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SteelChallengeLeaderboard : UdonSharpBehaviour
    {
        [SerializeField]
        private int maxRecords = 7;

        [SerializeField]
        private string leaderboardFormat = "{0}. {1,14}: {2,5}\n";

        [SerializeField]
        private string timeFormat = "{0}";

        [SerializeField]
        private Text leaderboardText;

        [SerializeField]
        private Text lastTimeText;

        [SerializeField]
        private Text bestTimeText;

        [SerializeField]
        private string defaultBestTimeUser;

        [SerializeField]
        private long defaultBestTimeRecord;

        private TimeSpan _currentBestTime = new TimeSpan(0);
        private TimeSpan _currentLastTime = new TimeSpan(0);

        private DataList _records = new DataList();

        [UdonSynced]
        private string _recordsJson;

        private void Start()
        {
            if (Networking.IsMaster)
            {
                if (defaultBestTimeUser != "" && defaultBestTimeRecord != 0)
                {
                    AddRecord(new TimeSpan(defaultBestTimeRecord), defaultBestTimeUser);
                }

                RequestSerialization();
            }
        }

        public override void OnPreSerialization()
        {
            if (!VRCJson.TrySerializeToJson(_records, JsonExportType.Minify, out var result))
            {
                Debug.LogError($"[SteelChallengeLeaderboard] Failed to serialize records: {result.Error}");
                return;
            }

            _recordsJson = result.String;

            UpdateInfoDisplay();
            UpdateLeaderboardDisplay();
        }

        public override void OnDeserialization()
        {
            Debug.Log("[SteelChallengeLeaderboard] Received an updated data");
            if (!VRCJson.TryDeserializeFromJson(_recordsJson, out var result))
            {
                Debug.LogError($"[SteelChallengeLeaderboard] Failed to deserialize records: {result.Error}");
                return;
            }

            _records = result.DataList;

            UpdateInfoDisplay();
            UpdateLeaderboardDisplay();
        }

        public void AddRecord(TimeSpan time, string weaponName, VRCPlayerApi api)
        {
            if (api.isLocal)
            {
                if (_currentBestTime.Ticks == 0 || time.Ticks < _currentBestTime.Ticks)
                    _currentBestTime = time;
                _currentLastTime = time;
                UpdateInfoDisplay();
            }

            AddRecord(time, $"{api.displayName} - {weaponName.Replace("BBBullet: ", "")}");
            UpdateLeaderboardDisplay();
            Debug.Log(
                $"[SteelChallengeLeaderboard] Added record of {api.displayName} as {time.Ticks}");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private void AddRecord(TimeSpan time, string username)
        {
            var newRecord = new DataDictionary();
            newRecord.Add("name", username);
            newRecord.Add("time", time.Ticks);

            var userRecord = GetCurrentRecordOf(username, out var userRank);
            var userRecordTime = new TimeSpan((long)userRecord["time"].Number);

            if (userRecordTime < time)
            {
                return;
            }

            _records.Remove(userRecord);

            var ranking = GetRankingOf(time);

            if (ranking >= _records.Count)
            {
                _records.Add(newRecord);
                return;
            }

            _records.Insert(ranking, newRecord);
        }

        private void UpdateInfoDisplay()
        {
            lastTimeText.text = string.Format(timeFormat, _currentLastTime.ToString(@"s\.ff"));
            bestTimeText.text = string.Format(timeFormat, _currentBestTime.ToString(@"s\.ff"));
        }

        private void UpdateLeaderboardDisplay()
        {
            var text = "";
            var tokens = _records.ToArray();
            var length = Mathf.Min(tokens.Length, maxRecords);
            for (var i = 0; i < length; i++)
            {
                var record = tokens[i].DataDictionary;
                var username = record["name"].String;
                var time = new TimeSpan((long)record["time"].Number);
                text += string.Format(leaderboardFormat, i + 1, username, time.ToString(@"s\.ff"));
            }

            leaderboardText.text = text;
        }

        private DataDictionary GetCurrentRecordOf(string displayName, out int index)
        {
            var recordTokens = _records.ToArray();
            for (index = 0; index < recordTokens.Length; index++)
            {
                var record = recordTokens[index].DataDictionary;
                if (record["name"] != displayName) continue;
                return record;
            }

            var newRecord = new DataDictionary();
            newRecord.Add("name", displayName);
            newRecord.Add("time", int.MaxValue);
            _records.Add(newRecord);
            return newRecord;
        }

        private int GetRankingOf(TimeSpan time)
        {
            var recordTokens = _records.ToArray();
            for (var i = 0; i < recordTokens.Length; i++)
            {
                var record = recordTokens[i].DataDictionary;
                var recordTime = new TimeSpan((long)record["time"].Number);
                if (recordTime < time) continue;
                return i;
            }

            return recordTokens.Length;
        }
    }
}
