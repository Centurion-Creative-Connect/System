using CenturionCC.System.Player;
using CenturionCC.System.Utils;
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

        private PlayerBase _source;

        public PlayerBase Source
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
            return Source != null ? Source.Kills * 100 - Source.Deaths : -1;
        }

        public void UpdateText()
        {
            if (Source == null)
            {
                rankingText.text = "??";
                displayNameText.text = "???(InvalidSource)";
                killsText.text = "??";
                deathsText.text = "??";
                return;
            }

            rankingText.text = $"{(transform.GetSiblingIndex() + 1)}";

            displayNameText.text = Source.VrcPlayer.SafeGetDisplayName("???(InvalidVrcPlayer)");
            killsText.text = Source.Kills.ToString();
            deathsText.text = Source.Deaths.ToString();
        }
    }
}