﻿using CenturionCC.System.Audio;
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

        private AudioManager _audioManager;
        private RoleData _cachedRoleData;

        private VRCPlayerApi _cachedVrcPlayerApi;

        private FootstepAudioStore _footstepAudio;

        [CanBeNull]
        private PlayerViewBase _playerView;

        private bool _playerViewNull = true;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedIsDead))]
        private bool _syncedIsDead;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedPlayerId))]
        private int _syncedPlayerId = -1;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedTeamId))]
        private int _syncedTeamId;

        [CanBeNull]
        public PlayerViewBase PlayerView
        {
            get => _playerView;
            set
            {
                _playerView = value;
                _playerViewNull = !value;
            }
        }

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

        public bool SyncedIsDead
        {
            get => _syncedIsDead;
            protected set
            {
                var lastDied = _syncedIsDead;
                _syncedIsDead = value;

                if (!_syncedIsDead && (lastDied != _syncedIsDead))
                    playerManager.Invoke_OnPlayerRevived(this);

                UpdateView();
            }
        }

        public override int PlayerId => SyncedPlayerId;
        public override int TeamId => SyncedTeamId;
        public override bool IsDead => SyncedIsDead;

        public override bool IsAssigned => VrcPlayer != null && VrcPlayer.IsValid();
        public override VRCPlayerApi VrcPlayer => _cachedVrcPlayerApi;
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
            ResetPlayer();
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
            if (_playerViewNull) return;
            // playerViewNull will be set when playerView was changed
            // ReSharper disable once PossibleNullReferenceException
            _playerView.UpdateTarget();
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
            SyncedIsDead = false;

            Sync();
        }

        public override void ResetStats()
        {
            Deaths = 0;
            Kills = 0;
            LastHitData.ResetData();
            // Making sure
            SyncedIsDead = false;

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
            var attacker = playerManager.GetPlayerById(LastHitData.AttackerId);
            if (attacker == null)
            {
                playerManager.Logger.LogError($"[PlayerModel] Could not find attacker {LastHitData.AttackerId}");
                return;
            }

            Kill();

            ++Deaths;
            ++attacker.Kills;

            playerManager.Invoke_OnKilled(attacker, this, LastHitData.Type);
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