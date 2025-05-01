using System;
using UdonSharp;
using UnityEngine.UI;

namespace CenturionCC.System.Audio
{
    [Obsolete("Legacy behaviour. No longer supported.")]
    public class AudioPlayerValuePresenter : UdonSharpBehaviour
    {
        public AudioPlayer audioPlayer;
        public Dropdown dropdownUI;

        private void FixedUpdate()
        {
            if (audioPlayer.GetCurrentClipIndex() != dropdownUI.value) audioPlayer.ChangeClip(dropdownUI.value);
        }
    }
}