using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionPlayer : PlayerBase
    {
        private const string LogPrefix = "[CPlayer] ";

        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        [SerializeField] [NewbieInject]
        private RoleProvider roleProvider;

        [SerializeField] [NewbieInject]
        private CenturionPlayerManager playerManager;

        private readonly DataList _playerAreas = new DataList();

        [UdonSynced]
        private short _deaths;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedHealth))]
        private float _health = 100;

        private bool _isInSafeZone;

        [UdonSynced]
        private short _kills;

        [UdonSynced]
        private float _maxHealth = 100;

        [UdonSynced]
        private short _score;

        [UdonSynced] [FieldChangeCallback(nameof(SyncedTeamId))]
        private byte _teamId;

        public float SyncedHealth
        {
            get => _health;
            protected set
            {
                var lastIsDead = IsDead;
                var lastHealth = _health;
                _health = value;

                playerManager.Invoke_OnPlayerHealthChanged(this, lastHealth);
                if (lastIsDead == IsDead) return;

                if (IsDead)
                {
                    var attacker = playerManager.GetPlayerById(LastDamageInfo.AttackerId());
                    var type = LastDamageInfo.AttackerId() == PlayerId
                        ? KillType.ReverseFriendlyFire
                        : playerManager.IsFriendly(this, attacker)
                            ? KillType.FriendlyFire
                            : KillType.Default;

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

        public int Score => _score;

        public override string DisplayName =>
            $"<color=#{playerManager.GetTeamColor(TeamId).ToHtmlStringRGBA()}>{VrcPlayer.SafeGetDisplayName()}</color>";

        public override int PlayerId => Networking.GetOwner(gameObject).playerId;

        public override int Kills
        {
            get => _kills;
            set => _kills = (short)value;
        }

        public override int Deaths
        {
            get => _deaths;
            set => _deaths = (short)value;
        }

        public override float Health => SyncedHealth;
        public override float MaxHealth => _maxHealth;
        public override int TeamId => _teamId;
        public override bool IsDead => _health <= 0;
        public override bool IsInSafeZone => _isInSafeZone;
        public override VRCPlayerApi VrcPlayer => Networking.GetOwner(gameObject);
        public override RoleData[] Roles => roleProvider.GetPlayerRoles(VrcPlayer);

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

            _maxHealth = maxHealth;
            RequestSerialization();
        }

        public override void OnLocalHit(PlayerColliderBase playerCollider, DamageData data, Vector3 contactPoint)
        {
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
            logger.LogVerbose($"{LogPrefix}UpdateView");

            var colliders = GetComponentsInChildren<CenturionPlayerCollider>(true);
            foreach (var col in colliders)
            {
                col.gameObject.SetActive(!playerManager.IsInStaffTeam(this));
                col.IsDebugVisible = playerManager.IsDebug;
            }

            var playerTags = GetComponentsInChildren<CenturionPlayerTag>(true);
            foreach (var playerTag in playerTags)
            {
                playerTag.Refresh();
            }
        }

        [NetworkCallable]
        public override void ResetToDefault()
        {
            if (!IsLocal)
            {
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(ResetToDefault));
                return;
            }

            _deaths = 0;
            _kills = 0;
            _score = 0;
            SyncedTeamId = 0;
            _maxHealth = 100;
            SyncedHealth = _maxHealth;

            playerManager.Invoke_OnPlayerReset(this);
            RequestSerialization();
        }

        public override void Kill()
        {
            if (!IsLocal) return;

            SyncedHealth = 0;
            RequestSerialization();
        }

        public override void Revive()
        {
            if (!IsLocal) return;

            SyncedHealth = _maxHealth;
            RequestSerialization();
        }

        public override void OnAreaEnter(PlayerAreaBase area)
        {
            _playerAreas.Add(area);
            UpdateSafeZoneStatus();
            playerManager.Invoke_OnPlayerEnteredArea(this, area);
        }

        public override void OnAreaExit(PlayerAreaBase area)
        {
            _playerAreas.RemoveAll(area);
            UpdateSafeZoneStatus();
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