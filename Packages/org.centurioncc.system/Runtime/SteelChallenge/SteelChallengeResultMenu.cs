using System;
using DerpyNewbie.Common.UI;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.SteelChallenge
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SteelChallengeResultMenu : UdonSharpBehaviour
    {
        [SerializeField]
        private PopUpImage popUp;
        [SerializeField]
        private Text hitTimeText;
        [SerializeField]
        private Text resultTimeText;
        [SerializeField]
        private Text penaltyText;

        public void ShowResultMenu(TimeSpan[] hitTimes,
            int misses,
            TimeSpan resultTime,
            bool hasFootFaults,
            bool hasFalseStart)
        {
            penaltyText.text = _GetPenaltyText(misses, hasFootFaults, hasFalseStart);
            hitTimeText.text = _GetHitTimeText(hitTimes);
            resultTimeText.text = _GetSecondsFromTimeSpan(resultTime);
            popUp.Show();
        }

        public void HideResultMenu()
        {
            popUp.Hide();
        }

        private string _GetPenaltyText(int missCount, bool hasFootFaults, bool hasFalseStart)
        {
            var result = "";
            if (hasFalseStart)
                result += "+ 3.00 False Start\n";
            if (hasFootFaults)
                result += "+ 3.00 Foot Faults\n";
            if (missCount > 0)
                result += $"+ {3 * missCount}.00 Misses x{missCount}\n";
            return result;
        }

        private string _GetHitTimeText(TimeSpan[] hitTimes)
        {
            var result = "";
            for (var i = 0; i < hitTimes.Length; i++)
                result += string.Format("{0,2}. {1,5}\n", i + 1, _GetSecondsFromTimeSpan(hitTimes[i]));
            return result;
        }

        private string _GetSecondsFromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 0)
                return "0.00";
            if (timeSpan.TotalSeconds >= 30)
                return "30.00";
            return timeSpan.ToString(@"s\.ff");
        }
    }
}