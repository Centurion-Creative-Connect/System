using CenturionCC.System.Audio;
using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Defuser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Defuser : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;
        [SerializeField]
        private DefuserPickup defuserPickup;
        [SerializeField] [CanBeNull]
        private DefuserText defuserText;
        [SerializeField] [CanBeNull]
        private Animator animator;
        [SerializeField] [CanBeNull]
        private ParticleSystem iconParticle;
        [SerializeField]
        private double plantTime = 5D;
        [SerializeField]
        private double defuseTime = 60D;
        [SerializeField]
        private bool bypassPlantCheck;
        [SerializeField]
        private bool useIconParticle = true;
        [SerializeField]
        private AudioDataStore defuseReadyAudio;
        [SerializeField]
        private AudioDataStore defuseCancelAudio;
        [SerializeField]
        private AudioDataStore defuseCompleteAudio;
        private readonly int _defuserHasIconAnim = Animator.StringToHash("DefuserHasIcon");
        private readonly int _defuserPlantProgressAnim = Animator.StringToHash("DefuserPlantProgress");

        private readonly int _defuserProgressAnim = Animator.StringToHash("DefuserProgress");
        private readonly int _defuserStateAnim = Animator.StringToHash("DefuserState");
        [UdonSynced]
        private long _defuseBeginDateTime = DateTime.MinValue.Ticks;
        [UdonSynced]
        private long _defuseCompleteDateTime = DateTime.MinValue.Ticks;

        [UdonSynced]
        private long _plantBeginDateTime = DateTime.MinValue.Ticks;
        [UdonSynced]
        private long _plantCompleteDateTime = DateTime.MinValue.Ticks;
        [UdonSynced] [FieldChangeCallback(nameof(ShowIcon))]
        private bool _showIcon;

        [UdonSynced] [FieldChangeCallback(nameof(State))]
        private DefuserState _syncedState;

        public DefuserState State
        {
            get => _syncedState;
            private set
            {
                var oldState = _syncedState;
                _syncedState = value;
                if (oldState != _syncedState)
                    OnStateChanged(oldState, value);
            }
        }

        public bool ShowIcon
        {
            get => _showIcon;
            private set
            {
                _showIcon = value;
                UpdateIconColor();
                var localPlayerTeam = 0;
                var localPlayer = playerManager.GetLocalPlayer();
                if (localPlayer != null) localPlayerTeam = localPlayer.TeamId;
                var currentTeamId = GetCurrentTeamId();

                // LocalPlayer must be same team or in special team to be able to see defuser icon.
                // Except when current defuser team is special, no one should be able to see the icon.
                var shouldShow = (localPlayerTeam == currentTeamId || playerManager.IsSpecialTeamId(localPlayerTeam)) &&
                                 !playerManager.IsSpecialTeamId(currentTeamId);
                var showing = value && useIconParticle && shouldShow;

                if (iconParticle != null) iconParticle.gameObject.SetActive(showing);
                if (animator != null) animator.SetBool(_defuserHasIconAnim, showing);
            }
        }

        public bool CanPlant { get; set; }

        [field: UdonSynced]
        public int Planter { get; private set; }

        [field: UdonSynced]
        public int PlanterTeamId { get; private set; }

        public DateTime PlantBegin
        {
            get => new DateTime(_plantBeginDateTime);
            private set => _plantBeginDateTime = value.Ticks;
        }

        public DateTime PlantComplete
        {
            get => new DateTime(_plantCompleteDateTime);
            private set => _plantCompleteDateTime = value.Ticks;
        }

        public DateTime DefuseBegin
        {
            get => new DateTime(_defuseBeginDateTime);
            private set => _defuseBeginDateTime = value.Ticks;
        }

        public DateTime DefuseComplete
        {
            get => new DateTime(_defuseCompleteDateTime);
            private set => _defuseCompleteDateTime = value.Ticks;
        }

        [CanBeNull] public VRC_Pickup VRCPickup => defuserPickup != null ? defuserPickup.pickup : null;

        public void ResetDefuser()
        {
            Planter = 0;
            PlanterTeamId = 0;
            ShowIcon = false;

            var initDateTime = DateTime.MinValue;
            PlantBegin = initDateTime;
            PlantComplete = initDateTime;
            DefuseBegin = initDateTime;
            DefuseComplete = initDateTime;

            State = DefuserState.Idle;
            Sync();
        }

        public void Open()
        {
            if (State != DefuserState.Idle) return;
            State = DefuserState.Opened;
            Sync();
        }

        public void Close()
        {
            switch (State)
            {
                default:
                case DefuserState.Idle:
                    return;
                case DefuserState.Opened:
                    break;
                case DefuserState.PlantingTimer:
                    AbortPlanting();
                    break;
                case DefuserState.Defusing:
                case DefuserState.Defused:
                    CancelDefusing();
                    break;
            }

            State = DefuserState.Idle;
            Sync();
        }

        public void BeginPlanting()
        {
            if (State != DefuserState.Opened) return;
            if (!CanPlant && !bypassPlantCheck)
            {
                if (defuserText != null) defuserText.CannotPlant(Networking.LocalPlayer.SafeGetDisplayName("root"));
                return;
            }

            Planter = Networking.LocalPlayer.playerId;
            var planterPlayer = playerManager.GetPlayerById(Planter);
            PlanterTeamId = planterPlayer != null ? planterPlayer.TeamId : 0;
            PlantBegin = Networking.GetNetworkDateTime();
            PlantComplete = Networking.GetNetworkDateTime().AddSeconds(plantTime);
            State = DefuserState.PlantingTimer;
            Sync();
        }

        public void AbortPlanting()
        {
            if (State != DefuserState.PlantingTimer) return;

            State = DefuserState.Idle;
            Sync();
        }

        public void BeginDefusing()
        {
            if (State != DefuserState.PlantingUser) return;

            DefuseBegin = Networking.GetNetworkDateTime();
            DefuseComplete = Networking.GetNetworkDateTime().AddSeconds(defuseTime);
            State = DefuserState.Defusing;
            Sync();
        }

        public void CancelDefusing()
        {
            if (State != DefuserState.Defusing && State != DefuserState.Defused) return;

            State = DefuserState.Idle;
            Sync();
        }

        public void PlantTimerCoroutine()
        {
            if (State != DefuserState.PlantingTimer) return;

            SendCustomEventDelayedFrames(nameof(PlantTimerCoroutine), 1);

            var username = VRCPlayerApi.GetPlayerById(Planter).SafeGetDisplayName("root");
            var now = Networking.GetNetworkDateTime();
            var plantProgress = CalculateProgressNormalized(PlantBegin, now, PlantComplete);
            var etaInSeconds = Mathf.CeilToInt((float)now.Subtract(PlantBegin).TotalSeconds);
            if (defuserText != null) defuserText.PlantProgress(username, plantProgress, etaInSeconds);
            if (animator != null) animator.SetFloat(_defuserPlantProgressAnim, plantProgress);

            if (!Networking.IsMaster) return;

            var player = playerManager.GetPlayerById(Planter);
            if (player == null || player.IsDead)
            {
                State = DefuserState.Idle;
                Sync();
                return;
            }

            if (PlantComplete < Networking.GetNetworkDateTime())
            {
                State = DefuserState.PlantingUser;
                Sync();
            }
        }

        public void DefusingTimerCoroutine()
        {
            if (State != DefuserState.Defusing) return;

            SendCustomEventDelayedFrames(nameof(DefusingTimerCoroutine), 1);

            var defuseProgress =
                CalculateProgressNormalized(DefuseBegin, Networking.GetNetworkDateTime(), DefuseComplete);
            if (defuserText != null) defuserText.DefuseProgress(defuseProgress);
            if (animator != null) animator.SetFloat(_defuserProgressAnim, defuseProgress);

            if (!Networking.IsMaster) return;

            if (DefuseComplete < Networking.GetNetworkDateTime() && !defuserPickup.IsCancelling)
            {
                State = DefuserState.Defused;
                Sync();
            }
        }

        private int GetCurrentTeamId()
        {
            if (Utilities.IsValid(defuserPickup.pickup.currentPlayer))
            {
                var carrier = playerManager.GetPlayerById(defuserPickup.pickup.currentPlayer.playerId);
                if (carrier != null) return carrier.TeamId;
            }

            var planter = playerManager.GetPlayerById(Planter);
            if (planter != null) return planter.TeamId;

            var owner = playerManager.GetPlayerById(Networking.GetOwner(gameObject).playerId);
            if (owner != null) return owner.TeamId;

            return 0;
        }

        private void UpdateIconColor()
        {
            ChangeIconColor(playerManager.GetTeamColor(GetCurrentTeamId()));
        }

        private void ChangeIconColor(Color color)
        {
            if (iconParticle == null) return;
            var main = iconParticle.main;
            main.startColor = color;
        }

        public void ShowIconForTeam()
        {
            ShowIcon = true;
            Sync();
        }

        public void HideIconForTeam()
        {
            ShowIcon = false;
            Sync();
        }

        public bool CanDefuseAsTeam(int teamId)
        {
            return teamId != PlanterTeamId || teamId == 0 || playerManager.IsStaffTeamId(teamId);
        }

        private void OnStateChanged(DefuserState old, DefuserState next)
        {
            Debug.Log($"State changed from {old} to {next}");
            defuserPickup.OnStateChanged(old, next);

            switch (next)
            {
                case DefuserState.Opened:
                case DefuserState.Idle:
                {
                    if (animator != null)
                    {
                        animator.SetFloat(_defuserProgressAnim, 0);
                        animator.SetFloat(_defuserPlantProgressAnim, 0);
                    }

                    if (defuserText != null)
                        defuserText.DrawIdle(VRCPlayerApi.GetPlayerById(Planter).SafeGetDisplayName("root"));

                    break;
                }
                case DefuserState.PlantingTimer:
                {
                    if (animator != null)
                    {
                        animator.SetFloat(_defuserProgressAnim, 0);
                        animator.SetFloat(_defuserPlantProgressAnim, 1F);
                    }

                    PlantTimerCoroutine();
                    break;
                }
                case DefuserState.PlantingUser:
                {
                    if (animator != null)
                    {
                        animator.SetFloat(_defuserProgressAnim, 0);
                        animator.SetFloat(_defuserPlantProgressAnim, 1F);
                    }

                    if (defuserText != null) defuserText.PlantReady();
                    audioManager.PlayAudioAtTransform(defuseReadyAudio, transform);
                    break;
                }
                case DefuserState.Defusing:
                {
                    if (animator != null)
                    {
                        animator.SetFloat(_defuserProgressAnim, 0);
                        animator.SetFloat(_defuserPlantProgressAnim, 1F);
                    }

                    DefusingTimerCoroutine();
                    break;
                }
                case DefuserState.Defused:
                {
                    audioManager.PlayAudioAtTransform(defuseCompleteAudio, transform);
                    break;
                }
            }

            switch (old)
            {
                case DefuserState.PlantingTimer:
                {
                    if (next == DefuserState.PlantingUser)
                    {
                        if (defuserText != null) defuserText.PlantReady();
                        audioManager.PlayAudioAtTransform(defuseReadyAudio, transform);
                    }
                    else
                    {
                        if (defuserText != null)
                            defuserText.Abort(VRCPlayerApi.GetPlayerById(Planter).SafeGetDisplayName("root"));
                    }

                    break;
                }
                case DefuserState.Defusing:
                {
                    if (next == DefuserState.Idle) audioManager.PlayAudioAtTransform(defuseCancelAudio, transform);

                    break;
                }
            }

            if (animator != null) animator.SetInteger(_defuserStateAnim, (int)next);
        }

        private void Sync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private static float CalculateProgressNormalized(DateTime start, DateTime now, DateTime complete)
        {
            var total = complete.Subtract(start).TotalSeconds;
            var elapsed = now.Subtract(start).TotalSeconds;
            if (elapsed <= 0) return 0F;
            if (elapsed >= total) return 1F;

            return (float)(elapsed / total);
        }
    }

    public enum DefuserState
    {
        Idle,
        Opened,
        PlantingTimer,
        PlantingUser,
        Defusing,
        Defused
    }
}
