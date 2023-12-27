using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AreaPlayerCounter : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        private int _eventCallbackCount;

        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[0];

        private PlayerBase[] _playersInArea = new PlayerBase[0];

        [PublicAPI]
        public int TotalPlayerCount => _playersInArea.Length;
        [PublicAPI]
        public int[] TeamPlayerCount { get; private set; } = new int[short.MaxValue];

        private void Start()
        {
            playerManager.SubscribeCallback(this);
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

            foreach (var player in _playersInArea)
                if (player != null)
                    IncrementTeamCount(player.TeamId);
        }

        [PublicAPI]
        public PlayerBase[] GetPlayersInArea()
        {
            return _playersInArea;
        }

        [PublicAPI]
        public void GetPlayerCount(out int allPlayersCount, out int redPlayerCount, out int yellowPlayerCount)
        {
            allPlayersCount = _playersInArea.Length;
            redPlayerCount = TeamPlayerCount[1];
            yellowPlayerCount = TeamPlayerCount[2];
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            Debug.Log($"[PlayerCounter-{name}] OnPlayerTriggerEnter: {player.displayName}");

            var playerBase = playerManager.GetPlayerById(player.playerId);
            if (playerBase == null) return;

            _playersInArea = _playersInArea.AddAsSet(playerBase);
            IncrementTeamCount(playerBase.TeamId);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            Debug.Log($"[PlayerCounter-{name}] OnPlayerTriggerExit: {player.displayName}");

            var playerBase = playerManager.GetPlayerById(player.playerId);
            if (playerBase == null) return;

            _playersInArea = _playersInArea.RemoveItem(playerBase);
            DecrementTeamCount(playerBase.TeamId);
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            if (!_playersInArea.ContainsItem(player)) return;

            DecrementTeamCount(oldTeam);
            IncrementTeamCount(player.TeamId);
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            if (!_playersInArea.ContainsItem(player)) return;

            _playersInArea = _playersInArea.RemoveItem(player);
            DecrementTeamCount(player.TeamId);
        }

        private void DecrementTeamCount(int id)
        {
            if (id >= 0 && id <= 255) --TeamPlayerCount[id];
            Invoke_CountChanged();
        }

        private void IncrementTeamCount(int id)
        {
            if (id >= 0 && id <= 255) ++TeamPlayerCount[id];
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