using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [Obsolete("Use MassPlayer PlayerModel/PlayerView/PlayerUpdater instead")] [DefaultExecutionOrder(30)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ShooterPlayer : PlayerBase
    {
        private const string Prefix = "[<color=blue>Player</color>] ";
        private const string DebugString =
            "name    : {0}\n" +
            "pid     : {1}\n" +
            "ownerpid: {2}\n" +
            "active  : {3}\n" +
            "team    : {4}\n" +
            "K/D     : {5}\n" +
            "IsLWCol : {6}";

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField]
        private PlayerTag playerTag;
        [SerializeField]
        private PlayerHumanoidCollider playerHumanoidCollider;
        [SerializeField]
        private HitDisplay display;
        private AudioManager _audioManager;
        private RoleData _cachedRoleData;

        private VRCPlayerApi _cachedVrcPlayerApi;
        private FootstepAudioStore _footstepAudio;
        private bool _hasCheckedInit;

        private long _lastHitDetectionTimeTick;

        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedPlayerId))]
        private int _syncedPlayerId = -1;
        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedTeamId))]
        private int _syncedTeamId;

        public override VRCPlayerApi VrcPlayer => _cachedVrcPlayerApi;

        public override int PlayerId => SyncedPlayerId;

        public override int TeamId => SyncedTeamId;

        public override long LastDiedTimeTicks => _lastHitDetectionTimeTick;

        public override bool IsAssigned => VrcPlayer != null && VrcPlayer.IsValid();

        public int SyncedPlayerId
        {
            get => _syncedPlayerId;
            protected set
            {
                var lastPlayerId = _syncedPlayerId;
                var lastRole = Role;
                var lastAssigned = IsAssigned;

                _syncedPlayerId = value;
                _cachedVrcPlayerApi = VRCPlayerApi.GetPlayerById(value);
                _cachedRoleData = playerManager.RoleManager.GetPlayerRole(_cachedVrcPlayerApi);

                playerManager.Invoke_OnPlayerChanged(this, lastPlayerId, lastRole.HasPermission(), lastAssigned);
                if (Networking.IsMaster && lastPlayerId != value)
                    SetTeam(0);

                UpdateView();
            }
        }

        public int SyncedTeamId
        {
            get => _syncedTeamId;
            protected set
            {
                var lastTeam = _syncedTeamId;

                _syncedTeamId = value;

                if (lastTeam != _syncedTeamId)
                    playerManager.Invoke_OnTeamChanged(this, lastTeam);

                UpdateView();
            }
        }

        public override RoleData Role => _cachedRoleData;

        private void Start()
        {
            _audioManager = playerManager.AudioManager;
            _footstepAudio = playerManager.FootstepAudio;

            _EnsureInit();

            if (Networking.IsMaster)
                ResetPlayer();

            playerManager.UpdateManager.SubscribeFixedUpdate(this);
            playerManager.UpdateManager.SubscribeSlowFixedUpdate(this);
        }

        public void _FixedUpdate()
        {
            var api = VrcPlayer;
            if (api == null || !api.IsValid()) return;

            _EnsureInit();

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

            _EnsureInit();

            var localHeadTrackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var remoteHeadPos = api.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            var localHeadPos = localHeadTrackingData.position;

            playerTag.UpdatePositionAndRotation(
                localHeadPos,
                localHeadTrackingData.rotation * Vector3.forward,
                remoteHeadPos
            );
            playerHumanoidCollider.SlowUpdateCollider(api);

            if (playerManager.IsDebug)
            {
                var o = gameObject;
                playerTag.SetDebugTagText(string.Format(DebugString,
                    o.name,
                    PlayerId,
                    Networking.GetOwner(o).playerId,
                    IsAssigned,
                    TeamId,
                    $"{Kills} / {Deaths}",
                    playerHumanoidCollider.IsUsingLightweightCollider)
                );
            }
        }

        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public override void SetPlayer(int id)
        {
            if (!Networking.IsMaster)
                return;

            SyncedPlayerId = id;

            Sync();
        }

        public override void SetTeam(int team)
        {
            if (!Networking.IsMaster)
                return;

            SyncedTeamId = team;

            Sync();
        }

        public override void UpdateView()
        {
            _EnsureInit();

            playerTag.UpdateView();
            playerHumanoidCollider.UpdateView();
        }

        public override void Sync()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void ResetPlayer()
        {
            if (!Networking.IsMaster)
                return;

            ResetStats();

            SyncedPlayerId = -1;
            SyncedTeamId = 0;

            Sync();
        }

        public override void ResetStats()
        {
            Kills = 0;
            Deaths = 0;

            playerManager.Invoke_OnResetPlayerStats(this);
        }

        public override void OnDamage(PlayerCollider playerCollider, DamageData data, Vector3 contactPoint)
        {
            var networkNow = Networking.GetNetworkDateTime();
            if (networkNow.Subtract(LastDiedDateTime).TotalSeconds > 10F)
            {
                _lastHitDetectionTimeTick = networkNow.Ticks;
                playerManager.Logger.LogVerbose(
                    $"[Player]OnDamage: Updated last hit detection time tick for {NewbieUtils.GetPlayerName(VrcPlayer)}");
            }

            playerManager.Invoke_OnHitDetection(playerCollider, data, contactPoint);
        }

        public override void OnDeath()
        {
            if (!IsLocal)
                display.Play();
        }

        private void _EnsureInit()
        {
            if (_hasCheckedInit)
                return;

            if (!playerTag.HasInitialized)
                playerTag.Init(this, playerManager);
            if (!playerHumanoidCollider.HasInitialized)
                playerHumanoidCollider.Init(this, playerManager);

            _hasCheckedInit = true;
        }

        #region PlayFootstepMethods

        public void PlayPrototypeFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Prototype, false);
        }

        public void PlaySlowPrototypeFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Prototype, true);
        }

        public void PlayGravelFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Gravel, false);
        }

        public void PlaySlowGravelFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Gravel, true);
        }

        public void PlayWoodFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Wood, false);
        }

        public void PlaySlowWoodFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Wood, true);
        }

        public void PlayMetallicFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Metallic, false);
        }

        public void PlaySlowMetallicFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Metallic, true);
        }

        public void PlayDirtFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Dirt, false);
        }

        public void PlaySlowDirtFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Dirt, true);
        }

        public void PlayConcreteFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Concrete, false);
        }

        public void PlaySlowConcreteFootstepAudio()
        {
            _PlayFootstepAudio(ObjectType.Concrete, true);
        }

        private void _PlayFootstepAudio(ObjectType type, bool isSlow)
        {
            if (_footstepAudio != null && VrcPlayer != null && Utilities.IsValid(VrcPlayer))
                _audioManager.PlayAudioAtPosition(_footstepAudio.Get(type, isSlow), VrcPlayer.GetPosition());
        }

        #endregion
    }
}