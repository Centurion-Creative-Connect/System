using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CenturionCC.System.UI.HeadUI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HeadUILocalHitEffect : PlayerManagerCallbackBase
    {
        private const float HapticDuration = 0.02F;

        [SerializeField]
        public bool useHaptic;
        [SerializeField]
        public float localPlayerHitDuration = 1F;
        [SerializeField]
        private RectTransform rectTransform;

        [SerializeField]
        private AudioSource hitSound;
        [SerializeField]
        private AudioClip hapticSource;
        [SerializeField]
        private Image hitSprite;
        [SerializeField]
        private Image labelSprite;
        [SerializeField]
        private Sprite[] availableSprites;

        [SerializeField]
        private float inTime = 0.1F;
        [SerializeField]
        private Vector2 inPosition = new Vector2(0, -500);
        [SerializeField]
        private float stopTime = 5F;
        [SerializeField]
        private Vector2 stopPosition = Vector2.zero;
        [SerializeField]
        private float outTime = 0.1F;
        [SerializeField]
        private Vector2 outPosition = new Vector2(0, 500);

        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private float _alpha;
        private float _alphaSmoothTime;
        private float _alphaTarget;
        private float _alphaVelocity;

        [NonSerialized]
        private float _currentTime;
        [NonSerialized]
        private int _hapticFrequency;
        [NonSerialized]
        private int _hapticPackSize;
        [NonSerialized]
        private float[] _hapticSamples;

        private bool _isPlaying;
        [NonSerialized]
        private int _lastSpriteIndex;
        private float _posSmoothTime;

        private Vector2 _posTarget;
        private Vector2 _posVelocity;

        [NonSerialized]
        public DateTime lastHitEffectPlayBeginTime;
        [NonSerialized]
        public DateTime lastHitEffectPlayEndTime;

        private void Start()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (hapticSource.preloadAudioData == false)
                hapticSource.LoadAudioData();

            _hapticFrequency = hapticSource.frequency;
            _hapticPackSize = hapticSource.samples * hapticSource.channels;
            _hapticSamples = new float[_hapticPackSize];

            hapticSource.GetData(_hapticSamples, 0);

            playerManager.SubscribeCallback(this);

            SetAlpha(0F);
        }

        public void _Update()
        {
            if (_currentTime > inTime + stopTime + outTime + 2F)
            {
                _isPlaying = false;
                _currentTime = 0F;
                SetAlpha(0F);
                updateManager.UnsubscribeUpdate(this);
                lastHitEffectPlayEndTime = Networking.GetNetworkDateTime();
            }

            rectTransform.anchoredPosition = Vector2.SmoothDamp(
                rectTransform.anchoredPosition,
                _posTarget,
                ref _posVelocity,
                _posSmoothTime
            );

            SetAlpha(_alpha = Mathf.SmoothDamp(_alpha, _alphaTarget, ref _alphaVelocity, _alphaSmoothTime));

            PlaySoundHaptic();

            _currentTime += Time.deltaTime;
        }

        public void Play()
        {
            Debug.Log("[LocalHitEffect] Play");
            if (_isPlaying)
            {
                Debug.LogError("[LocalHitEffect] Still playing but reset!");
                lastHitEffectPlayEndTime = Networking.GetNetworkDateTime();
            }

            if (hitSound)
                hitSound.Play();

            _lastSpriteIndex = RandomNoDouble();
            hitSprite.sprite = availableSprites[_lastSpriteIndex];

            DoSmoothIn();
            SendCustomEventDelayedSeconds(nameof(DoSmoothOut), inTime + stopTime);

            _currentTime = 0F;
            _isPlaying = true;
            lastHitEffectPlayBeginTime = Networking.GetNetworkDateTime();

            updateManager.UnsubscribeUpdate(this);
            updateManager.SubscribeUpdate(this);
        }

        private void PlaySoundHaptic()
        {
            var i = Mathf.RoundToInt(_currentTime * _hapticFrequency);
            if (i >= _hapticSamples.Length)
                return;

            var f = Mathf.Abs(_hapticSamples[i]);

            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, HapticDuration, f, f);
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, HapticDuration, f, f);
        }

        private void SetAlpha(float alpha)
        {
            var hc = hitSprite.color;
            hitSprite.color = new Color(hc.r, hc.g, hc.b, alpha);
            var lc = labelSprite.color;
            labelSprite.color = new Color(lc.r, lc.g, lc.b, alpha);
        }

        private int RandomNoDouble()
        {
            var result = _lastSpriteIndex;
            while (result == _lastSpriteIndex)
                result = UnityEngine.Random.Range(0, availableSprites.Length);
            return result;
        }

        public void DoSmoothIn()
        {
            rectTransform.anchoredPosition = inPosition;
            _posTarget = stopPosition;
            _posVelocity = Vector2.zero;
            _posSmoothTime = inTime;
            _alphaTarget = 1F;
            _alphaVelocity = 0F;
            _alphaSmoothTime = inTime;
        }

        public void DoSmoothOut()
        {
            _posTarget = outPosition;
            _posVelocity = Vector2.zero;
            _posSmoothTime = outTime;
            _alphaTarget = 0F;
            _alphaVelocity = 0F;
            _alphaSmoothTime = outTime;
        }

        #region PlayerManagerCallback

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (!hitPlayer.IsLocal)
                return;

            Play();
        }

        #endregion
    }
}