using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionPlayerArea : PlayerAreaBase
    {
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
                player.OnAreaExit(this);
            }

            _inAreaPlayers.Clear();
        }

        public override PlayerBase[] GetPlayersInArea()
        {
            var players = new PlayerBase[_inAreaPlayers.Count];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = (PlayerBase)_inAreaPlayers[i].Reference;
            }

            return players;
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
    }
}