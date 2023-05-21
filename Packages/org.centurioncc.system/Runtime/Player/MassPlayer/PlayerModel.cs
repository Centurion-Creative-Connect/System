using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.MassPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerModel : PlayerBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [CanBeNull]
        public PlayerViewBase playerView;
        private AudioManager _audioManager;
        private RoleData _cachedRoleData;

        private FootstepAudioStore _footstepAudio;
        private bool _invokeOnDeathNextOnDeserialization;
        [UdonSynced] [FieldChangeCallback(nameof(SyncedDeaths))]
        private int _syncedDeaths;
        [UdonSynced]
        private long _syncedLastDiedTimeTicks = 0;
        [UdonSynced] [FieldChangeCallback(nameof(SyncedPlayerId))]
        private int _syncedPlayerId = -1;
        [UdonSynced] [FieldChangeCallback(nameof(SyncedTeamId))]
        private int _syncedTeamId;
        public VRCPlayerApi cachedVrcPlayerApi;

        public int SyncedPlayerId
        {
            get => _syncedPlayerId;
            protected set
            {
                var lastPlayerId = _syncedPlayerId;
                var lastRole = Role;
                var lastAssigned = IsAssigned;

                _syncedPlayerId = value;
                cachedVrcPlayerApi = VRCPlayerApi.GetPlayerById(value);
                _cachedRoleData = playerManager.RoleManager.GetPlayerRole(cachedVrcPlayerApi);

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

                if (lastSyncedDeaths < 0 || lastSyncedDeaths == value || value == 0)
                    return;

                _invokeOnDeathNextOnDeserialization = true;
            }
        }

        [field: UdonSynced]
        [field: FieldChangeCallback(nameof(SyncedLastAttackerPlayerId))]
        public int SyncedLastAttackerPlayerId { get; protected set; } = -1;

        public override long LastDiedTimeTicks => _syncedLastDiedTimeTicks;
        public override int PlayerId => SyncedPlayerId;
        public override int TeamId => SyncedTeamId;
        public override int Deaths
        {
            get => SyncedDeaths;
            set => SyncedDeaths = value;
        }

        public override bool IsAssigned => VrcPlayer != null && VrcPlayer.IsValid();
        public override VRCPlayerApi VrcPlayer => cachedVrcPlayerApi;
        public override RoleData Role => _cachedRoleData;

        // Utilities.IsValid checks if it is null or not.
        // ReSharper disable once PossibleNullReferenceException
        public Vector3 Position => Utilities.IsValid(VrcPlayer) ? VrcPlayer.GetPosition() : Vector3.positiveInfinity;

        private void Start()
        {
            _audioManager = playerManager.AudioManager;
            _footstepAudio = playerManager.FootstepAudio;
        }

        public int ChildKeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
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
            if (!_invokeOnDeathNextOnDeserialization) return;

            _invokeOnDeathNextOnDeserialization = false;
            var attacker = playerManager.GetPlayerById(SyncedLastAttackerPlayerId);
            if (attacker == null)
            {
                playerManager.Logger.LogError(
                    $"[Player]{Index}: Failed to get attacker {SyncedLastAttackerPlayerId} for {NewbieUtils.GetPlayerName(VrcPlayer)}!");
                return;
            }

            ++attacker.Kills;

            playerManager.Invoke_OnKilled(attacker, this);
            UpdateView();
        }

        public override void SetPlayer(int vrcPlayerId)
        {
            SyncedPlayerId = vrcPlayerId;
            Sync();
        }

        public override void SetTeam(int teamId)
        {
            SyncedTeamId = teamId;
            Sync();
        }

        public override void UpdateView()
        {
            if (playerView == null)
                return;

            playerView.UpdateView();
        }

        public override void Sync()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void ResetPlayer()
        {
            ResetStats();

            SyncedPlayerId = -1;
            SyncedTeamId = 0;

            Sync();
        }

        public override void ResetStats()
        {
            SyncedDeaths = 0;
            Kills = 0;

            playerManager.Invoke_OnResetPlayerStats(this);
        }

        public override void OnDamage(PlayerCollider playerCollider, DamageData data, Vector3 contactPoint)
        {
            if (playerCollider == null || data == null)
            {
                playerManager.Logger.LogError(
                    $"[Player]OnDamage: PlayerCollider or DamageData were null! will not check!");
                return;
            }

            if (!data.ShouldApplyDamage)
            {
                playerManager.Logger.LogVerbose(
                    $"[Player]OnDamage: Will ignore damage because ShouldApplyDamage == false");
                return;
            }

            var attacker = playerManager.GetPlayerById(data.DamagerPlayerId);

            if (attacker == null)
            {
                playerManager.Logger.LogVerbose(
                    $"[Player]OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because attacker is null");
                return;
            }

            if (PlayerId == attacker.PlayerId)
            {
                playerManager.Logger.LogVerbose(
                    $"[Player]OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because self shooting");
                return;
            }

            if (!IsLocal && !attacker.IsLocal)
            {
                playerManager.Logger.LogVerbose(
                    $"[Player]OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because neither of players are local player");
                return;
            }

            if (TeamId == attacker.TeamId)
            {
                playerManager.Invoke_OnFriendlyFire(attacker, this);
                if (TeamId != 0 && !playerManager.AllowFriendlyFire)
                {
                    playerManager.Logger.LogVerbose(
                        $"[Player]OnDamage: Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because attacker {NewbieUtils.GetPlayerName(attacker.VrcPlayer)} is in same team");
                    return;
                }
            }

            if (Networking.GetNetworkDateTime().Subtract(LastDiedDateTime).TotalSeconds < 5F)
            {
                playerManager.Logger.LogVerbose(
                    $"[Player]Will ignore damage to {NewbieUtils.GetPlayerName(VrcPlayer)} because that player has been hit recently");
                return;
            }


            ++SyncedDeaths;
            SyncedLastAttackerPlayerId = attacker.PlayerId;
            _syncedLastDiedTimeTicks = Networking.GetNetworkDateTime().Ticks;

            Sync();

            playerManager.Invoke_OnHitDetection(playerCollider, data, contactPoint,
                data.DamagerPlayerId == Networking.LocalPlayer.playerId);
        }

        public override void OnDeath()
        {
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