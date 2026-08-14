using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using NotImplementedException = System.NotImplementedException;
namespace CenturionCC.System.Gimmick.AreaPlayerCounter
{
    public abstract class AreaPlayerCounterCallback : UdonSharpBehaviour
    {
        public abstract void OnAreaPlayerCountChanged();
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AreaPlayerCounter : PlayerAreaBase
    {
        private const int MinTeamId = 0;
        private const int MaxTeamId = short.MaxValue;

        [SerializeField] [NewbieInject(SearchScope.Children)] [HideInInspector]
        private Collider[] colliders;
        private readonly DataDictionary _playersInAreaDict = new DataDictionary();

        private int _eventCallbackCount;

        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[0];

        [PublicAPI]
        public int TotalPlayerCount => _playersInAreaDict.Count;

        [PublicAPI]
        public int[] TeamPlayerCount { get; private set; } = new int[MaxTeamId];

        public override string AreaName => gameObject.name;
        public override bool IsSafeZone => false;

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
            foreach (var playerToken in playersInArea)
            {
                var player = (PlayerBase)playerToken.Reference;
                if (player == null)
                {
                    CenturionDiagnostic.LogWarning($"[PlayerAreaCounter-{name}] null player in the dictionary!");
                    continue;
                }

                IncrementTeamCount(player.TeamId);
            }
        }

        [PublicAPI]
        public override PlayerBase[] GetPlayersInArea()
        {
            // Reconstruct PlayerBase array based on DataDictionary keys because it's easier to use
            var playersInAreaTokens = _playersInAreaDict.GetKeys().ToArray();
            var playerBaseArr = new PlayerBase[playersInAreaTokens.Length];
            for (var i = 0; i < playerBaseArr.Length; i++)
                playerBaseArr[i] = (PlayerBase)playersInAreaTokens[i].Reference;

            return playerBaseArr;
        }

        public override bool IsInside(Vector3 position)
        {
            foreach (var col in colliders)
            {
                // skip disabled colliders
                if (col == null || !col.gameObject.activeInHierarchy || !col.enabled)
                {
                    continue;
                }

                // if point is inside, return true
                if (Mathf.Approximately(Vector3.Distance(col.ClosestPoint(position), position), 0))
                {
                    return true;
                }
            }

            // otherwise, return false
            return false;
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

            var playerBase = playerManager.GetPlayer(player);
            if (playerBase == null) return;

            var key = new DataToken(playerBase);
            if (!_playersInAreaDict.ContainsKey(key))
            {
                // For the first time player enters collider
                _playersInAreaDict.Add(key, 0);
                IncrementTeamCount(playerBase.TeamId);
                playerBase.OnAreaEnter(this);
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
                playerBase.OnAreaExit(this);
            }
            else
            {
                _playersInAreaDict[key] = triggerCount;
            }
        }

        [UsedImplicitly]
        public void OnPlayerTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!_playersInAreaDict.ContainsKey(player)) return;

            DecrementTeamCount(oldTeam);
            IncrementTeamCount(player.TeamId);
        }

        [UsedImplicitly]
        public void OnPlayerAdded(PlayerBase player)
        {
            if (!_playersInAreaDict.Remove(player)) return;

            DecrementTeamCount(player.TeamId);
            player.OnAreaExit(this);
        }

        [UsedImplicitly]
        public void OnPlayerRemoved(PlayerBase player)
        {
            if (!_playersInAreaDict.Remove(player)) return;

            DecrementTeamCount(player.TeamId);
            player.OnAreaExit(this);
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
                var b = (AreaPlayerCounterCallback)_eventCallbacks[i];
                if (b != null) b.OnAreaPlayerCountChanged();
            }
        }
    }
}
