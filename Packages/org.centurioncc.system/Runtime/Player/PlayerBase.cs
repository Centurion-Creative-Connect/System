using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Common.Role;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class PlayerBase : UdonSharpBehaviour
    {
        [PublicAPI]
        public abstract int PlayerId { get; }

        [PublicAPI]
        public virtual int Index { get; protected set; } = -1;

        [PublicAPI]
        public abstract int TeamId { get; }

        [PublicAPI]
        public virtual int Kills { get; set; }

        [PublicAPI]
        public virtual int Deaths { get; set; }

        [PublicAPI]
        public virtual long PreviousDiedTimeTicks { get; set; }

        [PublicAPI]
        public virtual DateTime PreviousDiedTime
        {
            get =>
                DateTime.MinValue.Ticks < PreviousDiedTimeTicks &&
                DateTime.MaxValue.Ticks > PreviousDiedTimeTicks
                    ? new DateTime(PreviousDiedTimeTicks)
                    : DateTime.MinValue;
            set => PreviousDiedTimeTicks = value.Ticks;
        }

        [PublicAPI]
        public virtual bool IsAssigned => PlayerId != -1;

        [PublicAPI]
        public virtual bool IsLocal => PlayerId == Networking.LocalPlayer.playerId;

        [PublicAPI] [CanBeNull]
        public abstract VRCPlayerApi VrcPlayer { get; }

        [PublicAPI] [CanBeNull]
        public abstract RoleData Role { get; }

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
        public abstract void OnDeath();
    }
}