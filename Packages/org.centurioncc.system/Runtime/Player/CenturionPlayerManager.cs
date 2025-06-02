using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Data;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionPlayerManager : PlayerManager
    {
        private const string LogPrefix = "[CPlayerManager] ";

        [SerializeField]
        private Color[] teamColors;

        [SerializeField]
        private Color staffTeamColor = new Color(0.172549F, 0.4733055F, 0.8117647F, 1F);

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(FriendlyFireMode))]
        private FriendlyFireMode friendlyFireMode = FriendlyFireMode.Never;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(ShowCreatorTag))]
        private bool showCreatorTag;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(ShowStaffTag))]
        private bool showStaffTag = true;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(ShowTeamTag))]
        private bool showTeamTag = true;

        [SerializeField]
        private bool isDebug;

        private readonly DataList _resolvedEventIds = new DataList();

        public override bool IsDebug
        {
            get => isDebug;
            set
            {
                if (isDebug == value) return;
                isDebug = value;
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
            get => showTeamTag;
            protected set
            {
                if (showTeamTag == value) return;
                showTeamTag = value;
                Invoke_OnPlayerTagChanged(TagType.Team, value);
            }
        }

        public override bool ShowStaffTag
        {
            get => showStaffTag;
            protected set
            {
                if (showStaffTag == value) return;
                showStaffTag = value;
                Invoke_OnPlayerTagChanged(TagType.Staff, value);
            }
        }

        public override bool ShowCreatorTag
        {
            get => showCreatorTag;
            protected set
            {
                if (showCreatorTag == value) return;
                showCreatorTag = value;
                Invoke_OnPlayerTagChanged(TagType.Creator, value);
            }
        }

        public override FriendlyFireMode FriendlyFireMode
        {
            get => friendlyFireMode;
            protected set
            {
                if (friendlyFireMode == value) return;

                friendlyFireMode = value;
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

                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_BroadcastDamageInfo), info.ToBytes());
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

                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_BroadcastDamageInfo), info.ToBytes());
                return true;
            }

            // on enemy fire
            // if damage cannot be applied to the enemy, ignore it
            if (!info.CanDamageEnemy())
            {
                return false;
            }

            // if damage has already been resolved, ignore it
            if (IsEventResolved(info.EventId()))
            {
                return false;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_BroadcastDamageInfo), info.ToBytes());
            return true;
        }

        public void RequestTeamChangeBroadcast(int playerId, int teamId)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplyTeamChange), playerId, teamId);
        }

        public void Internal_ProcessDamageInfo(DamageInfo info)
        {
            var victimPlayer = GetPlayerById(info.VictimId());
            if (!victimPlayer)
            {
                logger.LogWarn($"{LogPrefix}Victim Player {info.VictimId()} is not found");
                return;
            }

            var attackerPlayer = GetPlayerById(info.AttackerId());
            if (!attackerPlayer)
            {
                logger.LogWarn($"{LogPrefix}Attacker Player {info.AttackerId()} is not found");
                return;
            }

            _resolvedEventIds.Add(info.EventId().ToString("D"));

            if (IsFriendly(attackerPlayer, victimPlayer) && CanDamageFriendly())
            {
                switch (friendlyFireMode)
                {
                    case FriendlyFireMode.Never:
                        break;
                    case FriendlyFireMode.Always:
                        victimPlayer.LastHitData.SetData(info, KillType.FriendlyFire);
                        victimPlayer.Kill();
                        break;
                    case FriendlyFireMode.Both:
                        attackerPlayer.LastHitData.SetData(info, KillType.ReverseFriendlyFire);
                        attackerPlayer.Kill();
                        victimPlayer.LastHitData.SetData(info, KillType.FriendlyFire);
                        victimPlayer.Kill();
                        break;
                    case FriendlyFireMode.Reverse:
                        attackerPlayer.LastHitData.SetData(info, KillType.ReverseFriendlyFire);
                        attackerPlayer.Kill();
                        break;
                    case FriendlyFireMode.Warning:
                        Invoke_OnFriendlyFireWarning(victimPlayer, null, Vector3.zero);
                        break;
                }

                return;
            }

            victimPlayer.LastHitData.SetData(info, KillType.Default);
            victimPlayer.Kill();
        }

        [NetworkCallable]
        public void Internal_ApplyTeamChange(int playerId, int teamId)
        {
            if (Networking.LocalPlayer.playerId != playerId) return;
            var localPlayer = GetLocalPlayer();

            if (!localPlayer) return;
            localPlayer.SetTeam(teamId);
        }

        [NetworkCallable(100)]
        public void Internal_BroadcastDamageInfo(byte[] info)
        {
            var damageInfo = DamageInfo.FromBytes(info);
            Internal_ProcessDamageInfo(damageInfo);
        }

        public bool CanDamageFriendly()
        {
            switch (friendlyFireMode)
            {
                case FriendlyFireMode.Reverse:
                case FriendlyFireMode.Both:
                case FriendlyFireMode.Always:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsEventResolved(Guid eventId)
        {
            return _resolvedEventIds.Contains(eventId.ToString("D"));
        }
    }
}