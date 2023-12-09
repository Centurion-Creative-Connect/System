using System;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GameMatchHandler : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PrintableBase logger;
        private readonly DataDictionary _currentMatchEvents = new DataDictionary();

        private readonly DataDictionary _currentMatchStatistics = new DataDictionary();

        private readonly DataDictionary _totalMatchLog = new DataDictionary();
        private readonly DataDictionary _totalStatistics = new DataDictionary();

        // [UdonSynced] [FieldChangeCallback(nameof(CurrentMatchGuidString))]
        private string _currentMatchGuid = Guid.Empty.ToString("D");

        private bool _hasLoggedMatch;
        // [UdonSynced] [FieldChangeCallback(nameof(IsInMatch))]
        private bool _isInMatch;
        private DateTime _matchStartTime;

        private string CurrentMatchGuidString
        {
            get => _currentMatchGuid;
            set => CurrentMatchGuid = Guid.Parse(value);
        }

        private Guid CurrentMatchGuid
        {
            get => Guid.Parse(_currentMatchGuid);
            set
            {
                if (CurrentMatchGuid == value) return;

                _currentMatchStatistics.Clear();
                _currentMatchEvents.Clear();

                _currentMatchGuid = value.ToString("D");
                _hasLoggedMatch = false;
            }
        }

        private bool IsInMatch
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

                _totalMatchLog.Add(CurrentMatchGuid.ToString("D"), matchDataDict);
            }
        }

        private void Start()
        {
            playerManager.SubscribeCallback(this);
        }

        public void BeginMatch()
        {
            if (_isInMatch)
            {
                logger.LogError("[Match] Could not begin match: already running");
                return;
            }

            CurrentMatchGuid = Guid.NewGuid();
            IsInMatch = true;
            Sync();
        }

        public void EndMatch()
        {
            if (!_isInMatch)
            {
                logger.LogError("[Match] Could not end match: not running");
                return;
            }

            IsInMatch = false;
            Sync();
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

            Debug.Log("====== BEGIN MATCH LOG ======");
            Debug.Log(jsonLog.String);
            Debug.Log("====== END MATCH LOG ======");
        }

        public override void OnKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!_isInMatch) return;

            var dict = new DataDictionary();
            dict.Add("type", "killed");
            dict.Add("data", victim.LastHitData.ToDictionary());

            AddMatchEventLog(dict);

            IncrementDeath(_currentMatchStatistics, victim);
            IncrementKill(_currentMatchStatistics, attacker);

            IncrementDeath(_totalStatistics, victim);
            IncrementKill(_totalStatistics, attacker);
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            if (!_isInMatch) return;

            var dict = new DataDictionary();
            dict.Add("type", "revived");

            var dataDict = new DataDictionary();
            dataDict.Add("id", player.PlayerId);
            dataDict.Add("name", VRCPlayerApi.GetPlayerById(player.PlayerId).SafeGetDisplayName());
            dataDict.Add("time", Networking.GetNetworkDateTime().ToString("O"));

            dict.Add("data", dataDict);

            AddMatchEventLog(dict);
        }

        private void Sync()
        {
            // if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            // RequestSerialization();
        }

        private void AddMatchEventLog(DataDictionary dict)
        {
            var key = Guid.NewGuid().ToString("D");
            while (_currentMatchEvents.ContainsKey(key))
            {
                key = Guid.NewGuid().ToString("D");
            }

            _currentMatchEvents.Add(key, dict);
        }

        private static void IncrementDeath(DataDictionary statsDict, PlayerBase playerBase)
        {
            if (EnsureStatsTableExist(statsDict, playerBase)) return;
            var key = playerBase.VrcPlayer.SafeGetDisplayName();
            var playerTable = statsDict[key].DataDictionary;

            playerTable["death"] = playerTable["death"].Int + 1;
            if (playerTable["killStreak"].Int > playerTable["highestKillStreak"].Int)
                playerTable["highestKillStreak"] = playerTable["killStreak"];
            playerTable["killStreak"] = 0;

            statsDict[key] = playerTable;
        }

        private static void IncrementKill(DataDictionary statsDict, PlayerBase playerBase)
        {
            if (EnsureStatsTableExist(statsDict, playerBase)) return;
            var key = playerBase.VrcPlayer.SafeGetDisplayName();
            var playerTable = statsDict[key].DataDictionary;

            playerTable["kill"] = playerTable["kill"].Int + 1;
            playerTable["killStreak"] = playerTable["killStreak"].Int + 1;
            if (playerTable["killStreak"].Int >= playerTable["highestKillStreak"].Int)
                playerTable["highestKillStreak"] = playerTable["killStreak"];

            statsDict[key] = playerTable;
        }

        private static bool EnsureStatsTableExist(DataDictionary statsDict, PlayerBase playerBase)
        {
            var key = playerBase.VrcPlayer.SafeGetDisplayName("null");
            if (key == "null") return true;

            if (statsDict.ContainsKey(key)) return false;

            var d = new DataDictionary();
            d.Add("kill", 0);
            d.Add("death", 0);
            d.Add("killStreak", 0);
            d.Add("highestKillStreak", 0);
            d.Add("score", 0);

            statsDict.Add(key, d);
            return false;
        }
    }
}