using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Moderator.UIElement
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TrackingIconUI : UdonSharpBehaviour
    {
        [SerializeField]
        private Text headerText;
        [SerializeField]
        private Text descriptionText;
        [SerializeField]
        private Text displayNameText;
        [SerializeField]
        private Image iconImage;

        private float _lastStartTime;
        private bool _isFollowing;
        private Transform _target;
        private TrackingIconUICallback _callback;

        public bool IsFollowing => _isFollowing;

        public void StartFollowing(TrackingIconUICallback callback, Transform target, string displayName,
            string description, bool isWarning)
        {
            if (_isFollowing)
                return;
            gameObject.SetActive(true);

            _target = target;
            _callback = callback;

            headerText.text = isWarning ? "Warning" : "Hit";
            headerText.color = isWarning ? Color.yellow : Color.white;
            descriptionText.text = description;
            displayNameText.text = displayName;
            iconImage.color = isWarning ? Color.yellow : Color.white;

            _lastStartTime = Time.timeSinceLevelLoad;
            _isFollowing = true;
            Debug.Log($"[TrackingIconUI] now following {displayName}");
        }

        public void StopFollowing()
        {
            _isFollowing = false;
            gameObject.SetActive(false);
            if (_callback != null)
            {
                _callback.OnStoppedFollowing(this);
            }

            Debug.Log($"[TrackingIconUI] stopped following {displayNameText.text}");
        }

        public void UpdatePosition(Camera referenceCam)
        {
            if (!_isFollowing || _target == null) return;
            if (_lastStartTime + 3F < Time.timeSinceLevelLoad)
            {
                StopFollowing();
                return;
            }

            var targetPos = _target.position;
            var point = referenceCam.WorldToScreenPoint(targetPos);
            var camTransform = referenceCam.transform;
            float dot = Vector3.Dot(camTransform.forward, (targetPos - camTransform.position).normalized);

            if (dot > .4F)
            {
                transform.localPosition = point;
            }
        }
    }
}