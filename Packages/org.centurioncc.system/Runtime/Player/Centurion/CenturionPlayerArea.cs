using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionPlayerArea : PlayerAreaBase
    {
        [SerializeField] [NewbieInject(SearchScope.Children)] [HideInInspector]
        private Collider[] colliders;

        [SerializeField]
        private string areaName;

        [SerializeField]
        private bool isSafeZone;

        private readonly DataList _inAreaPlayers = new DataList();

        public override string AreaName => areaName;
        public override bool IsSafeZone => isSafeZone;

        private void OnDisable()
        {
            var players = GetPlayersInArea();
            foreach (var player in players)
            {
                if (player) player.OnAreaExit(this);
            }

            _inAreaPlayers.Clear();
        }

        public override PlayerBase[] GetPlayersInArea()
        {
            var players = new PlayerBase[_inAreaPlayers.Count];
            for (var i = 0; i < players.Length; i++)
            {
                players[i] = (PlayerBase)_inAreaPlayers[i].Reference;
            }

            return players;
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

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            var playerBase = playerManager.GetPlayer(player);
            if (!playerBase)
                return;

            playerBase.OnAreaEnter(this);
            _inAreaPlayers.Add(playerBase);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            var playerBase = playerManager.GetPlayer(player);
            if (!playerBase)
                return;

            playerBase.OnAreaExit(this);
            _inAreaPlayers.Remove(playerBase);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            // try to get alive PlayerBase instance
            var playerBase = playerManager.GetPlayer(player);
            if (playerBase)
            {
                playerBase.OnAreaExit(this);
                _inAreaPlayers.Remove(playerBase);
                return;
            }

            // if PlayerBase has already been destroyed, remove nulls from the list
            var tokens = _inAreaPlayers.ToArray();
            foreach (var token in tokens)
            {
                if (token.TokenType == TokenType.Reference && ((PlayerBase)token.Reference) != null)
                {
                    continue;
                }

                _inAreaPlayers.Remove(token);
            }
        }
    }
}
