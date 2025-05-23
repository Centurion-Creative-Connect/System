﻿using System;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [Obsolete("Use AudioManager#PlayAudioAtPosition(AudioDataStore, Vector3) instead")]
    public class AudioHolder : UdonSharpBehaviour
    {
        [SerializeField]
        private AudioDataStore audioData;

        [SerializeField]
        private AudioManager audioManager;

        public void PlayAtPosition(Vector3 pos, float vol)
        {
            audioManager.PlayAudioAtPosition(audioData.Clip, pos, vol, audioData.Pitch, audioData.DopplerLevel,
                audioData.Spread, audioData.MinDistance, audioData.MaxDistance);
        }
    }
}