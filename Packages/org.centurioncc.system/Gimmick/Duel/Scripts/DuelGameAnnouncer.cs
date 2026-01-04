using CenturionCC.System.Audio;
using DerpyNewbie.Common;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gimmick.Duel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DuelGameAnnouncer : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject] [HideInInspector]
        private AudioManager audioManager;

        [SerializeField]
        private AudioDataStore makeReadyAudio;
        [SerializeField]
        private AudioDataStore[] roundCountAudios;
        [SerializeField]
        private AudioDataStore roundMatchPointAudio;
        [SerializeField]
        private AudioDataStore roundStartingCountdownAudio;
        [SerializeField]
        private AudioDataStore winnerTeamAAudio;
        [SerializeField]
        private AudioDataStore winnerTeamBAudio;

        [SerializeField]
        private float startingDelayTime = 0.5F;

        private bool _cancelStartingSchedule = false;

        public void PlayMakeReady()
        {
            Play(makeReadyAudio);
        }

        public void ScheduleStartingAnnounce(long startingTimeTicks, int roundCount, bool isMatchPoint)
        {
            _cancelStartingSchedule = false;

            if (isMatchPoint)
                PlayMatchPoint();
            else
                PlayRoundCount(roundCount);

            var now = Networking.GetNetworkDateTime();
            var startingDateTime = new DateTime(startingTimeTicks);
            var delay = (float)startingDateTime.Subtract(now.AddSeconds(roundStartingCountdownAudio.Clip.length))
                .TotalSeconds + startingDelayTime;

            Debug.Log($"[DuelGameAnnouncer] Scheduling to play after {delay} seconds");

            SendCustomEventDelayedSeconds(nameof(PlayRoundStartingCountdown), delay);
        }

        public void CancelStartingAnnounceAll()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(CancelStartingAnnounce));
        }

        public void CancelStartingAnnounce()
        {
            _cancelStartingSchedule = true;
        }

        public void PlayMatchPoint()
        {
            Play(roundMatchPointAudio);
        }

        public void PlayRoundCount(int roundCount)
        {
            if (roundCountAudios.Length <= roundCount || roundCount <= 0)
                Play(roundCountAudios[0]);
            else
                Play(roundCountAudios[roundCount]);
        }

        public void PlayRoundStartingCountdown()
        {
            if (_cancelStartingSchedule) return;
            Play(roundStartingCountdownAudio);
        }

        public void PlayWinnerAAll()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayWinnerA));
        }

        public void PlayWinnerA()
        {
            Play(winnerTeamAAudio);
        }

        public void PlayWinnerBAll()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayWinnerB));
        }

        public void PlayWinnerB()
        {
            Play(winnerTeamBAudio);
        }

        private void Play(AudioDataStore dataStore)
        {
            audioManager.PlayAudioAtPosition(dataStore.Clip, transform.position, dataStore.Volume, dataStore.Pitch,
                dataStore.DopplerLevel, dataStore.Spread, dataStore.MinDistance, dataStore.MaxDistance);
        }
    }
}
