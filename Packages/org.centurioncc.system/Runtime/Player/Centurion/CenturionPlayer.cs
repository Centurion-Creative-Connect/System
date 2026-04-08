using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

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
        private PlayerViewBase[] playerViews;

        private readonly DataList _playerAreas = new DataList();
        private short _deaths;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedHealth))]
        private float _health = 100;
        private bool _isAddedToManager;

        private bool _isCollidersActive = true;

        private bool _isInSafeZone;
        private bool _isUpdateViewScheduled;

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

                playerManager.Event.Invoke_OnPlayerTeamChanged(this, lastTeam);
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

                playerManager.Event.Invoke_OnPlayerHealthChanged(this, lastHealth);
                if (lastIsDead == IsDead) return;

                if (IsDead)
                {
                    var attacker = playerManager.GetPlayerById(LastDamageInfo.AttackerId());
                    var type = LastDamageInfo.AttackerId() == PlayerId
                        ? KillType.ReverseFriendlyFire
                        : attacker.IsFriendly(this)
                            ? KillType.FriendlyFire
                            : KillType.Default;

                    if (attacker != null && type == KillType.Default)
                    {
                        attacker.Kills += 1;
                        attacker.KillStreak += 1;
                    }

                    KillStreak = 0;
                    ++Deaths;

                    playerManager.Event.Invoke_OnPlayerKilled(
                        attacker,
                        this,
                        type
                    );
                }
                else
                {
                    playerManager.Event.Invoke_OnPlayerRevived(this);
                }
            }
        }

        private float SyncedMaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = value;
                playerManager.Event.Invoke_OnPlayerHealthChanged(this, SyncedHealth);
            }
        }

        public override string DisplayName => $"<color=#{playerManager.GetTeamColor(TeamId).ToHtmlStringRGBA()}>{VrcPlayer.SafeGetDisplayName()}</color>";

        public override int PlayerId => Networking.GetOwner(gameObject).playerId;

        public override int Kills
        {
            get => _kills;
            set
            {
                var changed = _kills != value;
                _kills = (short)value;

                if (changed) playerManager.Event.Invoke_OnPlayerStatsChanged(this);
            }
        }

        public override int Deaths
        {
            get => _deaths;
            set
            {
                var changed = _deaths != value;
                _deaths = (short)value;

                if (changed) playerManager.Event.Invoke_OnPlayerStatsChanged(this);
            }
        }

        public override int Score
        {
            get => _score;
            set
            {
                var changed = _score != value;
                _score = (short)value;

                if (changed) playerManager.Event.Invoke_OnPlayerStatsChanged(this);
            }
        }

        public override int KillStreak { get; set; }

        public override float Health => SyncedHealth;
        public override float MaxHealth => _maxHealth;
        public override int TeamId => _teamId;
        public override bool IsDead => _health <= 0;
        public override bool IsInSafeZone => _isInSafeZone;
        public override VRCPlayerApi VrcPlayer => Networking.GetOwner(gameObject);
        public override RoleData[] Roles => roleProvider.GetPlayerRoles(VrcPlayer);
        public bool IsCulled { get; set; }
        public bool IsCollidersActive { get => _isCollidersActive && !this.IsInStaffTeam(); private set => _isCollidersActive = value; }

        private void Start()
        {
            LastDamageInfo = DamageInfo.NewEmpty();
        }

        private void OnDestroy()
        {
            playerManager.RemovePlayerFromCache(this);
            playerManager.Event.Invoke_OnPlayerRemoved(this);
            _isAddedToManager = false;
        }

        public void Internal_UpdateView()
        {
            foreach (var col in playerColliders)
            {
                if (!col) continue;
                col.gameObject.SetActive(!IsCulled && IsCollidersActive);
                col.PostLateUpdate();
                col.IsDebugVisible = playerManager.IsDebug;
            }

            simpleCollider.gameObject.SetActive(IsCulled && IsCollidersActive);
            simpleCollider.PostLateUpdate();
            simpleCollider.IsDebugVisible = playerManager.IsDebug;

            foreach (var playerExtension in playerViews)
            {
                if (!playerExtension) continue;
                playerExtension.OnUpdateView();
            }

            _isUpdateViewScheduled = false;
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

        #region OverridenMethods
        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!player.IsOwner(gameObject) || _isAddedToManager) return;
            playerManager.AddPlayerToCache(this);
            playerManager.Event.Invoke_OnPlayerAdded(this);
            _isAddedToManager = true;
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

        public override void SetCollidersActive(bool isActive)
        {
            IsCollidersActive = isActive;
            UpdateView();
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
            if (_isUpdateViewScheduled) return;
            _isUpdateViewScheduled = true;
            SendCustomEventDelayedFrames(nameof(Internal_UpdateView), 1, EventTiming.LateUpdate);
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
            IsCollidersActive = true;

            playerManager.Event.Invoke_OnPlayerReset(this);
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
            playerManager.Event.Invoke_OnPlayerEnteredArea(this, area);
        }

        public override void OnAreaExit(PlayerAreaBase area)
        {
            _playerAreas.RemoveAll(area);
            UpdateSafeZoneStatus();
            UpdateView();
            playerManager.Event.Invoke_OnPlayerExitedArea(this, area);
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
        #endregion
    }
}
