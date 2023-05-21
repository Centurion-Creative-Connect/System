using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NotificationUI : NotificationProvider
    {
        [SerializeField]
        private HitDisplay notificationDisplay;
        [SerializeField]
        private Sprite[] notificationIcons;
        [SerializeField]
        private Image notificationImage;
        [SerializeField]
        private Text notificationText;

        // TODO: duration is ignored
        public override void Show(NotificationLevel level, string message, float duration = 5F, int id = 0)
        {
            int imageIndex = 0;
            switch (level)
            {
                default:
                case NotificationLevel.Info:
                    imageIndex = 0;
                    break;
                case NotificationLevel.Warn:
                    imageIndex = 1;
                    break;
                case NotificationLevel.Error:
                    imageIndex = 2;
                    break;
                case NotificationLevel.Help:
                    imageIndex = 3;
                    break;
            }

            if (imageIndex > notificationIcons.Length)
            {
                Debug.LogError("[NotificationUI] Image index is out of range! falling back to index of 0");
                imageIndex = 0;
            }

            var sprite = notificationIcons[imageIndex];
            notificationImage.sprite = sprite;
            notificationText.text = message;
            notificationDisplay.Play();
        }
    }
}