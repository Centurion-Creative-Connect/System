using TMPro;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.SystemEventLogger
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SystemEventLogger : UdonSharpBehaviour
    {
        [SerializeField]
        private TMP_Text text;

        public void AppendLog(string prefix, string category, string message)
        {
            text.text += $"<color=grey>[{prefix}] {category,-6}:</color> {message}\n";
        }
    }
}
