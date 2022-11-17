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
    [DefaultExecutionOrder(30)] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
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

        [SerializeField]
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

        private bool _invokeOnDeathNextOnDeserialization;
        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedDeaths))]
        private int _syncedDeaths = -1;
        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedLastAttackerPlayerId))]
        private int _syncedLastAttackerPlayerId = -1;
        [NonSerialized] [UdonSynced]
        private long _syncedLastDiedTimeTicks = 0;

        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedPlayerId))]
        private int _syncedPlayerId = -1;
        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(SyncedTeamId))]
        private int _syncedTeamId = 0;

        public override VRCPlayerApi VrcPlayer => _cachedVrcPlayerApi;

        public override int PlayerId => SyncedPlayerId;

        public override int TeamId => SyncedTeamId;

        public override int Deaths
        {
            get => SyncedDeaths;
            set => SyncedDeaths = value;
        }

        public override long LastDiedTimeTicks => _syncedLastDiedTimeTicks;

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

        public int SyncedDeaths
        {
            get => _syncedDeaths;
            protected set
            {
                var lastSyncedDeaths = _syncedDeaths;
                _syncedDeaths = value;

                if (lastSyncedDeaths == -1 || lastSyncedDeaths == value)
                    return;

                _invokeOnDeathNextOnDeserialization = true;
            }
        }

        public int SyncedLastAttackerPlayerId
        {
            get => _syncedLastAttackerPlayerId;
            protected set => _syncedLastAttackerPlayerId = value;
        }

        public override RoleData Role => _cachedRoleData;

        private void Start()
        {
            if (playerManager == null)
            {
                playerManager = CenturionSystemReference.GetPlayerManager();
                _audioManager = playerManager.AudioManager;
                _footstepAudio = playerManager.FootstepAudio;
            }

            playerTag.Init(this, playerManager);
            playerHumanoidCollider.Init(this, playerManager);

            if (Networking.IsMaster)
                ResetPlayer();

            playerManager.UpdateManager.SubscribeFixedUpdate(this);
            playerManager.UpdateManager.SubscribeSlowFixedUpdate(this);
        }

        public override void OnDeserialization()
        {
            CheckDiff();
        }

        public override void OnPreSerialization()
        {
            CheckDiff();
        }

        private void CheckDiff()
        {
            if (_invokeOnDeathNextOnDeserialization)
            {
                _invokeOnDeathNextOnDeserialization = false;
                var attacker = playerManager.GetPlayerById(SyncedLastAttackerPlayerId);
                if (attacker == null)
                {
                    playerManager.Logger.LogError(
                        $"{Prefix}{Index}: Failed to get attacker {SyncedLastAttackerPlayerId} for {NewbieUtils.GetPlayerName(VrcPlayer)}!");
                    return;
                }

                ++attacker.Kills;

                playerManager.Invoke_OnKilled(attacker, this);
                UpdateView();
            }
        }

        public void _FixedUpdate()
        {
            if (!IsAssigned) return;

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
            if (playerCollider == null || data == null)
            {
                playerManager.Logger.LogError(
                    $"{Prefix}OnDamage: PlayerCollider or DamageData were null! will not check!");
                return;
            }

            if (!data.ShouldApplyDamage)
            {
                playerManager.Logger.LogVerbose(
                    $"{Prefix}OnDamage: Will ignore damage because ShouldApplyDamage == false");
                return;
            }

            var attacker = playerManager.GetPlayerById(data.DamagerPlayerId);

            if (attacker == null)
            {
                playerManager.Logger.LogVerbose(
                    $"{Prefix}OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because attacker is null");
                return;
            }

            if (PlayerId == attacker.PlayerId)
            {
                playerManager.Logger.LogVerbose(
                    $"{Prefix}OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because self shooting");
                return;
            }

            if (!IsLocal && !attacker.IsLocal)
            {
                playerManager.Logger.LogVerbose(
                    $"{Prefix}OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because neither of players are local player");
                return;
            }

            if (TeamId == attacker.TeamId)
            {
                playerManager.Invoke_OnFriendlyFire(attacker, this);
                if (TeamId != 0 && !playerManager.AllowFriendlyFire)
                {
                    playerManager.Logger.LogVerbose(
                        $"{Prefix}OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because attacker {NewbieUtils.GetPlayerName(attacker.VrcPlayer)} is in same team");
                    return;
                }
            }

            if (Networking.GetNetworkDateTime().Subtract(LastDiedDateTime).TotalSeconds < 5F)
            {
                playerManager.Logger.LogVerbose(
                    $"{Prefix}Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because that player has been hit recently");
                return;
            }

            ++Deaths;
            SyncedLastAttackerPlayerId = attacker.PlayerId;
            _syncedLastDiedTimeTicks = Networking.GetNetworkDateTime().Ticks;

            Sync();

            playerManager.Invoke_OnHitDetection(playerCollider, data, contactPoint, attacker.IsLocal);
        }

        public override void OnDeath()
        {
            if (IsLocal)
                playerManager.LocalHitEffect.Play();
            else
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