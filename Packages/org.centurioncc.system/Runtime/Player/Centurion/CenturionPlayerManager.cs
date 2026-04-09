using System;
using CenturionCC.System.Utils;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Player.Centurion
{
    public enum PlayerBaseSimpleCalls
    {
        Kill,
        Revive,
        ResetToDefault,
        ResetStats
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CenturionPlayerManager : PlayerManagerBase
    {
        private const int MaxResolvedIdPoolSize = 8;
        private const string Prefix = "[<color=teal>PlayerManager</color>] ";

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

        [SerializeField]
        private float cullingDistance = 25;

        private readonly DataList _cachedCenturionPlayers = new DataList();
        private readonly DataList _resolvedEventIds = new DataList();
        private PlayerBase _cachedLocalPlayer;
        private int _lastUpdatedCenturionPlayerIndex;
        private int _lastUpdatedEventIdIndex;

        public override bool IsDebug
        {
            get => isDebug;
            set
            {
                if (isDebug == value) return;

                isDebug = value;
                Event.Invoke_OnDebugModeChanged(value);
            }
        }

        public override bool ShowTeamTag
        {
            get => showTeamTag;
            protected set
            {
                if (showTeamTag == value) return;

                showTeamTag = value;
                Event.Invoke_OnPlayerTagChanged(TagType.Team, value);
            }
        }

        public override bool ShowStaffTag
        {
            get => showStaffTag;
            protected set
            {
                if (showStaffTag == value) return;

                showStaffTag = value;
                Event.Invoke_OnPlayerTagChanged(TagType.Staff, value);
            }
        }

        public override bool ShowCreatorTag
        {
            get => showCreatorTag;
            protected set
            {
                if (showCreatorTag == value) return;

                showCreatorTag = value;
                Event.Invoke_OnPlayerTagChanged(TagType.Creator, value);
            }
        }

        public override FriendlyFireMode FriendlyFireMode
        {
            get => friendlyFireMode;
            protected set
            {
                if (friendlyFireMode == value) return;

                friendlyFireMode = value;
                Event.Invoke_OnFriendlyFireModeChanged(value);
            }
        }

        public float CullingDistance
        {
            get => cullingDistance;
            set => cullingDistance = value;
        }

        public override void PostLateUpdate()
        {
            if (_cachedCenturionPlayers.Count == 0) return;

            _lastUpdatedCenturionPlayerIndex = (_lastUpdatedCenturionPlayerIndex + 1) % _cachedCenturionPlayers.Count;

            var centurionPlayer = (CenturionPlayer)_cachedCenturionPlayers[_lastUpdatedCenturionPlayerIndex].Reference;
            if (centurionPlayer == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}PostLateUpdate: could not update player: destroyed CenturionPlayer is still present in the list at idx of {_lastUpdatedCenturionPlayerIndex}");
                return;
            }

            var vrcPlayer = centurionPlayer.VrcPlayer;
            if (vrcPlayer == null || !Utilities.IsValid(vrcPlayer))
            {
                CenturionDiagnostic.LogWarning($"{Prefix}PostLateUpdate: could not update player: vrcPlayer is not valid");
                return;
            }

            var distance = Vector3.Distance(vrcPlayer.GetPosition(), Networking.LocalPlayer.GetPosition());
            centurionPlayer.IsCulled = distance > CullingDistance || centurionPlayer.IsInSafeZone;
            centurionPlayer.UpdateView();
        }

        public override PlayerBase GetLocalPlayer()
        {
            if (_cachedLocalPlayer) return _cachedLocalPlayer;

            _cachedLocalPlayer = GetPlayer(Networking.LocalPlayer);

            return _cachedLocalPlayer;
        }

        public override PlayerBase[] GetPlayers()
        {
            var players = new PlayerBase[_cachedCenturionPlayers.Count];
            for (var i = 0; i < players.Length; i++)
            {
                players[i] = (PlayerBase)_cachedCenturionPlayers[i].Reference;
            }

            return players;
        }

        public override PlayerBase GetPlayer(VRCPlayerApi vrcPlayer)
        {
            if (!Utilities.IsValid(vrcPlayer)) return null;

            // you just can't. DataList doesn't support that.
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _cachedCenturionPlayers.Count; i++)
            {
                var centurionPlayer = (CenturionPlayer)_cachedCenturionPlayers[i].Reference;
                if (!centurionPlayer || centurionPlayer.VrcPlayer != vrcPlayer) continue;

                return centurionPlayer;
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
            if (PlayerBaseExtensions.IsStaffTeamId(teamId)) return staffTeamColor;
            if (teamId < 0 || teamId >= teamColors.Length) return teamColors[0];

            return teamColors[teamId];
        }

        public void AddPlayerToCache(PlayerBase player)
        {
            if (_cachedCenturionPlayers.Contains(player))
            {
                CenturionDiagnostic.LogWarning($"{Prefix}Invoke_OnPlayerAdded: player {player.PlayerId} has already been added");
                return;
            }

            _cachedCenturionPlayers.Add(player);
        }

        public void RemovePlayerFromCache(PlayerBase player)
        {
            _cachedCenturionPlayers.Remove(player);
        }

        public bool CanDamageFriendly()
        {
            switch (friendlyFireMode)
            {
                case FriendlyFireMode.Reverse:
                case FriendlyFireMode.Both:
                case FriendlyFireMode.Always:
                case FriendlyFireMode.Warning:
                    return true;
                default:
                    return false;
            }
        }

        public void AddResolvedEventId(Guid eventId)
        {
            if (_resolvedEventIds.Count < MaxResolvedIdPoolSize)
            {
                _resolvedEventIds.Add(eventId.ToString("D"));
                return;
            }

            _resolvedEventIds[_lastUpdatedEventIdIndex] = eventId.ToString("D");
            _lastUpdatedEventIdIndex = (_lastUpdatedEventIdIndex + 1) % MaxResolvedIdPoolSize;
        }

        public bool IsResolvedEventId(Guid eventId)
        {
            return _resolvedEventIds.Contains(eventId.ToString("D"));
        }

        #region BroadcastRequests
        public bool RequestDamageBroadcast(DamageInfo info)
        {
            var localVrcPlayer = Networking.LocalPlayer;
            // don't process reviews if the local player is not associated
            if (info.VictimId() != localVrcPlayer.playerId && info.AttackerId() != localVrcPlayer.playerId)
            {
                return false;
            }

            var victimCenturionPlayer = this.GetPlayerById(info.VictimId());
            var attackerCenturionPlayer = this.GetPlayerById(info.AttackerId());

            if (victimCenturionPlayer == null || attackerCenturionPlayer == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}Victim or Attacker CenturionPlayer is not found");
                return false;
            }

            // in special case
            // if the victim was in a staff team, ignore it
            if (victimCenturionPlayer.IsInStaffTeam())
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
            if (victimCenturionPlayer.IsFriendly(attackerCenturionPlayer) && !attackerCenturionPlayer.IsInStaffTeam())
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

            // ignore if the detection type disallows it
            switch (info.DetectionType())
            {
                case DetectionType.All:
                    break;
                case DetectionType.VictimSide:
                    if (localVrcPlayer.playerId != victimCenturionPlayer.PlayerId)
                        return false;
                    break;
                case DetectionType.AttackerSide:
                    if (localVrcPlayer.playerId != attackerCenturionPlayer.PlayerId)
                        return false;
                    break;
                case DetectionType.None:
                default:
                    return false;
            }

            // if the victim was already dead, ignore it
            if (victimCenturionPlayer.IsDead)
            {
                return false;
            }

            // if damage has already been resolved, ignore it
            if (IsResolvedEventId(info.EventId()))
            {
                return false;
            }

            // if callbacks have rejected the damage, ignore it
            if (Event.Invoke_OnDamagePreBroadcast(info))
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

        public void RequestHealthChangeBroadcast(int playerId, float health)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplyHealthChange), playerId, health);
        }

        public void RequestMaxHealthChangeBroadcast(int playerId, float maxHealth)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplyMaxHealthChange), playerId, maxHealth);
        }

        public void RequestResetToDefaultBroadcast(int playerId)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplySimpleCalls),
                playerId, PlayerBaseSimpleCalls.ResetToDefault);
        }

        public void RequestResetStatsBroadcast(int playerId)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplySimpleCalls),
                playerId, PlayerBaseSimpleCalls.ResetStats);
        }

        public void RequestKillBroadcast(int playerId)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplySimpleCalls),
                playerId, PlayerBaseSimpleCalls.Kill);
        }

        public void RequestReviveBroadcast(int playerId)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_ApplySimpleCalls),
                playerId, PlayerBaseSimpleCalls.Revive);
        }
        #endregion

        #region Internals
        public void Internal_ProcessDamageInfo(DamageInfo info)
        {
            var victimPlayer = this.GetPlayerById(info.VictimId());
            if (victimPlayer == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}Victim Player {info.VictimId()} is not found");
                return;
            }

            var attackerPlayer = this.GetPlayerById(info.AttackerId());
            if (attackerPlayer == null)
            {
                CenturionDiagnostic.LogWarning($"{Prefix}Attacker Player {info.AttackerId()} is not found");
                return;
            }

            if (victimPlayer.IsDead)
            {
                logger.LogWarn($"{Prefix}Victim Player {info.VictimId()} is already dead");
                return;
            }

            if (Event.Invoke_OnDamagePostBroadcast(info))
            {
                logger.LogWarn($"{Prefix}Callback has rejected to apply damage");
                return;
            }

            if (attackerPlayer.IsLocal || victimPlayer.IsLocal)
            {
                AddResolvedEventId(info.EventId());
            }

            if (attackerPlayer.IsFriendly(victimPlayer) && CanDamageFriendly() && !attackerPlayer.IsInStaffTeam())
            {
                switch (friendlyFireMode)
                {
                    case FriendlyFireMode.Never:
                        break;
                    case FriendlyFireMode.Always:
                        victimPlayer.ApplyDamage(info);
                        break;
                    case FriendlyFireMode.Both:
                        attackerPlayer.ApplyDamage(info);
                        victimPlayer.ApplyDamage(info);
                        break;
                    case FriendlyFireMode.Reverse:
                        attackerPlayer.ApplyDamage(info);
                        break;
                    case FriendlyFireMode.Warning:
                        if (attackerPlayer.IsLocal) Event.Invoke_OnPlayerFriendlyFireWarning(victimPlayer, info);
                        break;
                }

                return;
            }

            victimPlayer.ApplyDamage(info);
        }

        [NetworkCallable(100)]
        public void Internal_ApplyTeamChange(int playerId, int teamId)
        {
            if (Networking.LocalPlayer.playerId != playerId) return;
            var localPlayer = GetLocalPlayer();

            if (CenturionDiagnostic.Assert(localPlayer != null, "PlayerManager:Internal_ApplyTeamChange: localPlayer != null")) return;

            localPlayer.SetTeam(teamId);
        }

        [NetworkCallable(100)]
        public void Internal_ApplyHealthChange(int playerId, float health)
        {
            if (Networking.LocalPlayer.playerId != playerId) return;
            var localPlayer = GetLocalPlayer();

            if (CenturionDiagnostic.Assert(localPlayer != null, "PlayerManager:Internal_ApplyHealthChange: localPlayer != null")) return;
            localPlayer.SetHealth(health);
        }

        [NetworkCallable(100)]
        public void Internal_ApplyMaxHealthChange(int playerId, float maxHealth)
        {
            if (Networking.LocalPlayer.playerId != playerId) return;
            var localPlayer = GetLocalPlayer();

            if (CenturionDiagnostic.Assert(localPlayer != null, "PlayerManager:Internal_ApplyMaxHealthChange: localPlayer != null")) return;
            localPlayer.SetMaxHealth(maxHealth);
        }

        [NetworkCallable(100)]
        public void Internal_BroadcastDamageInfo(byte[] info)
        {
            var damageInfo = DamageInfo.FromBytes(info);
            Internal_ProcessDamageInfo(damageInfo);
        }

        [NetworkCallable(100)]
        public void Internal_ApplySimpleCalls(int playerId, PlayerBaseSimpleCalls simpleCallType)
        {
            if (Networking.LocalPlayer.playerId != playerId) return;
            var localPlayer = GetLocalPlayer();

            if (CenturionDiagnostic.Assert(localPlayer != null, "PlayerManager:Internal_ApplySimpleCalls: localPlayer != null")) return;
            switch (simpleCallType)
            {
                case PlayerBaseSimpleCalls.Kill:
                    localPlayer.Kill();
                    break;
                case PlayerBaseSimpleCalls.Revive:
                    localPlayer.Revive();
                    break;
                case PlayerBaseSimpleCalls.ResetToDefault:
                    localPlayer.ResetToDefault();
                    break;
                case PlayerBaseSimpleCalls.ResetStats:
                    localPlayer.ResetStats();
                    break;
                default:
                    CenturionDiagnostic.LogError($"PlayerManager:Internal_ApplySimpleCalls: Unknown call type {simpleCallType}");
                    return;
            }
        }
        #endregion
    }
}
