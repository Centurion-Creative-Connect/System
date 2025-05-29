using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using NotImplementedException = System.NotImplementedException;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionPlayerManager : PlayerManager
    {
        private const string LogPrefix = "[CPlayerManager] ";

        [SerializeField] [NewbieInject]
        private CenturionDamageResolver damageResolver;

        [UdonSynced]
        private FriendlyFireMode _friendlyFireMode;

        public override bool IsDebug { get; set; }
        public override bool ShowTeamTag { get; protected set; }
        public override bool ShowStaffTag { get; protected set; }
        public override bool ShowCreatorTag { get; protected set; }
        public override FriendlyFireMode FriendlyFireMode { get; protected set; }

        [PublicAPI] [CanBeNull]
        public override PlayerBase GetLocalPlayer()
        {
            return GetPlayer(Networking.LocalPlayer);
        }

        [PublicAPI] [CanBeNull]
        public override PlayerBase GetPlayerById(int vrcPlayerId)
        {
            return GetPlayer(VRCPlayerApi.GetPlayerById(vrcPlayerId));
        }

        public override PlayerBase[] GetPlayers()
        {
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            var playerBases = new PlayerBase[players.Length];
            VRCPlayerApi.GetPlayers(players);
            for (int i = 0; i < players.Length; i++)
            {
                playerBases[i] = GetPlayer(players[i]);
            }

            return playerBases;
        }

        public override Color GetTeamColor(int teamId)
        {
            return Color.cyan;
        }

        [PublicAPI] [CanBeNull]
        public override PlayerBase GetPlayer(VRCPlayerApi vrcPlayer)
        {
            if (!Utilities.IsValid(vrcPlayer)) return null;

            var objects = Networking.GetPlayerObjects(vrcPlayer);
            foreach (var obj in objects)
            {
                if (!Utilities.IsValid(obj)) continue;
                var player = obj.GetComponentInChildren<CenturionPlayer>();
                if (!Utilities.IsValid(player)) continue;
                return player;
            }

            return null;
        }

        /// <summary>
        /// Requests damage review to be broadcasted
        /// </summary>
        /// <param name="info">detected damage info</param>
        /// <returns>true if broadcasted, false otherwise</returns>
        public bool RequestDamageBroadcast(DamageInfo info)
        {
            var localVrcPlayer = Networking.LocalPlayer;
            // don't process reviews if the local player is not associated
            if (info.VictimId() != localVrcPlayer.playerId && info.AttackerId() != localVrcPlayer.playerId)
            {
                return false;
            }

            var victimCenturionPlayer = GetPlayerById(info.VictimId());
            var attackerCenturionPlayer = GetPlayerById(info.AttackerId());

            if (!victimCenturionPlayer || !attackerCenturionPlayer)
            {
                logger.LogWarn($"{LogPrefix}Victim or Attacker CenturionPlayer is not found");
                return false;
            }

            // on special case
            // if victim was in staff team, ignore it
            if (IsInStaffTeam(victimCenturionPlayer))
            {
                return false;
            }

            // if victim was already dead, ignore it
            if (victimCenturionPlayer.IsDead)
            {
                return false;
            }

            // on self fire
            if (victimCenturionPlayer.PlayerId == attackerCenturionPlayer.PlayerId)
            {
                // if damage cannot be applied to self, ignore it 
                if (!info.CanDamageSelf())
                {
                    return false;
                }

                damageResolver.BroadcastEvent(info);
                return true;
            }

            // on friendly fire
            if (IsFriendly(victimCenturionPlayer, attackerCenturionPlayer))
            {
                // if damage cannot be applied to friendly, ignore it
                if (!info.CanDamageFriendly())
                {
                    return false;
                }

                // if friendly fire is not allowed, ignore it
                if (info.RespectFriendlyFireSetting() && !CanDamageFriendly())
                {
                    return false;
                }

                // if it was victim-local detection, ignore it 
                if (victimCenturionPlayer.PlayerId == localVrcPlayer.playerId)
                {
                    return false;
                }

                damageResolver.BroadcastEvent(info);
                return true;
            }

            // on enemy fire
            // if damage cannot be applied to enemy, ignore it
            if (!info.CanDamageEnemy())
            {
                return false;
            }

            // if damage has already been resolved, ignore it
            if (damageResolver.IsEventResolved(info.EventId()))
            {
                return false;
            }

            damageResolver.BroadcastEvent(info);
            return true;
        }

        /// <summary>
        /// Applies damage to player if victim was local
        /// </summary>
        /// <param name="info">broadcasted damage info</param>
        public void Internal_ApplyLocalDamageInfo(DamageInfo info)
        {
            // if user is not victim, ignore it
            if (info.VictimId() != Networking.LocalPlayer.playerId)
            {
                return;
            }

            var localPlayer = GetLocalPlayer();
            if (!localPlayer) return;

            localPlayer.Kill();
        }

        public bool CanDamageFriendly()
        {
            switch (_friendlyFireMode)
            {
                case FriendlyFireMode.Reverse:
                case FriendlyFireMode.Both:
                case FriendlyFireMode.Always:
                    return true;
                default:
                    return false;
            }
        }
    }
}