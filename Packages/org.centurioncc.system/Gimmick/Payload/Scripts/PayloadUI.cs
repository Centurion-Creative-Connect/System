using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
namespace CenturionCC.System.Gimmick.Payload
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PayloadUI : UdonSharpBehaviour
    {
        [Header("UI Object References")]
        [SerializeField]
        private Text currentStatusText;
        [SerializeField]
        private Text currentModeText;
        [SerializeField]
        private Text playerCountText;

        [Header("Text Formats")]
        [SerializeField]
        private string currentStatusFormat
            = @"現在のステータス  : {0}";
        [SerializeField]
        private string currentModeFormat
            = @"現在のモード      : {0}";
        [SerializeField]
        private string playerCountFormat
            = @"周囲のプレイヤー数: <color=red>{0}</color>,  <color=yellow>{1}</color>";

        private void Start()
        {
            UpdateStatus(PayloadStatus.Inactive, PayloadStatusContext.None);
            UpdateMode(PayloadMode.Inactive);
            UpdatePlayerCount(0, 0);
        }

        [PublicAPI]
        public void UpdateStatus(PayloadStatus status, PayloadStatusContext context)
        {
            currentStatusText.text = string.Format(currentStatusFormat, GetStatusMessageContent(status, context));
        }

        [PublicAPI]
        public void UpdateMode(PayloadMode mode)
        {
            currentModeText.text = string.Format(currentModeFormat, GetModeMessageContent(mode));
        }

        [PublicAPI]
        public void UpdatePlayerCount(int redPlayerCount, int yellowPlayerCount)
        {
            playerCountText.text = string.Format(playerCountFormat, redPlayerCount, yellowPlayerCount);
        }

        private static string GetStatusMessageContent(PayloadStatus status, PayloadStatusContext context)
        {
            string message;
            switch (status)
            {
                case PayloadStatus.Inactive:
                    message = "非アクティブ";
                    break;
                case PayloadStatus.Running:
                    message = "進行中";
                    break;
                case PayloadStatus.Stopped:
                    message = "停止中";
                    break;
                default:
                    message = "不明";
                    break;
            }

            switch (context)
            {
                default:
                case PayloadStatusContext.None:
                    break;
                case PayloadStatusContext.StoppedByEnemy:
                    message += " (敵が居る)";
                    break;
                case PayloadStatusContext.NoFriendlyNearby:
                    message += " (味方が居ない)";
                    break;
                case PayloadStatusContext.SpeedLv1:
                    message += " ( > )";
                    break;
                case PayloadStatusContext.SpeedLv2:
                    message += " (>> )";
                    break;
                case PayloadStatusContext.SpeedLv3:
                    message += " (>>>)";
                    break;
            }

            return message;
        }

        private static string GetModeMessageContent(PayloadMode mode)
        {
            switch (mode)
            {
                default:
                case PayloadMode.Inactive:
                    return "非アクティブ";
                case PayloadMode.Red:
                    return "<color=red>赤色チーム</color>で進行";
                case PayloadMode.Yellow:
                    return "<color=yellow>黄色チーム</color>で進行";
            }
        }
    }
}
