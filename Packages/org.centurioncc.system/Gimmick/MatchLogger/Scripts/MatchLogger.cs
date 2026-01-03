using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.MatchLogger
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MatchLogger : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PrintableBase logger;

        private readonly DataDictionary _currentMatchEvents = new DataDictionary();
        private readonly DataDictionary _currentMatchStatistics = new DataDictionary();
        private readonly DataDictionary _totalMatchLog = new DataDictionary();
        private readonly DataDictionary _totalStatistics = new DataDictionary();

        private string _currentMatchGuid = Guid.Empty.ToString("D");
        private bool _hasLoggedMatch;

        private bool _isInMatch;
        private DateTime _matchEndTime;
        private DateTime _matchStartTime;

        public string CurrentMatchGuidString
        {
            get => _currentMatchGuid;
            private set => CurrentMatchGuid = Guid.Parse(value);
        }

        public Guid CurrentMatchGuid
        {
            get => Guid.Parse(_currentMatchGuid);
            private set
            {
                if (CurrentMatchGuid == value) return;

                _currentMatchGuid = value.ToString("D");
                _hasLoggedMatch = false;
            }
        }

        public bool IsInMatch
        {
            get => _isInMatch;
            set
            {
                if (_isInMatch == value) return;

                _isInMatch = value;
                if (value)
                {
                    _matchStartTime = Networking.GetNetworkDateTime();
                    return;
                }

                if (_hasLoggedMatch) return;

                _hasLoggedMatch = true;

                var matchDataDict = new DataDictionary();
                matchDataDict.Add("gameMode", "Unknown");
                matchDataDict.Add("start", _matchStartTime.ToString("O"));
                matchDataDict.Add("end", Networking.GetNetworkDateTime().ToString("O"));
                matchDataDict.Add("statistics", _currentMatchStatistics.DeepClone());
                matchDataDict.Add("events", _currentMatchEvents.DeepClone());

                _totalMatchLog.SetValue(CurrentMatchGuid.ToString("D"), matchDataDict);
            }
        }

        public DateTime MatchStartTime
        {
            get => _matchStartTime;
            private set => _matchStartTime = value;
        }

        public DateTime MatchEndTime
        {
            get => _matchEndTime;
            private set => _matchEndTime = value;
        }

        public string CurrentGameMode { get; private set; }

        private void Start()
        {
            playerManager.Subscribe(this);
            MatchStartTime = Networking.GetNetworkDateTime();
            MatchEndTime = Networking.GetNetworkDateTime();
        }

        public void BeginMatch(string gameMode)
        {
            if (IsInMatch)
            {
                logger.LogError("[Match] Could not begin match: already running");
                return;
            }

            // clears all statistics
            _currentMatchStatistics.Clear();
            _currentMatchEvents.Clear();

            Internal_BeginMatch(gameMode, Networking.GetNetworkDateTime());
        }

        public void LateBeginMatch(string gameMode)
        {
            if (IsInMatch)
            {
                logger.LogError("[Match] Could not begin match: already running");
                return;
            }

            Internal_BeginMatch(gameMode, _matchEndTime);
        }

        private void Internal_BeginMatch(string gameMode, DateTime matchStartTime)
        {
            // adds all players into statistics
            var allPlayers = playerManager.GetPlayers();
            AddPlayersToStats(_totalStatistics, allPlayers);
            AddPlayersToStats(_currentMatchStatistics, allPlayers);

            // set new guid and mark current state as in match
            CurrentMatchGuid = Guid.NewGuid();
            CurrentGameMode = gameMode;
            IsInMatch = true;
            MatchStartTime = matchStartTime;

            // update current weapon information
            UpdateMatch();
        }

        public void UpdateMatch()
        {
            if (!IsInMatch)
            {
                logger.LogError("[Match] Could not update match: not running");
                return;
            }

            UpdateWeapons(_totalStatistics, _currentMatchStatistics);
        }

        public void EndMatch()
        {
            if (!IsInMatch)
            {
                logger.LogError("[Match] Could not end match: not running");
                return;
            }

            MatchEndTime = Networking.GetNetworkDateTime();

            var allPlayers = playerManager.GetPlayers();
            AddPlayersToStats(_totalStatistics, allPlayers);
            AddPlayersToStats(_currentMatchStatistics, allPlayers);
            UpdateMatch();

            var matchDataDict = new DataDictionary();
            matchDataDict.Add("gameMode", CurrentGameMode);
            matchDataDict.Add("start", MatchStartTime.ToString("O"));
            matchDataDict.Add("end", MatchEndTime.ToString("O"));
            matchDataDict.Add("statistics", _currentMatchStatistics.DeepClone());
            matchDataDict.Add("events", _currentMatchEvents.DeepClone());

            _totalMatchLog.Add(CurrentMatchGuid.ToString("D"), matchDataDict);
            IsInMatch = false;
        }

        public void PrintMatchLog()
        {
            var log = new DataDictionary();
            log.Add("totalStatistics", _totalStatistics);
            log.Add("matches", _totalMatchLog);

            if (!VRCJson.TrySerializeToJson(log, JsonExportType.Beautify, out var jsonLog))
            {
                logger.LogError($"[Match] Could not print match log: {jsonLog.Error}");
                return;
            }

            Debug.Log($"====== BEGIN MATCH LOG ======\n{jsonLog}\n====== END MATCH LOG ======");
        }

        public DataList GetTotalRecordedDisplayNames()
        {
            return _totalStatistics.GetKeys();
        }

        public DataList GetCurrentRecordedDisplayNames()
        {
            return _currentMatchStatistics.GetKeys();
        }

        public bool GetCurrentStatisticOf(string displayName, out DataToken value)
        {
            return _currentMatchStatistics.TryGetValue(displayName, out value);
        }

        public bool GetTotalStatisticOf(string displayName, out DataToken value)
        {
            return _totalStatistics.TryGetValue(displayName, out value);
        }

        public void AddMatchEventLog(string type, DataDictionary data)
        {
            string key;
            do
            {
                key = Guid.NewGuid().ToString("D");
            } while (_currentMatchEvents.ContainsKey(key));

            var dict = new DataDictionary();
            dict.Add("type", type);
            dict.Add("data", data);
            _currentMatchEvents.Add(key, dict);
        }

        public void IncrementKill(PlayerBase player, string weapon, float distance)
        {
            IncrementKill(_totalStatistics, player, weapon, distance);
            IncrementKill(_currentMatchStatistics, player, weapon, distance);
        }

        public void IncrementDeath(PlayerBase player)
        {
            IncrementDeath(_totalStatistics, player);
            IncrementDeath(_currentMatchStatistics, player);
        }

        public void UpdateTeam(PlayerBase player)
        {
            UpdateTeam(_totalStatistics, player);
            UpdateTeam(_currentMatchStatistics, player);
        }

        public void EnsureStatsTableExist(PlayerBase player)
        {
            EnsureStatsTableExist(_totalStatistics, player);
            EnsureStatsTableExist(_currentMatchStatistics, player);
        }

        private void UpdateWeapons(params DataDictionary[] dictionaries)
        {
            foreach (var managedGun in gunManager.GetGunInstances())
            {
                if (managedGun == null || managedGun.CurrentHolder == null || !managedGun.CurrentHolder.IsValid()) continue;
                foreach (var dict in dictionaries)
                {
                    AddWeapon(
                        dict,
                        playerManager.GetPlayerById(managedGun.CurrentHolder.playerId),
                        managedGun.WeaponName
                    );
                }
            }
        }

        #region StaticMethods
        private static void IncrementDeath(DataDictionary statsDict, PlayerBase playerBase)
        {
            if (EnsureStatsTableExist(statsDict, playerBase)) return;
            var key = playerBase.VrcPlayer.SafeGetDisplayName();
            var playerTable = statsDict[key].DataDictionary;

            playerTable["team"] = playerBase.TeamId;
            playerTable["death"] = playerTable["death"].Int + 1;
            if (playerTable["killStreak"].Int > playerTable["highestKillStreak"].Int)
                playerTable["highestKillStreak"] = playerTable["killStreak"];
            playerTable["killStreak"] = 0;
        }

        private static void IncrementKill(DataDictionary statsDict, PlayerBase playerBase, string weapon,
                                          float distance)
        {
            if (EnsureStatsTableExist(statsDict, playerBase)) return;
            var key = playerBase.VrcPlayer.SafeGetDisplayName();
            var playerTable = statsDict[key].DataDictionary;

            playerTable["team"] = playerBase.TeamId;
            playerTable["kill"] = playerTable["kill"].Int + 1;
            playerTable["killStreak"] = playerTable["killStreak"].Int + 1;
            if (playerTable["killStreak"].Int >= playerTable["highestKillStreak"].Int)
                playerTable["highestKillStreak"] = playerTable["killStreak"];
            if (!playerTable["weapons"].DataList.Contains(weapon))
                playerTable["weapons"].DataList.Add(weapon);
            if (playerTable["longestShot"].Float < distance)
                playerTable["longestShot"] = distance;
            playerTable["recentWeapon"] = weapon;
            playerTable["score"] = playerTable["score"].Int + 100 * playerTable["killStreak"].Int;
        }

        private static void UpdateTeam(DataDictionary statsDict, PlayerBase playerBase)
        {
            if (EnsureStatsTableExist(statsDict, playerBase)) return;
            var key = playerBase.VrcPlayer.SafeGetDisplayName();
            var playerTable = statsDict[key].DataDictionary;

            playerTable["team"] = playerBase.TeamId;
        }

        private static void AddWeapon(DataDictionary statsDict, PlayerBase playerBase, string weapon)
        {
            if (EnsureStatsTableExist(statsDict, playerBase)) return;
            var key = playerBase.VrcPlayer.SafeGetDisplayName();
            var playerTable = statsDict[key].DataDictionary;
            playerTable["recentWeapon"] = weapon;
            if (playerTable["weapons"].DataList.Contains(weapon)) return;
            playerTable["weapons"].DataList.Add(weapon);
        }

        private static void AddPlayersToStats(DataDictionary statsDict, PlayerBase[] playerBases)
        {
            foreach (var playerBase in playerBases) EnsureStatsTableExist(statsDict, playerBase);
        }

        private static bool EnsureStatsTableExist(DataDictionary statsDict, PlayerBase playerBase)
        {
            var key = playerBase.VrcPlayer.SafeGetDisplayName("null");
            if (key == "null") return true;

            if (statsDict.ContainsKey(key)) return false;

            var d = new DataDictionary();
            d.Add("displayName", playerBase.VrcPlayer.SafeGetDisplayName());
            d.Add("kill", 0);
            d.Add("death", 0);
            d.Add("killStreak", 0);
            d.Add("highestKillStreak", 0);
            d.Add("score", 0);
            d.Add("team", playerBase.TeamId);
            d.Add("recentWeapon", "");
            d.Add("longestShot", 0.0F);
            d.Add("weapons", new DataList());

            statsDict.Add(key, d);
            return false;
        }
        #endregion
    }
}
