using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NotificationUI : UdonSharpBehaviour
    {
        [SerializeField]
        private HitDisplay notificationDisplay;
        [SerializeField]
        private Sprite[] notificationIcons;
        [SerializeField]
        private Image notificationImage;
        [SerializeField]
        private Text notificationText;

        public void ShowInfo(string text)
        {
            Show(0, text);
        }

        public void ShowWarn(string text)
        {
            Show(1, text);
        }

        public void ShowError(string text)
        {
            Show(2, text);
        }

        private void Show(int imageIndex, string text)
        {
            if (imageIndex < 0 || imageIndex > notificationIcons.Length)
            {
                Debug.LogError("[NotificationUI] Image index is out of range! falling back to index of 0");
                imageIndex = 0;
            }

            var sprite = notificationIcons[imageIndex];
            notificationImage.sprite = sprite;
            notificationText.text = text;
            notificationDisplay.Play();
        }
    }
}