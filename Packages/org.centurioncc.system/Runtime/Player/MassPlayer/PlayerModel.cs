using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
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
        [UdonSynced] [FieldChangeCallback(nameof(SyncedIsDead))]
        private bool _syncedIsDead;
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
        public bool SyncedIsDead
        {
            get => _syncedIsDead;
            protected set
            {
                var lastDied = _syncedIsDead;

                _syncedIsDead = value;

                if (!_syncedIsDead && lastDied != _syncedIsDead)
                    playerManager.Invoke_OnPlayerRevived(this);

                UpdateView();
            }
        }

        public override int PlayerId => SyncedPlayerId;
        public override int TeamId => SyncedTeamId;
        public override bool IsDead => SyncedIsDead;

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
            SyncedIsDead = false;
            Deaths = 0;
            Kills = 0;
            LastHitData.ResetData();

            playerManager.Invoke_OnResetPlayerStats(this);
        }

        public override void OnDamage(PlayerCollider playerCollider, DamageData data, Vector3 contactPoint)
        {
            playerManager.Invoke_OnHitDetection(playerCollider, data, contactPoint);
        }

        public override void Kill()
        {
            SyncedIsDead = true;
            if (IsLocal)
                Sync();
        }

        public override void Revive()
        {
            SyncedIsDead = false;
            if (IsLocal)
                Sync();
        }

        public override void OnHitDataUpdated()
        {
            Kill();
            playerManager.Invoke_OnKilled(playerManager.GetPlayerById(LastHitData.AttackerId), this, LastHitData.Type);
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