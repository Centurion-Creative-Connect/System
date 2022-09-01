using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HitDisplay : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject display;
        [SerializeField]
        private float displayDuration = 1F;
        [SerializeField]
        private bool allowOverridePlaying;

        [Header("LookAt")]
        [SerializeField]
        private bool doLookAtPlayer = true;
        [SerializeField]
        private bool adjustToPlayerHeight;

        [Header("Slide In")]
        [SerializeField]
        private bool doSlideIn = true;
        [SerializeField]
        private Vector3 slideInVector = Vector3.up;
        [SerializeField]
        private float slideInDuration = .5F;
        [SerializeField]
        private float slideInAmount = 1F;

        [Header("Slide Out")]
        [SerializeField]
        private bool doSlideOut;
        [SerializeField]
        private Vector3 slideOutVector = Vector3.up;
        [SerializeField]
        private float slideOutDuration = .1F;
        [SerializeField]
        private float slideOutAmount = 2F;

        private float _currSlideIn;
        private float _currSlideOut;
        private Vector3 _defaultPosition;
        private float _playerHeight;
        private float _timer;

        private UpdateManager _updateManager;
        public bool IsPlaying { get; private set; }

        private void Start()
        {
            _updateManager = GameObject.Find("UpdateManager").GetComponent<UpdateManager>();

            if (!display)
            {
                Debug.LogError("HitDisplay::Display is null");
                return;
            }

            _defaultPosition = transform.localPosition;
            display.SetActive(false);
        }

        public void _Update()
        {
            if (!IsPlaying) return;

            _timer += Time.deltaTime;

            if (_timer > displayDuration)
                Stop();
            else
                Process();
        }

        public void Play()
        {
            if (IsPlaying && !allowOverridePlaying)
            {
                Debug.Log("[HitDisplay] already playing!");
                return;
            }

            Stop();
            display.SetActive(true);
            IsPlaying = true;
            _updateManager.UnsubscribeUpdate(this);
            _updateManager.SubscribeUpdate(this);
            Debug.Log("[HitDisplay] now playing!");
        }

        private void Process()
        {
            if (doSlideIn)
                ProcessSlideIn();
            if (doSlideOut)
                ProcessSlideOut();

            var nextPos = _defaultPosition + slideInVector * _currSlideIn + slideOutVector * _currSlideOut;

            transform.localPosition = nextPos;

            if (doLookAtPlayer)
                transform.LookAt(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);

            //Debug.Log($"timer: {_timer:F2}, slideIn: {_currSlideIn:F2}, slideOut: {_currSlideOut:F2}");
        }

        private void ProcessSlideIn()
        {
            var upwardProgress = _timer / slideInDuration;
            var upward = adjustToPlayerHeight ? slideInAmount + _playerHeight : slideInAmount;
            _currSlideIn = Mathf.Lerp(0, upward, upwardProgress);
        }

        private void ProcessSlideOut()
        {
            var slideOutProgress = (_timer - (displayDuration - slideOutDuration)) / slideOutDuration;
            _currSlideOut = Mathf.Lerp(0, slideOutAmount, slideOutProgress);
        }

        public void Stop()
        {
            _updateManager.UnsubscribeUpdate(this);

            _timer = 0;
            _currSlideIn = 0;
            _currSlideOut = 0;

            var player = Networking.LocalPlayer;
            var trackingPos = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            var playerPos = player.GetPosition();

            _playerHeight = (trackingPos - playerPos).y;
            Debug.Log($"[HitDisplay] player height = {_playerHeight}, expect added height for {slideInAmount}");
            display.transform.localPosition = _defaultPosition;
            display.SetActive(false);
            IsPlaying = false;
            Debug.Log("[HitDisplay] stopped!");
        }
    }
}