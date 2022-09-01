using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CenturionCC.System.SteelChallenge
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

        [UdonSynced]
        private long[] _bestTimeRecords;
        [UdonSynced]
        private string _bestTimeRecordUsers;

        private TimeSpan _currentBestTime = new TimeSpan(0);
        private TimeSpan _currentLastTime = new TimeSpan(0);

        private void Start()
        {
            if (Networking.IsMaster)
            {
                _bestTimeRecords = new long[maxRecords];
                _bestTimeRecordUsers = "";
                _bestTimeRecords[0] = defaultBestTimeRecord;
                _bestTimeRecordUsers = defaultBestTimeUser;
                RequestSerialization();
                UpdateInfoDisplay();
                UpdateLeaderboardDisplay();
            }
        }

        public override void OnDeserialization()
        {
            Debug.Log("[SteelChallengeLeaderboard] Received an updated data");
            UpdateInfoDisplay();
            UpdateLeaderboardDisplay();
        }

        public void AddRecord(TimeSpan time, VRCPlayerApi api)
        {
            if (api.isLocal)
            {
                if (_currentBestTime.Ticks == 0 || time.Ticks < _currentBestTime.Ticks)
                    _currentBestTime = time;
                _currentLastTime = time;
                UpdateInfoDisplay();
            }

            // Get index of where to insert
            var insertionIndex = -1;
            for (var i = 0; i < _bestTimeRecords.Length; i++)
            {
                if (_bestTimeRecords[i] != 0 && time.Ticks >= _bestTimeRecords[i]) continue;
                insertionIndex = i;
                break;
            }

            if (insertionIndex == -1) return;
            var users = GetLeaderboardUsers();

            // Swap from end of array to make space for insertion
            for (var i = _bestTimeRecords.Length - 1; i > insertionIndex; i--)
            {
                _bestTimeRecords[i] = _bestTimeRecords[i - 1];
                users[i] = users[i - 1];
            }

            _bestTimeRecords[insertionIndex] = time.Ticks;
            users[insertionIndex] = api.displayName;
            SetLeaderboardUsers(users);

            UpdateLeaderboardDisplay();
            Debug.Log(
                $"[SteelChallengeLeaderboard] Added record of {api.displayName} as {time.Ticks} at {insertionIndex}");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private void UpdateInfoDisplay()
        {
            lastTimeText.text = string.Format(timeFormat, _currentLastTime.ToString(@"s\.ff"));
            bestTimeText.text = string.Format(timeFormat, _currentBestTime.ToString(@"s\.ff"));
        }

        private void UpdateLeaderboardDisplay()
        {
            var text = "";
            var users = GetLeaderboardUsers();
            for (var i = 0; i < _bestTimeRecords.Length; i++)
                text += string.Format(leaderboardFormat, i + 1, users[i],
                    new TimeSpan(_bestTimeRecords[i]).ToString(@"s\.ff"));
            leaderboardText.text = text;
        }

        private string[] GetLeaderboardUsers()
        {
            var result = new string[maxRecords];
            _bestTimeRecordUsers.Split('\n').CopyTo(result, 0);
            return result;
        }

        private void SetLeaderboardUsers(string[] users)
        {
            _bestTimeRecordUsers = string.Join("\n", users);
        }
    }
}