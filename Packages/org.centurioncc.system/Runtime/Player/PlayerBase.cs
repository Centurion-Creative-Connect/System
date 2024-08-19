using CenturionCC.System.Player.MassPlayer;
using CenturionCC.System.Utils;
using DerpyNewbie.Common.Role;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [RequireComponent(typeof(LastHitData))]
    public abstract class PlayerBase : UdonSharpBehaviour
    {
        [SerializeField] private LastHitData lastHitData;
        protected int _deaths = 0;

        protected int _kills = 0;

        [PublicAPI] public abstract int PlayerId { get; }

        [PublicAPI] public virtual int Index { get; protected set; } = -1;

        [PublicAPI] public abstract int TeamId { get; }

        [PublicAPI]
        public virtual int Kills
        {
            get => _kills;
            set
            {
                var diff = value - _kills;
                _kills = value;

                KillStreak += diff;
                if (KillStreak < 0)
                    KillStreak = 0;
                if (KillStreak > HighestKillStreak)
                    HighestKillStreak = KillStreak;

                Score += 100 * KillStreak;
                if (LastHitData.Distance > 50)
                    Score += (int)LastHitData.Distance * 2;
            }
        }

        [PublicAPI]
        public virtual int Deaths
        {
            get => _deaths;
            set
            {
                _deaths = value;
                KillStreak = 0;
            }
        }

        [PublicAPI] public virtual int Score { get; set; }

        [PublicAPI] public virtual int KillStreak { get; set; }

        [PublicAPI] public virtual int HighestKillStreak { get; set; }

        [PublicAPI] public virtual LastHitData LastHitData => lastHitData;

        [PublicAPI] public virtual bool IsAssigned => PlayerId != -1;

        [PublicAPI] public virtual bool IsLocal => PlayerId == Networking.LocalPlayer.playerId;

        [PublicAPI] public abstract bool IsDead { get; }

        [PublicAPI] [CanBeNull] public abstract VRCPlayerApi VrcPlayer { get; }

        [PublicAPI] [CanBeNull] public abstract RoleData Role { get; }

        [PublicAPI]
        public virtual void SetId(int id)
        {
            if (Index != -1)
                return;

            Index = id;
        }

        [PublicAPI]
        public abstract void SetPlayer(int vrcPlayerId);

        [PublicAPI]
        public abstract void SetTeam(int teamId);

        [PublicAPI]
        public abstract void UpdateView();

        [PublicAPI]
        public abstract void Sync();

        [PublicAPI]
        public abstract void ResetPlayer();

        [PublicAPI]
        public abstract void ResetStats();

        [PublicAPI]
        public abstract void OnDamage(PlayerCollider playerCollider, DamageData data, Vector3 contactPoint);

        [PublicAPI]
        public abstract void Kill();

        [PublicAPI]
        public abstract void Revive();

        public virtual void OnHitDataUpdated()
        {
        }
    }
}