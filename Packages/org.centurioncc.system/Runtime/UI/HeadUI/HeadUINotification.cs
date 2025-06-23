using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.HeadUI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HeadUINotification : NotificationProvider
    {
        [SerializeField]
        private Sprite[] sprites;

        [SerializeField]
        private Color[] panelColors;

        [SerializeField]
        private GameObject notificationElement;

        [SerializeField]
        private float smoothTime = 0.1F;

        [SerializeField]
        private float maxSpeed = float.PositiveInfinity;

        private HeadUINotificationElement[] _elements = new HeadUINotificationElement[0];

        private void Start()
        {
            notificationElement.SetActive(false);
        }

        public void RecalculateTargets()
        {
            var height = 0F;
            foreach (var e in _elements)
            {
                if (!e) continue;
                e.positionTarget = new Vector2(0, height);
                height += e.rectTransform.rect.height;
            }
        }

        public void RemoveNotification(HeadUINotificationElement element)
        {
            _elements = _elements.RemoveItem(element);
            RecalculateTargets();
        }

        public override void Show(NotificationLevel level, string message, float duration = 5F, int id = 0)
        {
            if (id == 0)
            {
                id = message.GetHashCode();
            }

            Debug.Log($"[Notification] {id}:{message}");

            foreach (var elem in _elements)
            {
                if (!elem || elem.Id != id) continue;
                elem.AddDuplicate(message, duration);
                RecalculateTargets();
                return;
            }

            var obj = Instantiate(notificationElement, transform);
            obj.SetActive(true);
            var element = obj.GetComponent<HeadUINotificationElement>();
            GetNotificationConfig(level, out var sprite, out var color);
            element.Setup(this, sprite, color, duration, smoothTime, maxSpeed, message, id);
            _elements = _elements.AddAsSet(element);
            SendCustomEventDelayedFrames(nameof(RecalculateTargets), 2);
        }

        private void GetNotificationConfig(NotificationLevel level, out Sprite sprite, out Color color)
        {
            int index;
            switch (level)
            {
                default:
                case NotificationLevel.Info:
                    index = 0;
                    break;
                case NotificationLevel.Warn:
                    index = 1;
                    break;
                case NotificationLevel.Error:
                    index = 2;
                    break;
                case NotificationLevel.Help:
                    index = 3;
                    break;
            }

            if (index > sprites.Length || index > panelColors.Length)
                index = 0;

            sprite = sprites[index];
            color = panelColors[index];
        }
    }
}