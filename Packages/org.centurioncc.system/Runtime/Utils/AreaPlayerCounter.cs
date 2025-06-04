using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AreaPlayerCounter : PlayerManagerCallbackBase
    {
        private const int MinTeamId = 0;
        private const int MaxTeamId = short.MaxValue;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        private int _eventCallbackCount;

        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[0];
        private DataDictionary _playersInAreaDict = new DataDictionary();

        [PublicAPI]
        public int TotalPlayerCount => _playersInAreaDict.Count;

        [PublicAPI]
        public int[] TeamPlayerCount { get; private set; } = new int[MaxTeamId];

        private void Start()
        {
            playerManager.Subscribe(this);
        }

        [PublicAPI]
        public void SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.AddBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        [PublicAPI]
        public void UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            CallbackUtil.RemoveBehaviour(behaviour, ref _eventCallbackCount, ref _eventCallbacks);
        }

        [PublicAPI]
        public void Recount()
        {
            TeamPlayerCount = new int[short.MaxValue];

            var playersInArea = _playersInAreaDict.GetKeys().ToArray();
            foreach (var player in playersInArea)
                IncrementTeamCount(((PlayerBase)player.Reference).TeamId);
        }

        [PublicAPI]
        public PlayerBase[] GetPlayersInArea()
        {
            // Reconstruct PlayerBase array based on DataDictionary keys because it's easier to use
            var playersInAreaTokens = _playersInAreaDict.GetKeys().ToArray();
            var playerBaseArr = new PlayerBase[playersInAreaTokens.Length];
            for (var i = 0; i < playerBaseArr.Length; i++)
                playerBaseArr[i] = (PlayerBase)playersInAreaTokens[i].Reference;

            return playerBaseArr;
        }

        [PublicAPI]
        public void GetPlayerCount(out int allPlayersCount, out int redPlayerCount, out int yellowPlayerCount)
        {
            allPlayersCount = _playersInAreaDict.Count;
            redPlayerCount = TeamPlayerCount[1];
            yellowPlayerCount = TeamPlayerCount[2];
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            Debug.Log($"[PlayerCounter-{name}] OnPlayerTriggerEnter: {player.displayName}");

            var playerBase = playerManager.GetPlayerById(player.playerId);
            if (playerBase == null) return;

            var key = new DataToken(playerBase);
            if (!_playersInAreaDict.ContainsKey(key))
            {
                // For the first time player enters collider
                _playersInAreaDict.Add(key, 0);
                IncrementTeamCount(playerBase.TeamId);
            }

            var triggerCount = _playersInAreaDict[key].Int + 1;
            _playersInAreaDict[key] = triggerCount;
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            Debug.Log($"[PlayerCounter-{name}] OnPlayerTriggerExit: {player.displayName}");

            var playerBase = playerManager.GetPlayerById(player.playerId);
            if (playerBase == null) return;

            var key = new DataToken(playerBase);
            if (!_playersInAreaDict.ContainsKey(key)) return;

            var triggerCount = _playersInAreaDict[key].Int - 1;
            if (triggerCount <= 0)
            {
                // For the last time player exits collider
                _playersInAreaDict.Remove(key);
                DecrementTeamCount(playerBase.TeamId);
            }
            else
            {
                _playersInAreaDict[key] = triggerCount;
            }
        }

        public override void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!_playersInAreaDict.ContainsKey(player)) return;

            DecrementTeamCount(oldTeam);
            IncrementTeamCount(player.TeamId);
        }

        public override void OnPlayerAdded(PlayerBase player)
        {
            if (!_playersInAreaDict.ContainsKey(player)) return;

            _playersInAreaDict.Remove(player);
            DecrementTeamCount(player.TeamId);
        }

        private void DecrementTeamCount(int id)
        {
            if (id >= MinTeamId && id <= MaxTeamId) --TeamPlayerCount[id];
            Invoke_CountChanged();
        }

        private void IncrementTeamCount(int id)
        {
            if (id >= MinTeamId && id <= MaxTeamId) ++TeamPlayerCount[id];
            Invoke_CountChanged();
        }

        private void Invoke_CountChanged()
        {
            for (var i = 0; i < _eventCallbackCount; i++)
            {
                var b = _eventCallbacks[i];
                if (b != null) b.SendCustomEvent("OnAreaPlayerCountChanged");
            }
        }
    }
}