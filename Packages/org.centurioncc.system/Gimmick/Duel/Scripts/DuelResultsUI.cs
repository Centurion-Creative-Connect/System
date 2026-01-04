using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;
namespace CenturionCC.System.Gimmick.Duel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DuelResultsUI : UdonSharpBehaviour
    {
        [SerializeField]
        private Text resultsText;

        [UdonSynced]
        private string newLogValue;

        private void Start()
        {
            resultsText.text = "";
        }

        public void _AddMatchLog(DateTime time, DuelGamePlayer playerA, DuelGamePlayer playerB, int aScore, int bScore)
        {
            // in format of: [MM/DD hh:mm] {W/L}:{name}({weaponName}) vs {W/L}{name}({weaponName}): {score} - {score}
            var hasATeamWon = aScore > bScore;
            const string winPrefix = "<color=orange>W</color>";
            const string losePrefix = "<color=grey>L</color>";
            newLogValue =
                $"<color=grey>[{time:MM/dd hh:mm}]</color> " +
                $"{(hasATeamWon ? winPrefix : losePrefix)}:<color=green>{playerA.DisplayName}</color>({playerA.weaponName}) " +
                "vs " +
                $"{(hasATeamWon ? losePrefix : winPrefix)}:<color=blue>{playerB.DisplayName}</color>({playerB.weaponName}): " +
                $"<color=green>{aScore}</color> - <color=blue>{bScore}</color>";

            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            resultsText.text += newLogValue + "\n";
        }

        public override void OnPostSerialization(SerializationResult result)
        {
            if (result.success) resultsText.text += newLogValue + "\n";
        }
    }
}
