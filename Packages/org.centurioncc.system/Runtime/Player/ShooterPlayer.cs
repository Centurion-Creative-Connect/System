using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common.Role;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [DefaultExecutionOrder(30)] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ShooterPlayer : UdonSharpBehaviour
    {
        private const string Prefix = "[<color=blue>Player</color>] ";
        private const string DebugString =
            "name    : {0}\n" +
            "pid     : {1}\n" +
            "ownerpid: {2}\n" +
            "active  : {3}\n" +
            "team    : {4}\n" +
            "K/D     : {5}\n" +
            "<size=8>{6}</size>";

        [SerializeField]
        private PlayerManager playerManager;
        [SerializeField]
        private PlayerTag playerTag;
        [SerializeField]
        private PlayerStats playerStats;
        [SerializeField]
        private PlayerHumanoidCollider playerHumanoidCollider;
        [SerializeField]
        private HitDisplay display;

        private AudioManager _audioManager;
        private VRCPlayerApi _cachedApi;
        private FootstepAudioStore _footstepAudio;

        private int _lastCachedPlayerId;

        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedPlayerId))]
        private int _syncedPlayerId = -1;
        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(Team))]
        private int _team = 0;

        [NonSerialized]
        public int Index;


        public VRCPlayerApi VrcPlayer
        {
            get
            {
                if (_lastCachedPlayerId != SyncedPlayerId)
                {
                    var api = VRCPlayerApi.GetPlayerById(SyncedPlayerId);
                    _cachedApi = api;
                    _lastCachedPlayerId = SyncedPlayerId;
                }

                return _cachedApi;
            }
        }

        public int SyncedPlayerId
        {
            get => _syncedPlayerId;
            private set
            {
                var lastPlayerId = _syncedPlayerId;
                var lastRole = Role;
                var lastIsActive = IsActive;

                _syncedPlayerId = value;
                Role = playerManager.RoleManager.GetPlayerRole(VRCPlayerApi.GetPlayerById(value));
                IsActive = VrcPlayer != null && VrcPlayer.IsValid();
                playerHumanoidCollider.IsCollidersActive = IsActive;

                playerManager.Invoke_OnPlayerChanged(this, lastPlayerId, lastRole.HasPermission(), lastIsActive);
                if (Networking.IsMaster && lastPlayerId != value)
                    MasterOnly_SetTeam(0);

                playerTag.SetTeamTagColor(playerManager.GetTeamColor(Team));
                playerTag.UpdateView();
            }
        }

        public PlayerStats PlayerStats => playerStats;

        public PlayerTag PlayerTag => playerTag;

        public PlayerHumanoidCollider PlayerHumanoidCollider => playerHumanoidCollider;

        public int Team
        {
            get => _team;
            private set
            {
                var lastTeam = _team;

                _team = value;

                if (lastTeam != _team)
                    playerManager.Invoke_OnTeamChanged(this, lastTeam);
                playerTag.SetTeamTagColor(playerManager.GetTeamColor(value));
            }
        }

        [CanBeNull]
        public RoleData Role { get; private set; }

        public bool IsLocal => SyncedPlayerId == Networking.LocalPlayer.playerId;

        public bool IsActive { get; private set; }

        private void Start()
        {
            if (playerManager == null)
            {
                var o = GameObject.Find("PlayerManager");
                playerManager = o.GetComponent<PlayerManager>();
                _audioManager = playerManager.AudioManager;
                _footstepAudio = playerManager.FootstepAudio;
            }

            playerStats.Init(this, playerManager);
            playerTag.Init(this);
            playerHumanoidCollider.Init(this);

            if (Networking.IsMaster)
                MasterOnly_Reset();

            playerManager.UpdateManager.SubscribeFixedUpdate(this);
            playerManager.UpdateManager.SubscribeSlowFixedUpdate(this);
        }

        public void _FixedUpdate()
        {
            if (!IsActive) return;

            var api = VrcPlayer;
            if (api == null || !api.IsValid())
                return;

            transform.SetPositionAndRotation(
                api.GetPosition(),
                api.GetRotation());
            playerHumanoidCollider.UpdateCollider(api);
        }

        public void _SlowFixedUpdate()
        {
            var api = VrcPlayer;
            if (api == null || !api.IsValid())
                return;

            var localHeadTrackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var remoteHeadPos = api.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            var localHeadPos = localHeadTrackingData.position;

            playerTag.UpdatePositionAndRotation(
                localHeadPos,
                localHeadTrackingData.rotation * Vector3.forward,
                remoteHeadPos
            );
            playerHumanoidCollider.SlowUpdateCollider(api);

            if (playerTag.IsDebugTagShown)
            {
                var o = gameObject;
                playerTag.SetDebugTagText(string.Format(DebugString,
                    o.name,
                    SyncedPlayerId,
                    Networking.GetOwner(o).playerId,
                    IsActive,
                    Team,
                    $"{PlayerStats.Kill} / {PlayerStats.Death}",
                    playerHumanoidCollider.GetDebugString())
                );
            }
        }

        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public void MasterOnly_SetPlayer(int id)
        {
            if (!Networking.IsMaster)
                return;

            SyncedPlayerId = id;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void MasterOnly_SetTeam(int team)
        {
            if (!Networking.IsMaster)
                return;

            Team = team;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void MasterOnly_Sync()
        {
            if (!Networking.IsMaster)
                return;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void MasterOnly_Reset()
        {
            if (!Networking.IsMaster)
                return;

            SyncedPlayerId = -1;
            Team = 0;

            playerStats.ResetStats();

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void OnPlayerColliderEnter(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint)
        {
            var localPlayerId = Networking.LocalPlayer.playerId;
            var isLocalDamager = damageData.DamagerPlayerId == localPlayerId;

            playerManager.Invoke_OnHitDetection(playerCollider, damageData, contactPoint, isLocalDamager);
        }

        public void PlayHit()
        {
            display.Play();
        }

        #region PlayFootstepMethods

        public void PlayFallbackFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.FallbackAudio);
        }

        public void PlaySlowFallbackFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.SlowFallbackAudio);
        }

        public void PlayGroundFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.GroundAudio);
        }

        public void PlaySlowGroundFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.SlowGroundAudio);
        }

        public void PlayWoodFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.WoodAudio);
        }

        public void PlaySlowWoodFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.SlowWoodAudio);
        }

        public void PlayIronFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.IronAudio);
        }

        public void PlaySlowIronFootstepAudio()
        {
            if (_footstepAudio)
                _PlayFootstepAudio(_footstepAudio.SlowIronAudio);
        }

        private void _PlayFootstepAudio(AudioDataStore audioData)
        {
            if (audioData != null)
                _audioManager.PlayAudioAtPosition(
                    audioData.Clip, transform.position, audioData.Volume, audioData.Pitch);
        }

        #endregion
    }
}