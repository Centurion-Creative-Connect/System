using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Player
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

        [UdonSynced] [FieldChangeCallback(nameof(CurrentHealth))]
        private float _currentHealth = 100;

        [UdonSynced]
        private short _deaths;

        [UdonSynced]
        private short _kills;

        [UdonSynced]
        private float _maxHealth = 100;

        [UdonSynced]
        private short _score;

        [UdonSynced]
        private byte _teamId;

        public float CurrentHealth
        {
            get => _currentHealth;
            protected set
            {
                var lastIsDead = IsDead;
                _currentHealth = value;
                if (lastIsDead == IsDead) return;

                if (IsDead)
                {
                    playerManager.Invoke_OnKilled(
                        playerManager.GetPlayerById(LastHitData.AttackerId),
                        this,
                        KillType.Default
                    );
                }
                else
                {
                    playerManager.Invoke_OnPlayerRevived(this);
                }
            }
        }

        public int Score => _score;

        public override int PlayerId => Networking.GetOwner(gameObject).playerId;
        public override int Kills => _kills;
        public override int Deaths => _deaths;
        public override int TeamId => _teamId;
        public override bool IsDead => _currentHealth <= 0;
        public override VRCPlayerApi VrcPlayer => Networking.GetOwner(gameObject);
        public override RoleData Role => roleProvider.GetPlayerRole(VrcPlayer);

        public override void SetPlayer(int vrcPlayerId)
        {
            logger.LogError($"{LogPrefix}Cannot set player id for CenturionPlayer");
        }

        [NetworkCallable]
        public override void SetTeam(int teamId)
        {
            if (!IsLocal)
            {
                logger.Log($"{LogPrefix}Calling SetTeam({teamId}) on owner...");
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(SetTeam), teamId);
                return;
            }

            _teamId = (byte)teamId;
            RequestSerialization();
        }

        public override void UpdateView()
        {
        }

        public override void Sync()
        {
            RequestSerialization();
        }

        public override void ResetPlayer()
        {
            ResetStats();
        }

        [NetworkCallable]
        public override void ResetStats()
        {
            if (!IsLocal)
            {
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(ResetStats));
                return;
            }

            _deaths = 0;
            _kills = 0;
            _score = 0;
            _teamId = 0;
            _maxHealth = 100;
            _currentHealth = _maxHealth;

            RequestSerialization();
        }

        public override void OnDamage(PlayerCollider playerCollider, DamageData data, Vector3 contactPoint)
        {
            playerManager.RequestDamageBroadcast(DamageInfo.New(VrcPlayer, contactPoint, playerCollider.parts, data));
        }

        public override void Kill()
        {
            CurrentHealth = 0;
            RequestSerialization();
        }

        public override void Revive()
        {
            CurrentHealth = _maxHealth;
            RequestSerialization();
        }

        public void LocalOnDamage(DamageInfo info)
        {
            playerManager.RequestDamageBroadcast(info);
        }
    }
}