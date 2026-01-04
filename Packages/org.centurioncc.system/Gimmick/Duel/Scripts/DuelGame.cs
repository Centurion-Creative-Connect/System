using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Duel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DuelGame : PlayerManagerCallbackBase
    {
        [UdonSynced]
        public int teamAScore;

        [UdonSynced]
        public int teamBScore;

        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField]
        private DuelGameAnnouncer announcer;

        [SerializeField]
        private DuelGameUI ui;

        [SerializeField]
        private DuelResultsUI resultsUi;

        [SerializeField]
        public DuelGamePlayer playerA;

        [SerializeField]
        public DuelGamePlayer playerB;

        private bool _hasScheduledAnnounce;
        private bool _isGameMaster;
        private bool _isInGame;

        private DateTime _lastStateUpdated;
        private DuelGamePlayer _localPlayer;

        [UdonSynced]
        private long _matchStartingTime;

        [UdonSynced] [FieldChangeCallback(nameof(State))]
        private DuelGameState _state;

        [UdonSynced]
        private int _winningScore = 2;

        public DuelGameState State
        {
            get => _state;
            private set
            {
                var lastState = _state;
                _state = value;
                _lastStateUpdated = DateTime.Now;
                _hasScheduledAnnounce = false;
                ui.UpdateUI();

                if (lastState != _state)
                    Debug.Log($"[Duel] Game State changed from {lastState} to {_state}");
            }
        }

        public DateTime MatchStartingTime => new DateTime(_matchStartingTime);

        [field: UdonSynced]
        public int RoundCount { get; private set; }

        private void Update()
        {
            switch (State)
            {
                case DuelGameState.WaitingForPlayers:
                {
                    if (!_isGameMaster ||
                        !Utilities.IsValid(playerA.vrcPlayerApi) ||
                        !Utilities.IsValid(playerB.vrcPlayerApi)) return;

                    SetState(DuelGameState.WaitingForReady);

                    return;
                }
                case DuelGameState.WaitingForReady:
                {
                    if (!_hasScheduledAnnounce)
                    {
                        _hasScheduledAnnounce = true;
                        announcer.PlayMakeReady();
                    }

                    ui.UpdateUI();

                    if (!_isInGame) return;

                    if (!Utilities.IsValid(playerA.vrcPlayerApi) ||
                        !Utilities.IsValid(playerB.vrcPlayerApi) ||
                        _localPlayer == null) return;

                    CheckLocalPlayerReady();

                    if (!_isGameMaster || !playerA.isReady || !playerB.isReady ||
                        DateTime.Now < _lastStateUpdated.AddSeconds(1D)) return;

                    ++RoundCount;
                    _matchStartingTime = Networking.GetNetworkDateTime().AddSeconds(5D).Ticks;
                    SetState(DuelGameState.WaitingForStart);

                    return;
                }
                case DuelGameState.WaitingForStart:
                {
                    ui.UpdateUI();

                    if (!_hasScheduledAnnounce)
                    {
                        _hasScheduledAnnounce = true;
                        announcer.ScheduleStartingAnnounce(_matchStartingTime, RoundCount,
                            teamAScore + 1 == _winningScore || teamBScore + 1 == _winningScore);
                    }

                    if (_isInGame) CheckLocalPlayerReady();

                    if (_isGameMaster)
                    {
                        if (playerA.player.IsDead)
                        {
                            playerA.player.Revive();
                        }

                        if (playerB.player.IsDead)
                        {
                            playerB.player.Revive();
                        }

                        if (!playerA.isReady || !playerB.isReady)
                        {
                            announcer.CancelStartingAnnounceAll();
                            --RoundCount;
                            SetState(DuelGameState.WaitingForReady);
                            return;
                        }
                    }

                    if (Networking.GetNetworkDateTime().Ticks < _matchStartingTime) return;

                    SetState(DuelGameState.MatchInProgress);

                    return;
                }
                case DuelGameState.MatchInProgress:
                {
                    ui.UpdateUI();

                    if (!_isGameMaster) return;

                    if (playerA.player != null && playerA.player.IsDead)
                    {
                        ++teamBScore;
                        if (teamBScore >= _winningScore)
                        {
                            announcer.PlayWinnerBAll();
                            AddToResults();
                            ResetGame();
                            return;
                        }

                        SetState(DuelGameState.WaitingForReady);
                        return;
                    }

                    if (playerB.player != null && playerB.player.IsDead)
                    {
                        ++teamAScore;
                        if (teamAScore >= _winningScore)
                        {
                            announcer.PlayWinnerAAll();
                            AddToResults();
                            ResetGame();
                            return;
                        }

                        SetState(DuelGameState.WaitingForReady);
                        return;
                    }

                    return;
                }
                default:
                    return;
            }
        }

        private void SetState(DuelGameState state)
        {
            State = state;

            if (!_isGameMaster) return;

            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private bool CheckLocalPlayerReady()
        {
            if (_localPlayer == null) return false;

            var api = _localPlayer.vrcPlayerApi;
            var leftFoot = api.GetBonePosition(HumanBodyBones.LeftFoot);
            var rightFoot = api.GetBonePosition(HumanBodyBones.RightFoot);
            var bounds = _localPlayer.FootBounds.bounds;
            var isFootInBounding = bounds.Contains(leftFoot) && bounds.Contains(rightFoot);
            var locallyHeldGuns = gunManager.GetLocallyHeldGunInstances();
            var gunCount = locallyHeldGuns.Length;
            var isGunReady = gunCount == 0 || (gunCount == 1 && locallyHeldGuns[0].IsInWall);
            var isReady = isGunReady && isFootInBounding;

            if (_localPlayer.isReady != isReady)
            {
                _localPlayer.isReady = isReady;
                _localPlayer.weaponName = locallyHeldGuns.Length == 1 ? locallyHeldGuns[0].WeaponName : "???";
                _localPlayer.Sync();
            }

            return isReady;
        }

        private void UpdateInGameStatus()
        {
            _isGameMaster = Utilities.IsValid(playerA.vrcPlayerApi) && playerA.vrcPlayerApi.isLocal;
            _isInGame = (Utilities.IsValid(playerA.vrcPlayerApi) && playerA.vrcPlayerApi.isLocal) ||
                        (Utilities.IsValid(playerB.vrcPlayerApi) && playerB.vrcPlayerApi.isLocal);

            if (Utilities.IsValid(playerA.vrcPlayerApi) && playerA.vrcPlayerApi.isLocal)
                _localPlayer = playerA;

            if (Utilities.IsValid(playerB.vrcPlayerApi) && playerB.vrcPlayerApi.isLocal)
                _localPlayer = playerB;
        }

        private void AddToResults()
        {
            resultsUi._AddMatchLog(Networking.GetNetworkDateTime(), playerA, playerB, teamAScore, teamBScore);
        }

        public void OnGamePlayerJoined(DuelGamePlayer player)
        {
            UpdateInGameStatus();

            ui.UpdateUI();

            Debug.Log(
                $"[Duel] Player joined: {(Utilities.IsValid(player.vrcPlayerApi) ? $"{player.vrcPlayerApi.playerId}:{player.vrcPlayerApi.displayName}" : null)}, I am {(_isGameMaster ? "Game Master" : "NOT Game Master")}, {(_isInGame ? "In Game" : "NOT In Game")}");
        }

        public void OnGamePlayerLeft(DuelGamePlayer player)
        {
            UpdateInGameStatus();

            if (State != DuelGameState.WaitingForPlayers)
            {
                SetState(DuelGameState.WaitingForPlayers);
                ResetGame();
            }

            ui.UpdateUI();

            Debug.Log($"[Duel] Player left!");
        }

        public void ResetGame()
        {
            State = DuelGameState.WaitingForPlayers;
            _winningScore = 2;
            teamAScore = 0;
            teamBScore = 0;
            _matchStartingTime = 0;
            RoundCount = 0;
            _localPlayer = null;

            playerA.ResetPlayer();
            playerA.Sync();

            playerB.ResetPlayer();
            playerB.Sync();

            _isGameMaster = false;
            _isInGame = false;

            if (Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }

    public enum DuelGameState
    {
        WaitingForPlayers,
        WaitingForReady,
        WaitingForStart,
        MatchInProgress
    }
}
