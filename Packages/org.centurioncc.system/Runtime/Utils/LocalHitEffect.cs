﻿using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [Obsolete("Use HeadUILocalHitEffect instead.")]
    public class LocalHitEffect : PlayerManagerCallbackBase
    {
        private const float HapticDuration = 0.02F;

        [SerializeField]
        public bool useHaptic;

        [SerializeField]
        private AudioSource hitSound;

        [SerializeField]
        private HitDisplay hitDisplay;

        [SerializeField]
        private AudioClip hapticSource;

        [SerializeField]
        private Image hitSprite;

        [SerializeField]
        private Sprite[] availableSprites;

        [FormerlySerializedAs("_updateManager")]
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [NonSerialized]
        private float _currentTime;

        [NonSerialized]
        private int _hapticFrequency;

        [NonSerialized]
        private int _hapticPackSize;

        [NonSerialized]
        private float[] _hapticSamples;

        [NonSerialized]
        private DateTime _lastLocalPlayerHitTime;

        [NonSerialized]
        private int _lastSpriteIndex;

        private void Start()
        {
            if (hapticSource.preloadAudioData == false)
                hapticSource.LoadAudioData();

            _hapticFrequency = hapticSource.frequency;
            _hapticPackSize = hapticSource.samples * hapticSource.channels;
            _hapticSamples = new float[_hapticPackSize];

            hapticSource.GetData(_hapticSamples, 0);

            playerManager.Subscribe(this);
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!victim.IsLocal)
                return;

            Play();
        }

        public void _Update()
        {
            if (_currentTime > hapticSource.length || !useHaptic)
            {
                _currentTime = 0F;
                updateManager.UnsubscribeUpdate(this);
            }

            var i = Mathf.RoundToInt(_currentTime * _hapticFrequency);
            if (i >= _hapticSamples.Length)
            {
                Debug.LogError("[LocalHitEffect] haptic freq sample index was out of range!");
                i = 0;
            }

            var f = Mathf.Abs(_hapticSamples[i]);

            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, HapticDuration, 1, f);
            Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, HapticDuration, 1, f);

            _currentTime += Time.deltaTime;
        }

        public void Play()
        {
            Debug.Log("[LocalHitEffect] Play");
            if (hitSound) hitSound.Play();

            if (hitDisplay && !hitDisplay.IsPlaying)
            {
                _lastSpriteIndex = RandomNoDouble();
                hitSprite.sprite = availableSprites[_lastSpriteIndex];
                hitDisplay.Play();
            }

            if (useHaptic)
            {
                updateManager.UnsubscribeUpdate(this);
                updateManager.SubscribeUpdate(this);
            }
        }

        private int RandomNoDouble()
        {
            var result = _lastSpriteIndex;
            while (result == _lastSpriteIndex)
                result = UnityEngine.Random.Range(0, availableSprites.Length);
            return result;
        }
    }
}