using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionPlayerManager : PlayerManager
    {
        private const string LogPrefix = "[CPlayerManager] ";

        [SerializeField] [NewbieInject]
        private CenturionDamageResolver damageResolver;

        [SerializeField]
        private Color[] teamColors;

        [SerializeField]
        private Color staffTeamColor = new Color(0.172549F, 0.4733055F, 0.8117647F, 1F);

        [UdonSynced] [FieldChangeCallback(nameof(FriendlyFireMode))]
        private FriendlyFireMode _friendlyFireMode;

        private bool _isDebug;

        [UdonSynced] [FieldChangeCallback(nameof(ShowCreatorTag))]
        private bool _showCreatorTag;

        [UdonSynced] [FieldChangeCallback(nameof(ShowStaffTag))]
        private bool _showStaffTag;

        [UdonSynced] [FieldChangeCallback(nameof(ShowTeamTag))]
        private bool _showTeamTag;

        public override bool IsDebug
        {
            get => _isDebug;
            set
            {
                if (_isDebug == value) return;
                _isDebug = value;
                var players = GetPlayers();
                foreach (var player in players)
                {
                    player.UpdateView();
                }

                Invoke_OnDebugModeChanged(value);
            }
        }

        public override bool ShowTeamTag
        {
            get => _showTeamTag;
            protected set
            {
                if (_showTeamTag == value) return;
                _showTeamTag = value;
                Invoke_OnPlayerTagChanged(TagType.Team, value);
            }
        }

        public override bool ShowStaffTag
        {
            get => _showStaffTag;
            protected set
            {
                if (_showStaffTag == value) return;
                _showStaffTag = value;
                Invoke_OnPlayerTagChanged(TagType.Staff, value);
            }
        }

        public override bool ShowCreatorTag
        {
            get => _showCreatorTag;
            protected set
            {
                if (_showCreatorTag == value) return;
                _showCreatorTag = value;
                Invoke_OnPlayerTagChanged(TagType.Creator, value);
            }
        }

        public override FriendlyFireMode FriendlyFireMode
        {
            get => _friendlyFireMode;
            protected set
            {
                if (_friendlyFireMode == value) return;

                _friendlyFireMode = value;
                Invoke_OnFriendlyFireModeChanged(value);
            }
        }

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

        public override void SetPlayerTag(TagType type, bool isOn)
        {
            switch (type)
            {
                case TagType.Team:
                    ShowTeamTag = isOn;
                    break;
                case TagType.Dev:
                case TagType.Master:
                case TagType.Staff:
                    ShowStaffTag = isOn;
                    break;
                case TagType.Creator:
                    ShowCreatorTag = isOn;
                    break;
            }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override void SetFriendlyFireMode(FriendlyFireMode mode)
        {
            FriendlyFireMode = mode;

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public override Color GetTeamColor(int teamId)
        {
            if (IsStaffTeamId(teamId)) return staffTeamColor;
            if (teamId < 0 || teamId >= teamColors.Length) return teamColors[0];

            return teamColors[teamId];
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

            // in special case
            // if the victim was in a staff team, ignore it
            if (IsInStaffTeam(victimCenturionPlayer))
            {
                return false;
            }

            // if the victim was already dead, ignore it
            if (victimCenturionPlayer.IsDead)
            {
                return false;
            }

            // on self-fire
            if (victimCenturionPlayer.PlayerId == attackerCenturionPlayer.PlayerId)
            {
                // if damage cannot be applied to self, ignore it 
                if (!info.CanDamageSelf())
                {
                    return false;
                }

                damageResolver.BroadcastDamageInfo(info);
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

                damageResolver.BroadcastDamageInfo(info);
                return true;
            }

            // on enemy fire
            // if damage cannot be applied to the enemy, ignore it
            if (!info.CanDamageEnemy())
            {
                return false;
            }

            // if damage has already been resolved, ignore it
            if (damageResolver.IsEventResolved(info.EventId()))
            {
                return false;
            }

            damageResolver.BroadcastDamageInfo(info);
            return true;
        }

        public void RequestTeamChangeBroadcast(int playerId, int teamId)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplyTeamChange), playerId, teamId);
        }

        /// <summary>
        /// Applies damage to player if the victim was local
        /// </summary>
        /// <param name="info">broadcasted damage info</param>
        public void Internal_ApplyLocalDamageInfo(DamageInfo info)
        {
            var victimPlayer = GetPlayerById(info.VictimId());
            if (!victimPlayer)
            {
                logger.LogWarn($"{LogPrefix}Victim Player {info.VictimId()} is not found");
                return;
            }

            victimPlayer.LastHitData.SetData(info);

            if (victimPlayer.IsLocal)
            {
                victimPlayer.Kill();
                return;
            }
        }

        [NetworkCallable]
        public void Internal_ApplyTeamChange(int playerId, int teamId)
        {
            if (Networking.LocalPlayer.playerId != playerId) return;
            var localPlayer = GetLocalPlayer();

            if (!localPlayer) return;
            localPlayer.SetTeam(teamId);
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