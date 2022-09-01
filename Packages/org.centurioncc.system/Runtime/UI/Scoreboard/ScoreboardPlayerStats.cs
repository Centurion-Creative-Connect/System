using CenturionCC.System.Player;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI.Scoreboard
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScoreboardPlayerStats : UdonSharpBehaviour
    {
        [SerializeField]
        private Text rankingText;
        [SerializeField]
        private Text displayNameText;
        [SerializeField]
        private Text killsText;
        [SerializeField]
        private Text deathsText;

        private PlayerStats _source;

        public PlayerStats Source
        {
            get => _source;
            set
            {
                _source = value;
                UpdateText();
            }
        }

        public int GetPriority()
        {
            return Source != null ? Source.Kill * 100 - Source.Death : -1;
        }

        public void UpdateText()
        {
            if (Source == null)
            {
                rankingText.text = "??";
                displayNameText.text = "???";
                killsText.text = "??";
                deathsText.text = "??";
                return;
            }

            rankingText.text = $"{(transform.GetSiblingIndex() + 1)}";

            displayNameText.text = GameManager.GetPlayerName(Source.Player.VrcPlayer);
            killsText.text = Source.Kill.ToString();
            deathsText.text = Source.Death.ToString();
        }
    }
}