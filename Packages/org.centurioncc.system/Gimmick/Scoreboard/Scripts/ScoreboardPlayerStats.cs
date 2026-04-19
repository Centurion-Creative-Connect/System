using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Scoreboard
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScoreboardPlayerStats : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] private Text rankingText;
        [SerializeField] private Text displayNameText;
        [SerializeField] private Text killsText;
        [SerializeField] private Text deathsText;
        [Header("Unused Texts")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text weaponText;

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
            return Source ? Source.Kills * 100 - Source.Deaths : -1;
        }

        public void UpdateText()
        {
            if (!Source)
            {
                SetText(rankingText, "??");
                SetText(displayNameText, "???(InvalidSource)");
                SetText(weaponText, "???");
                SetText(killsText, "??");
                SetText(deathsText, "??");
                SetText(scoreText, "????");
                return;
            }

            SetText(rankingText, $"{(transform.GetSiblingIndex() + 1)}");
            SetText(displayNameText, Source.ColoredDisplayName);
            SetText(weaponText, "???");
            SetText(killsText, Source.Kills.ToString());
            SetText(deathsText, Source.Deaths.ToString());
            SetText(scoreText, "????");
        }

        private static void SetText(Text t, string msg)
        {
            if (!t) return;
            t.text = msg;
        }
    }
}
