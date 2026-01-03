using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Defuser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DefuserCancelInteraction : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField]
        private Defuser defuser;

        [SerializeField]
        private double cancelTime = 8D;
        [SerializeField]
        private double maxProgressBetweenTime = 2D;

        [SerializeField]
        private Transform lookAtTransform;
        [SerializeField]
        private GameObject canvas;
        [SerializeField]
        private Slider progressSlider;
        [SerializeField]
        private Text progressText;
        [SerializeField]
        private Text abortText;

        private DateTime _abortTextDisplayTime;
        [UdonSynced]
        private int _cancellingPlayerId = -1;
        [UdonSynced]
        private long _cancelProgressDateTime = DateTime.MinValue.Ticks;

        [UdonSynced]
        private long _cancelStartDateTime = DateTime.MinValue.Ticks;
        private DateTime _completedDisplayTime;
        private bool _isCancelling;

        public bool IsCancelling
        {
            get => _isCancelling;
            private set
            {
                _isCancelling = value;
                progressSlider.gameObject.SetActive(value);
                progressText.gameObject.SetActive(value);
            }
        }

        public DateTime CancelStartTime
        {
            get => new DateTime(_cancelStartDateTime);
            private set => _cancelStartDateTime = value.Ticks;
        }

        public DateTime CancelProgressTime
        {
            get => new DateTime(_cancelProgressDateTime);
            private set => _cancelProgressDateTime = value.Ticks;
        }

        private void OnTriggerEnter(Collider other)
        {
            var gunBase = other.GetComponent<GunBase>();
            if (gunBase == null || gunBase.CurrentHolder == null || !gunBase.CurrentHolder.IsValid()) return;

            var currentHolder = gunBase.CurrentHolder;

            if (!currentHolder.isLocal ||
                (_cancellingPlayerId != -1 && _cancellingPlayerId != currentHolder.playerId)) return;

            var now = Networking.GetNetworkDateTime();
            if (_cancellingPlayerId == -1)
            {
                var playerBase = playerManager.GetPlayerById(currentHolder.playerId);
                if (playerBase == null || !defuser.CanDefuseAsTeam(playerBase.TeamId)) return;

                IsCancelling = true;
                CancelStartTime = now;
                CancelProgressTime = now;
                _cancellingPlayerId = currentHolder.playerId;

                CancellingCoroutine();
            }

            CancelProgressTime = now;

            Sync();
        }

        public override void OnDeserialization()
        {
            if (_cancellingPlayerId != -1 && !IsCancelling)
            {
                IsCancelling = true;
                CancellingCoroutine();
            }
        }

        public void CancellingCoroutine()
        {
            if (defuser.State != DefuserState.Defusing || !IsCancelling)
            {
                var shouldSync = _cancellingPlayerId == Networking.LocalPlayer.playerId;
                Stop();
                if (shouldSync) Sync();
                return;
            }

            SendCustomEventDelayedFrames(nameof(CancellingCoroutine), 1);

            var now = Networking.GetNetworkDateTime();
            var elapsedSeconds = now.Subtract(CancelStartTime).TotalSeconds;
            var elapsedSecondsSinceLastProgress = now.Subtract(CancelProgressTime).TotalSeconds;
            var progress = Mathf.Clamp01((float)(elapsedSeconds - elapsedSecondsSinceLastProgress) / (float)cancelTime);

            // Update UI
            SetProgressUIActive(true);
            SetProgressUI(progress);

            // Do something at event
            if (_cancellingPlayerId != Networking.LocalPlayer.playerId) return;

            var playerBase = playerManager.GetPlayerById(_cancellingPlayerId);
            if (playerBase.IsDead)
            {
                Stop();
                Sync();
                ShowAbortText("You've been hit! Don't get hit next time!", 5D);
                return;
            }

            if (elapsedSecondsSinceLastProgress > maxProgressBetweenTime)
            {
                Stop();
                Sync();
                ShowAbortText("Bonking were not fast enough! Continue hitting faster!", 5D);
                return;
            }

            if (Mathf.Approximately(1, progress))
            {
                defuser.CancelDefusing();
                ShowCompleted();
                Stop();
                Sync();
                return;
            }
        }

        public void CompletedCoroutine()
        {
            if (_completedDisplayTime < Networking.GetNetworkDateTime())
            {
                SetProgressUIActive(false);
                return;
            }

            SendCustomEventDelayedFrames(nameof(CompletedCoroutine), 1);

            SetProgressUIActive(true);
            SetProgressUI(1F);
        }

        public void AbortTextCoroutine()
        {
            if (_abortTextDisplayTime < Networking.GetNetworkDateTime())
            {
                abortText.gameObject.SetActive(false);
                return;
            }

            SendCustomEventDelayedFrames(nameof(AbortTextCoroutine), 5);
        }

        private void ShowCompleted()
        {
            _completedDisplayTime = Networking.GetNetworkDateTime().AddSeconds(5D);
            CompletedCoroutine();
        }

        private void ShowAbortText(string text, double durInSec)
        {
            abortText.gameObject.SetActive(true);
            abortText.text = text;
            _abortTextDisplayTime = Networking.GetNetworkDateTime().AddSeconds(durInSec);
            AbortTextCoroutine();
        }

        private void SetProgressUI(float progress)
        {
            lookAtTransform.LookAt(Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
            progressSlider.value = progress;
            progressText.text = $"{progress:P0}";
        }

        private void SetProgressUIActive(bool isActive)
        {
            progressSlider.gameObject.SetActive(isActive);
            progressText.gameObject.SetActive(isActive);
        }

        private void Stop()
        {
            _cancellingPlayerId = -1;
            IsCancelling = false;
        }

        private void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
