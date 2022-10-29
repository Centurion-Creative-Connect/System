using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.Scoreboard
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScoreboardManager : PlayerManagerCallbackBase
    {
        [SerializeField]
        private GameObject sourceScoreboardElement;
        [SerializeField]
        private Transform reserve;
        [SerializeField]
        private Transform yelList;
        [SerializeField]
        private Transform redList;

        private ScoreboardPlayerStats[] _generatedScoreboardElement = new ScoreboardPlayerStats[0];
        private PlayerManager _playerMgr;

        private void Start()
        {
            _playerMgr = CenturionSystemReference.GetPlayerManager();
            _playerMgr.SubscribeCallback(this);
        }

        public override void OnPlayerChanged(PlayerBase player, int oldId, int newId)
        {
            var element = GetOrCreateElement(player);
            if (element != null)
                element.UpdateText();
        }

        public override void OnTeamChanged(PlayerBase player, int oldTeam)
        {
            var element = GetOrCreateElement(player);
            if (element == null)
                return;

            var target = reserve;

            switch (player.TeamId)
            {
                case 1:
                    target = redList;
                    break;
                case 2:
                    target = yelList;
                    break;
            }

            element.transform.SetParent(target);
            SortScoreboard();
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            SortScoreboard();
        }

        public override void OnResetPlayerStats(PlayerBase player)
        {
            var element = GetOrCreateElement(player);

            if (element != null)
                element.UpdateText();
        }

        private ScoreboardPlayerStats GetOrCreateElement(PlayerBase player)
        {
            foreach (var element in _generatedScoreboardElement)
            {
                var source = element.Source;
                if (source != null && source.PlayerId == player.PlayerId)
                    return element;
            }

            var generated = Instantiate(sourceScoreboardElement, reserve);
            var generatedElement = generated.GetComponent<ScoreboardPlayerStats>();
            generatedElement.Source = player;
            _generatedScoreboardElement = _generatedScoreboardElement.AddAsList(generatedElement);
            return generatedElement;
        }

        private void SortScoreboard()
        {
            var arr = _generatedScoreboardElement;
            var i = 1;
            while (i < arr.Length)
            {
                var j = i;
                while (j > 0 && arr[j - 1].GetPriority() > arr[j].GetPriority())
                {
                    var x = arr[j];
                    arr[j] = arr[j - 1];
                    arr[j - 1] = x;
                    --j;
                }

                ++i;
            }

            _generatedScoreboardElement = arr;
            foreach (var element in _generatedScoreboardElement)
                element.transform.SetAsFirstSibling();

            // Had to update text after sorted sibling since its depending on sibling index 
            foreach (var element in _generatedScoreboardElement)
                element.UpdateText();
        }
    }
}