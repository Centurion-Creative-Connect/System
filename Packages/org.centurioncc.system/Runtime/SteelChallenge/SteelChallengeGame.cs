using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.SteelChallenge
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SteelChallengeGame : UdonSharpBehaviour
    {
        public bool useAnnouncer;

        [SerializeField]
        private Transform shootingBoxReference;
        [SerializeField]
        private SteelChallengeLeaderboard leaderboard;
        [SerializeField]
        private SteelChallengeResultMenu resultMenu;
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private Text timer;
        [SerializeField]
        private Text descriptionText;
        [SerializeField] [TextArea(2, 5)]
        private string makeReadyMessage = "Make ready!";
        [SerializeField] [TextArea(2, 5)]
        private string pleaseMakeReadyMessage = "Please hold your hands up!";
        [SerializeField] [TextArea(2, 5)]
        private string pleaseBeInsideShootingBoxMessage = "Please get inside of white box!";
        [SerializeField] [TextArea(2, 5)]
        private string areYouReadyMessage = "Are you ready?";
        [SerializeField] [TextArea(2, 5)]
        private string standByMessage = "Stand by...";
        [SerializeField] [Tooltip("Target at index 0 will be the last target to shoot")]
        private SteelTarget[] targets;
        [Header("Announcer Voice")]
        [SerializeField]
        private AudioDataStore makeReadyVoice;
        [SerializeField]
        private AudioDataStore pleaseMakeReadyVoice;
        [SerializeField]
        private AudioDataStore pleaseMakeReadyDesktopVoice;
        [SerializeField]
        private AudioDataStore pleaseBeInsideShootingBoxVoice;
        [SerializeField]
        private AudioDataStore areYouReadyVoice;
        [SerializeField]
        private AudioDataStore standByVoice;
        [Header("SFX")]
        [SerializeField]
        private AudioDataStore hitSound;
        [SerializeField]
        private AudioDataStore startSignalSound;

        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;

        private readonly int _animatorGameState = Animator.StringToHash("GameState");
        private readonly int _animatorIsVR = Animator.StringToHash("IsVR");

        [UdonSynced] [FieldChangeCallback(nameof(GameEndTimeCallback))]
        private long _gameEndTime;
        [UdonSynced]
        private long _gameStartTime;

        [UdonSynced] [FieldChangeCallback(nameof(RawGameState))]
        private int _gameState;
        private bool _hasFalseStart;
        private bool _hasFootFaults;
        private int _hitCount;
        private DateTime[] _hitTimes;

        private DateTime _lastStateChangedTime;

        private DateTime GameStartDateTime
        {
            get => new DateTime(_gameStartTime);
            set => _gameStartTime = value.Ticks;
        }
        private DateTime GameEndDateTime
        {
            get => new DateTime(_gameEndTime);
            set => _gameEndTime = value.Ticks;
        }

        private long GameEndTimeCallback
        {
            set
            {
                _gameEndTime = value;
                timer.text = _GetSecondsFromTimeSpan(GameEndDateTime.Subtract(GameStartDateTime));
            }
        }

        private ScGameState GameState
        {
            get => (ScGameState)_gameState;
            set => RawGameState = (int)value;
        }

        private int RawGameState
        {
            get => _gameState;
            set
            {
                _gameState = value;
                _lastStateChangedTime = DateTime.Now;
                _UpdateAnimator();
            }
        }

        private void Start()
        {
            if (shootingBoxReference == null)
                shootingBoxReference = transform;
            _hitTimes = new DateTime[targets.Length];
            foreach (var target in targets)
                target.game = this;
            _UpdateAnimator();
        }

        private void Update()
        {
            if (GameState == ScGameState.Idle) return;
            if (Networking.IsOwner(gameObject))
                switch (GameState)
                {
                    case ScGameState.Idle:
                        return;
                    case ScGameState.Entry:
                    {
                        // Wait for announce
                        if (DateTime.Now.Subtract(_lastStateChangedTime).TotalSeconds <= 2.5D) return;

                        if (!_EnsureFootsInside(5D) || !_EnsureReadyPose(10D))
                            return;

                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayAreYouReady));
                        GameState = ScGameState.CheckReady;
                        RequestSerialization();
                        return;
                    }
                    case ScGameState.CheckReady:
                    {
                        // Wait for announce
                        if (DateTime.Now.Subtract(_lastStateChangedTime).TotalSeconds <= 2D) return;

                        if (!_EnsureReadyPose(5D))
                            return;

                        GameStartDateTime = DateTime.Now.AddSeconds(UnityEngine.Random.Range(1F, 4F));
                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStandBy));
                        GameState = ScGameState.StandBy;
                        RequestSerialization();
                        return;
                    }
                    case ScGameState.StandBy:
                    {
                        if (!_IsReadyPose())
                            _hasFalseStart = true;
                        if (!_IsFootInside())
                            _hasFootFaults = true;

                        // Wait for start time
                        if (DateTime.Now.Subtract(GameStartDateTime).TotalSeconds < 0)
                            return;

                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayStartSignal));
                        GameState = ScGameState.InString;
                        RequestSerialization();
                        return;
                    }
                    case ScGameState.InString:
                    {
                        if (!_IsFootInside())
                            _hasFootFaults = true;
                        break;
                    }
                    default:
                    {
                        /* Should never be reached. */
                        Debug.LogError("[SteelChallengeGame] Unexpected error! game state is unknown!");
                        return;
                    }
                }

            if (GameState == ScGameState.InString)
                timer.text = _GetSecondsFromTimeSpan(DateTime.Now.Subtract(GameStartDateTime));

            _UpdateAnimator();
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return null;
        }

        [PublicAPI]
        public void Play()
        {
            Debug.Log("[SteelChallengeGame] Play!");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Impl_Play));
            RequestSerialization();
        }

        public void Impl_Play()
        {
            GameState = ScGameState.Entry;
            _hasFalseStart = false;
            _hasFootFaults = false;
            _hitCount = 0;
            _hitTimes = new DateTime[targets.Length];
            foreach (var target in targets)
                target.hasHit = false;
            PlayMakeReady();
            resultMenu.HideResultMenu();
        }

        [PublicAPI]
        public void Close()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Impl_Close));
        }

        public void Impl_Close()
        {
            resultMenu.HideResultMenu();
        }

        [PublicAPI]
        public void EndGame()
        {
            GameState = ScGameState.Idle;
            var now = DateTime.Now;
            var targetMisses = 0;
            foreach (var target in targets)
                if (!target.hasHit)
                    ++targetMisses;
            var hitSpans = new TimeSpan[_hitTimes.Length];
            for (var i = 0; i < hitSpans.Length; i++) hitSpans[i] = _hitTimes[i].Subtract(GameStartDateTime);

            GameEndDateTime = now.AddSeconds(3 * targetMisses);
            var span = GameEndDateTime.Subtract(GameStartDateTime);
            leaderboard.AddRecord(span, Networking.LocalPlayer);
            resultMenu.ShowResultMenu(hitSpans, targetMisses, span, _hasFootFaults, _hasFalseStart);
            timer.text = _GetSecondsFromTimeSpan(span);

            Debug.Log($"[SteelChallengeGame] Ended string with time of {span.TotalSeconds} seconds");
            RequestSerialization();
        }

        public void OnTargetHit(SteelTarget target, int shotPlayerId)
        {
            if (Networking.GetOwner(gameObject).playerId != shotPlayerId || GameState != ScGameState.InString)
                return;

            if (!target.hasHit)
            {
                var now = DateTime.Now;
                _hitTimes[_hitCount] = now;
                ++_hitCount;
                Debug.Log(
                    $"[SteelChallengeGame] Hit {_hitCount} of {targets.Length} target at {_GetSecondsFromTimeSpan(now.Subtract(GameStartDateTime))}");
            }

            target.hasHit = true;
            if (target == targets[0])
                EndGame();
        }

        private string _GetSecondsFromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 0)
                return "0.00";
            if (timeSpan.TotalSeconds >= 30)
                return "30.00";
            return timeSpan.ToString(@"s\.ff");
        }

        private bool _EnsureReadyPose(double delayUntilNotify)
        {
            if (_IsReadyPose()) return true;
            if (DateTime.Now.Subtract(_lastStateChangedTime).TotalSeconds <= delayUntilNotify) return false;
            GameState = ScGameState.Entry;
            SendCustomNetworkEvent(NetworkEventTarget.All,
                Networking.LocalPlayer.IsUserInVR()
                    ? nameof(PlayPleaseMakeReady)
                    : nameof(PlayPleaseMakeReadyDesktop));
            RequestSerialization();
            return false;
        }

        private bool _EnsureFootsInside(double delayUntilNotify)
        {
            if (_IsFootInside()) return true;
            if (DateTime.Now.Subtract(_lastStateChangedTime).TotalSeconds <= delayUntilNotify) return false;
            GameState = ScGameState.Idle;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayPleaseBeInsideBox));
            RequestSerialization();
            return false;
        }

        private bool _IsReadyPose()
        {
            var p = Networking.LocalPlayer;
            var isHoldingGun = gunManager.IsHoldingGun;
            if (!p.IsUserInVR())
            {
                var head = p.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation.eulerAngles;
                var headRotX = head.x;
                // 340 ~ 270
                return headRotX > 270 && headRotX < 340; // Looking up
            }

            var headY = p.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position.y;
            var shoulderLeftY = p.GetBonePosition(HumanBodyBones.LeftShoulder).y;
            var shoulderRightY = p.GetBonePosition(HumanBodyBones.RightShoulder).y;
            var leftHandY = p.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position.y;
            var rightHandY = p.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position.y;

            var isHandsAboveHead = headY < leftHandY && headY < rightHandY;
            var isHandsAboveShoulder = shoulderLeftY < leftHandY && shoulderRightY < rightHandY;

            return _IsHumanoid(p) ? isHandsAboveShoulder && !isHoldingGun : isHandsAboveHead && !isHoldingGun;
        }

        private bool _IsFootInside()
        {
            var p = Networking.LocalPlayer;

            var leftFoot = p.GetBonePosition(HumanBodyBones.LeftFoot);
            var rightFoot = p.GetBonePosition(HumanBodyBones.RightFoot);
            var footBounds = new Bounds(shootingBoxReference.position, new Vector3(1, 50, 1));
            var isFootInsideBounds = footBounds.Contains(leftFoot) && footBounds.Contains(rightFoot);
            var isPlayerInsideBounds = footBounds.Contains(p.GetPosition());
            return _IsHumanoid(p) ? isFootInsideBounds : isPlayerInsideBounds;
        }

        private bool _IsHumanoid(VRCPlayerApi p)
        {
            return p.GetBonePosition(HumanBodyBones.Hips) != Vector3.zero;
        }

        private void _UpdateAnimator()
        {
            animator.SetInteger(_animatorGameState, RawGameState);
            animator.SetBool(_animatorIsVR, Networking.LocalPlayer.IsUserInVR());
        }

        #region PlayAudio

        public void PlayAudioAtTarget(SteelTarget target)
        {
            _PlayAudio(hitSound, target.transform.position);
        }


        public void PlayMakeReady()
        {
            if (useAnnouncer)
                _PlayAudio(makeReadyVoice, transform.position);
            descriptionText.text = makeReadyMessage;
        }

        public void PlayPleaseMakeReady()
        {
            if (useAnnouncer)
                _PlayAudio(pleaseMakeReadyVoice, transform.position);
            descriptionText.text = pleaseMakeReadyMessage;
        }

        public void PlayPleaseBeInsideBox()
        {
            if (useAnnouncer)
                _PlayAudio(pleaseBeInsideShootingBoxVoice, transform.position);
            descriptionText.text = pleaseBeInsideShootingBoxMessage;
        }

        public void PlayPleaseMakeReadyDesktop()
        {
            if (useAnnouncer)
                _PlayAudio(pleaseMakeReadyDesktopVoice, transform.position);
            descriptionText.text = pleaseMakeReadyMessage;
        }

        public void PlayAreYouReady()
        {
            if (useAnnouncer)
                _PlayAudio(areYouReadyVoice, transform.position);
            descriptionText.text = areYouReadyMessage;
        }

        public void PlayStandBy()
        {
            if (useAnnouncer)
                _PlayAudio(standByVoice, transform.position);
            descriptionText.text = standByMessage;
        }

        public void PlayStartSignal()
        {
            _PlayAudio(startSignalSound, transform.position);
        }

        private void _PlayAudio(AudioDataStore a, Vector3 p)
        {
            audioManager.PlayAudioAtPosition(a, p);
        }

        #endregion
    }

    public enum ScGameState
    {
        Idle,
        Entry,
        CheckReady,
        StandBy,
        InString
    }
}