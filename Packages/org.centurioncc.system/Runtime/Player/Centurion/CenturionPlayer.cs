using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionPlayer : PlayerBase
    {
        [SerializeField] [NewbieInject]
        private RoleProvider roleProvider;

        [SerializeField] [NewbieInject]
        private CenturionPlayerManager playerManager;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private CenturionPlayerCollider[] playerColliders;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private CenturionPlayerColliderSimple simpleCollider;

        [SerializeField] [NewbieInject]
        private CenturionPlayerTag[] playerTags;

        private readonly DataList _playerAreas = new DataList();

        private short _deaths;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedHealth))]
        private float _health = 100;

        private bool _isInSafeZone;

        private short _kills;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedMaxHealth))]
        private float _maxHealth = 100;

        private short _score;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedTeamId))]
        private byte _teamId;

        private byte SyncedTeamId
        {
            get => _teamId;
            set
            {
                var lastTeam = _teamId;
                _teamId = value;
                if (lastTeam == _teamId) return;

                playerManager.Invoke_OnPlayerTeamChanged(this, lastTeam);
                UpdateView();
            }
        }

        private float SyncedHealth
        {
            get => _health;
            set
            {
                var lastIsDead = IsDead;
                var lastHealth = _health;
                _health = value;

                playerManager.Invoke_OnPlayerHealthChanged(this, lastHealth);
                if (lastIsDead == IsDead) return;

                if (IsDead)
                {
                    var attacker = (CenturionPlayer)playerManager.GetPlayerById(LastDamageInfo.AttackerId());
                    var type = LastDamageInfo.AttackerId() == PlayerId
                        ? KillType.ReverseFriendlyFire
                        : playerManager.IsFriendly(this, attacker)
                            ? KillType.FriendlyFire
                            : KillType.Default;

                    if (attacker)
                    {
                        ++attacker.Kills;
                        ++attacker.KillStreak;
                    }

                    KillStreak = 0;
                    ++Deaths;

                    playerManager.Invoke_OnPlayerKilled(
                        attacker,
                        this,
                        type
                    );
                }
                else
                {
                    playerManager.Invoke_OnPlayerRevived(this);
                }
            }
        }

        private float SyncedMaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = value;
                playerManager.Invoke_OnPlayerHealthChanged(this, SyncedHealth);
            }
        }

        public override string DisplayName =>
            $"<color=#{playerManager.GetTeamColor(TeamId).ToHtmlStringRGBA()}>{VrcPlayer.SafeGetDisplayName()}</color>";

        public override int PlayerId => Networking.GetOwner(gameObject).playerId;

        public override int Kills
        {
            get => _kills;
            protected set
            {
                var changed = _kills != value;
                _kills = (short)value;

                if (changed) playerManager.Invoke_OnPlayerStatsChanged(this);
            }
        }

        public override int Deaths
        {
            get => _deaths;
            protected set
            {
                var changed = _deaths != value;
                _deaths = (short)value;

                if (changed) playerManager.Invoke_OnPlayerStatsChanged(this);
            }
        }

        public override int Score
        {
            get => _score;
            set
            {
                var changed = _score != value;
                _score = (short)value;

                if (changed) playerManager.Invoke_OnPlayerStatsChanged(this);
            }
        }

        public override int KillStreak { get; protected set; }

        public override float Health => SyncedHealth;
        public override float MaxHealth => _maxHealth;
        public override int TeamId => _teamId;
        public override bool IsDead => _health <= 0;
        public override bool IsInSafeZone => _isInSafeZone;
        public override VRCPlayerApi VrcPlayer => Networking.GetOwner(gameObject);
        public override RoleData[] Roles => roleProvider.GetPlayerRoles(VrcPlayer);
        public bool IsCulled { get; set; }

        private void Start()
        {
            LastDamageInfo = DamageInfo.NewEmpty();
            playerManager.Invoke_OnPlayerAdded(this);
        }

        private void OnDestroy()
        {
            playerManager.Invoke_OnPlayerRemoved(this);
        }

        public override void SetTeam(int teamId)
        {
            if (!IsLocal)
            {
                playerManager.RequestTeamChangeBroadcast(PlayerId, teamId);
                return;
            }

            SyncedTeamId = (byte)teamId;
            RequestSerialization();
        }

        public override void SetHealth(float health)
        {
            if (!IsLocal)
            {
                playerManager.RequestHealthChangeBroadcast(PlayerId, health);
                return;
            }

            SyncedHealth = health;
            RequestSerialization();
        }

        public override void SetMaxHealth(float maxHealth)
        {
            if (!IsLocal)
            {
                playerManager.RequestMaxHealthChangeBroadcast(PlayerId, maxHealth);
                return;
            }

            SyncedMaxHealth = maxHealth;
            RequestSerialization();
        }

        public override void OnLocalHit(PlayerColliderBase playerCollider, DamageData data, Vector3 contactPoint)
        {
            if (!IsLocal && data.DamagerPlayerId != Networking.LocalPlayer.playerId)
            {
                return;
            }

            var damageInfo = DamageInfo.New(VrcPlayer, contactPoint, playerCollider.BodyParts, data);
            playerManager.RequestDamageBroadcast(damageInfo);
        }

        public override void ApplyDamage(DamageInfo info)
        {
            LastDamageInfo = info;

            if (!IsLocal)
            {
                return;
            }

            SyncedHealth -= info.DamageAmount();
            RequestSerialization();
        }

        public override void UpdateView()
        {
            foreach (var col in playerColliders)
            {
                if (!col) continue;
                col.gameObject.SetActive(!IsCulled);
                col.PostLateUpdate();
                col.IsDebugVisible = playerManager.IsDebug;
            }

            simpleCollider.gameObject.SetActive(IsCulled);
            simpleCollider.PostLateUpdate();
            simpleCollider.IsDebugVisible = playerManager.IsDebug;

            foreach (var playerTag in playerTags)
            {
                if (!playerTag) continue;
                playerTag.Refresh();
            }
        }

        public override void ResetToDefault()
        {
            if (!IsLocal)
            {
                playerManager.RequestResetToDefaultBroadcast(PlayerId);
                return;
            }

            ResetStats();
            SyncedTeamId = 0;
            SyncedMaxHealth = 100;
            SyncedHealth = SyncedMaxHealth;

            playerManager.Invoke_OnPlayerReset(this);
            RequestSerialization();
        }

        public override void ResetStats()
        {
            if (!IsLocal)
            {
                playerManager.RequestResetStatsBroadcast(PlayerId);
                return;
            }

            Deaths = 0;
            Kills = 0;
            Score = 0;
            RequestSerialization();
        }

        public override void Kill()
        {
            if (!IsLocal)
            {
                playerManager.RequestKillBroadcast(PlayerId);
                return;
            }

            SyncedHealth = 0;
            RequestSerialization();
        }

        public override void Revive()
        {
            if (!IsLocal)
            {
                playerManager.RequestReviveBroadcast(PlayerId);
                return;
            }

            SyncedHealth = SyncedMaxHealth;
            RequestSerialization();
        }

        public override void OnAreaEnter(PlayerAreaBase area)
        {
            _playerAreas.Add(area);
            UpdateSafeZoneStatus();
            UpdateView();
            playerManager.Invoke_OnPlayerEnteredArea(this, area);
        }

        public override void OnAreaExit(PlayerAreaBase area)
        {
            _playerAreas.RemoveAll(area);
            UpdateSafeZoneStatus();
            UpdateView();
            playerManager.Invoke_OnPlayerExitedArea(this, area);
        }

        public override PlayerAreaBase[] GetCurrentPlayerAreas()
        {
            var result = new PlayerAreaBase[_playerAreas.Count];
            for (var i = 0; i < _playerAreas.Count; i++)
            {
                result[i] = (PlayerAreaBase)_playerAreas[i].Reference;
            }

            return result;
        }

        private void UpdateSafeZoneStatus()
        {
            _isInSafeZone = false;

            var playerAreas = GetCurrentPlayerAreas();
            foreach (var playerAreaBase in playerAreas)
            {
                if (!playerAreaBase.IsSafeZone) continue;
                _isInSafeZone = true;
                return;
            }
        }
    }
}