using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
namespace CenturionCC.System.Gimmick.AreaPlayerCounter
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AreaPlayerCounterText : UdonSharpBehaviour
    {
        [SerializeField]
        private AreaPlayerCounter counter;
        [SerializeField]
        private Text text;
        [SerializeField] [TextArea]
        private string format = "All   : %total%\n" +
                                "<color=red>Red</color>   : %red%\n" +
                                "<color=yellow>Yellow</color>: %yel%\n" +
                                "<color=cyan>Staff</color> : %staff%";
        [SerializeField]
        private string[] placeholders = { "%non%", "%red%", "%yel%", "%gre%", "%blu%", "%staff%" };
        [SerializeField]
        private short[] placeholderTeamIds = { 0, 1, 2, 3, 4, 255 };

        private void Start()
        {
            counter.SubscribeCallback(this);
        }

        public void OnAreaPlayerCountChanged()
        {
            Debug.Log($"[AreaPlayerCounterText-{name}] OnAreaPlayerCountChanged");
            UpdateText();
        }

        public void UpdateText()
        {
            text.text = GetFormattedText(counter, format, placeholders, placeholderTeamIds);
        }

        private static string GetFormattedText(AreaPlayerCounter counter,
                                               string format, string[] placeholders, short[] placeholderTeamIds)
        {
            format = format.Replace("%total%", counter.TotalPlayerCount.ToString());
            for (var i = 0; i < placeholders.Length || i < placeholderTeamIds.Length; i++)
                format = format.Replace(placeholders[i], counter.TeamPlayerCount[i].ToString());
            return format;
        }
    }
}
