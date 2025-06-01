using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Player
{
    [DefaultExecutionOrder(20)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [Obsolete]
    public class OldPlayerManager : PlayerManager
    {
        private const string Prefix = "[<color=orange>PlayerManager</color>] ";

        private const string MustBeMasterError =
            Prefix + "<color=red>You must be an master to execute this method</color>: {0}";

        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        [Header("Base Settings")] [SerializeField]
        private bool autoAddPlayerAtJoin = true;

        [SerializeField] private PlayerBase[] playerInstancePool;

        [SerializeField] private FootstepAudioStore footstepAudio;

        [Header("Team Settings")] [SerializeField]
        private FriendlyFireMode friendlyFireMode = FriendlyFireMode.Never;

        [SerializeField] private Color[] teamColors;

        [SerializeField] private int staffTeamId = 255;

        [SerializeField] private Color staffTeamColor = new Color(0.172549F, 0.4733055F, 0.8117647F, 1F);

        [Header("Tag Settings")] [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(ShowTeamTag))]
        private bool showTeamTag = true;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(ShowStaffTag))]
        private bool showStaffTag = true;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(ShowCreatorTag))]
        private bool showCreatorTag;

        [Header("Debug Settings")] [SerializeField]
        private bool isDebug;

        [Header("Deprecated Settings")] [SerializeField]
        private bool useBaseCollider = true;

        [SerializeField] private bool useAdditionalCollider = true;

        [SerializeField] private bool useLightweightCollider = true;

        [SerializeField] private bool alwaysUseLightweightCollider;

        private WatchdogChildCallbackBase[] _callbacks;

        [UdonSynced] [FieldChangeCallback(nameof(FriendlyFireModeSynced))]
        private int _friendlyFireModeSynced; // Synced value is used thru FieldChangeCallback

        private bool _isTeamPlayerCountsDirty;
        private int _localPlayerIndex = -1;

        public UpdateManager UpdateManager => updateManager;

        [Obsolete("Use FriendlyFireMode instead.")]
        public bool AllowFriendlyFire
        {
            get => FriendlyFireMode == FriendlyFireMode.Always || FriendlyFireMode == FriendlyFireMode.Both;
            private set => FriendlyFireMode = value ? FriendlyFireMode.Always : FriendlyFireMode.Never;
        }

        public override FriendlyFireMode FriendlyFireMode
        {
            get => (FriendlyFireMode)FriendlyFireModeSynced;
            protected set => FriendlyFireModeSynced = (int)value;
        }

        private int FriendlyFireModeSynced
        {
            get => _friendlyFireModeSynced;
            set
            {
                var previousMode = friendlyFireMode;

                friendlyFireMode = (FriendlyFireMode)value;
                _friendlyFireModeSynced = value;

                if (previousMode != friendlyFireMode) Invoke_OnFriendlyFireModeChanged(previousMode);
            }
        }

        public override bool UseBaseCollider
        {
            get => useBaseCollider;
            set
            {
                useBaseCollider = value;
                UpdateAllPlayerView();
            }
        }

        public override bool UseAdditionalCollider
        {
            get => useAdditionalCollider;
            set
            {
                useAdditionalCollider = value;
                UpdateAllPlayerView();
            }
        }

        public override bool UseLightweightCollider
        {
            get => useLightweightCollider;
            set
            {
                useLightweightCollider = value;
                UpdateAllPlayerView();
            }
        }

        public override bool AlwaysUseLightweightCollider
        {
            get => alwaysUseLightweightCollider;
            set
            {
                alwaysUseLightweightCollider = value;
                UpdateAllPlayerView();
            }
        }

        public override bool IsDebug
        {
            get => isDebug;
            set
            {
                isDebug = value;
                UpdateAllPlayerView();
            }
        }

        public override bool ShowTeamTag
        {
            get => showTeamTag;
            protected set
            {
                var shouldNotify = showTeamTag != value;
                showTeamTag = value;
                if (shouldNotify)
                    Invoke_OnPlayerTagChanged(TagType.Team, value);
            }
        }

        public override bool ShowStaffTag
        {
            get => showStaffTag;
            protected set
            {
                var shouldNotify = showStaffTag != value;
                showStaffTag = value;
                if (shouldNotify)
                    Invoke_OnPlayerTagChanged(TagType.Staff, value);
            }
        }

        public override bool ShowCreatorTag
        {
            get => showCreatorTag;
            protected set
            {
                var shouldNotify = showCreatorTag != value;
                showCreatorTag = value;
                if (shouldNotify)
                    Invoke_OnPlayerTagChanged(TagType.Creator, value);
            }
        }

        private void Start()
        {
            if (Networking.IsMaster)
            {
                FriendlyFireMode = friendlyFireMode;
            }

            if (playerInstancePool == null || playerInstancePool.Length <= 0)
            {
                logger.Log($"{Prefix}Getting ShooterPlayer instances");
                var players = new PlayerBase[transform.childCount];
                for (var i = 0; i < players.Length; i++)
                    players[i] = transform.GetChild(i).GetComponent<PlayerBase>();

                playerInstancePool = players;
            }

            logger.Log($"{Prefix}Updating ShooterPlayer index");
            for (var i = 0; i < playerInstancePool.Length; i++)
            {
                var player = GetPlayer(i);
                if (player != null) player.SetId(i);
            }

            logger.Log($"{Prefix}Generate Watchdog Callback");
            var callbacks = new WatchdogChildCallbackBase[playerInstancePool.Length];
            for (var i = 0; i < playerInstancePool.Length; i++)
                callbacks[i] = (WatchdogChildCallbackBase)(UdonSharpBehaviour)playerInstancePool[i];
            _callbacks = callbacks;

            RequestSerialization();

            logger.LogVerbose($"{Prefix}Start complete");
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsMaster && autoAddPlayerAtJoin) MasterOnly_AddPlayer(player.playerId);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (Networking.IsMaster) MasterOnly_RemovePlayer(player.playerId);
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            // Ownership transfer is disallowed. Only instance master can be owner of this object!
            return false;
        }

        public int KeepAlive(WatchdogProc wd, int nonce)
        {
            return nonce;
        }

        public WatchdogChildCallbackBase[] GetChildren()
        {
            return _callbacks;
        }

        public override string ToString()
        {
            if (Networking.GetOwner(gameObject).IsValid())
                return
                    $"{Prefix}\n" +
                    "PLAYERINFO:\n" +
                    $"   IsOwner : {Networking.IsOwner(gameObject)}\n" +
                    $"   IsMaster: {Networking.LocalPlayer.isMaster}\n" +
                    $"   PlayerId: {Networking.LocalPlayer.playerId}\n" +
                    "GETTERS:\n" +
                    $"  LocalPlayerIndex: {GetLocalPlayerIndex()}\n" +
                    $"  CanJoin       : {CanJoin()}\n" +
                    $"  CanLeave      : {CanLeave()}\n" +
                    $"  HasLocalPlayer: {HasLocalPlayer()}";

            return $"{Prefix}\n" +
                   "Not Ready";
        }

        #region Counters

        public override int PlayerCount => _playerCount;

        public override int ModeratorPlayerCount => _moderatorPlayerCount;

        public override int NoneTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _noneTeamPlayerCount;
            }
        }

        public override int NoneTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _noneTeamModPlayerCount;
            }
        }

        public override int RedTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _redTeamPlayerCount;
            }
        }

        public override int RedTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _redTeamModPlayerCount;
            }
        }

        public override int YellowTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _yellowTeamPlayerCount;
            }
        }

        public override int YellowTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _yellowTeamModPlayerCount;
            }
        }

        public override int GreenTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _greenTeamPlayerCount;
            }
        }

        public override int GreenTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _greenTeamModPlayerCount;
            }
        }

        public override int BlueTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _blueTeamPlayerCount;
            }
        }

        public override int BlueTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _blueTeamModPlayerCount;
            }
        }

        public override int UndefinedTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _undefinedTeamPlayerCount;
            }
        }

        public override int UndefinedTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _undefinedTeamModPlayerCount;
            }
        }

        #endregion

        #region PlayerCounts

        private int _playerCount;
        private int _moderatorPlayerCount;
        private int _noneTeamModPlayerCount;
        private int _noneTeamPlayerCount;
        private int _redTeamModPlayerCount;
        private int _redTeamPlayerCount;
        private int _yellowTeamModPlayerCount;
        private int _yellowTeamPlayerCount;
        private int _greenTeamModPlayerCount;
        private int _greenTeamPlayerCount;
        private int _blueTeamModPlayerCount;
        private int _blueTeamPlayerCount;
        private int _undefinedTeamPlayerCount;
        private int _undefinedTeamModPlayerCount;

        #endregion

        #region Logics

        public int MasterOnly_AddPlayer(int playerId)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_AddPlayer)));
                return -1;
            }

            if (HasPlayerIdOf(playerId))
            {
                logger.LogError($"{Prefix}Could not add player {playerId}: Player is already added");
                return -1;
            }

            var result = -1;
            for (var i = 0; i < GetPlayers().Length; i++)
            {
                var player = GetPlayer(i);
                if (player && player.PlayerId != -1) continue;
                MasterOnly_SetPlayer(i, playerId);
                result = i;
                return result;
            }

            logger.LogWarn($"{Prefix}Could not add player {playerId}: All instances are already active");
            return result;
        }

        public bool MasterOnly_RemovePlayer(int playerId)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_RemovePlayer)));
                return false;
            }

            var result = false;
            for (var i = 0; i < GetPlayers().Length; i++)
            {
                var player = GetPlayer(i);
                if (player && player.PlayerId != playerId) continue;
                MasterOnly_SetPlayer(i, -1);
                result = true;
                break;
            }

            if (result == false)
                logger.LogWarn($"{Prefix}Could not remove player {playerId}: Player not found");

            return result;
        }

        public void MasterOnly_SetPlayer(int index, int newPlayerId)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetPlayer)));
                return;
            }

            if (index < 0 || GetPlayers().Length < index)
            {
                logger.LogError($"{Prefix}Could not set player: Index out of bounds!");
                return;
            }

            var instance = GetPlayer(index);

            if (!instance)
            {
                logger.LogError($"{Prefix}Could not set player: Retrieved player instance was null!");
                return;
            }

            instance.SetPlayer(newPlayerId);
        }

        public void MasterOnly_SetTeam(int index, int newTeam)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetTeam)));
                return;
            }

            if (index < 0 || GetPlayers().Length < index)
            {
                logger.LogError($"{Prefix}Could not set team: Index out of bounds!");
                return;
            }

            var instance = GetPlayer(index);

            if (!instance)
            {
                logger.LogError($"{Prefix}Could not set player: Retrieved player instance was null!");
                return;
            }

            instance.SetTeam(newTeam);
        }

        public void MasterOnly_ResetAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetAllPlayer)));
                return;
            }

            foreach (var player in GetPlayers())
                if (player)
                    player.ResetPlayer();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateLocalPlayer));
            logger.LogVerbose($"{Prefix}All player instance's reset complete");
        }

        public void MasterOnly_AddAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_AddAllPlayer)));
                return;
            }

            foreach (var vrcPlayer in VRCPlayerApi.GetPlayers(new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]))
                if (vrcPlayer != null && vrcPlayer.IsValid() && !HasPlayerIdOf(vrcPlayer.playerId))
                    MasterOnly_AddPlayer(vrcPlayer.playerId);

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateLocalPlayer));
            logger.LogVerbose($"{Prefix}Added all vrc players");
        }

        public void MasterOnly_SyncAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SyncAllPlayer)));
                return;
            }

            foreach (var player in GetPlayers())
                if (player != null)
                    player.Sync();

            logger.LogVerbose($"{Prefix}Synced all shooter players");
            if (Networking.IsClogged)
                logger.LogWarn($"{Prefix}Network is clogged. there might be death run!");
        }

        public void MasterOnly_ResetTeam()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetTeam)));
                return;
            }

            foreach (var player in GetPlayers())
                if (player != null)
                    player.SetTeam(0);

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateLocalPlayer));
            logger.LogVerbose($"{Prefix}Cleared all player teams");
        }

        public void MasterOnly_SetTeamTagShown(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetTeamTagShown)));
                return;
            }

            ShowTeamTag = isOn;
            RequestSerialization();
        }

        public void MasterOnly_SetStaffTagShown(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetStaffTagShown)));
                return;
            }

            ShowStaffTag = isOn;
            RequestSerialization();
        }

        public void MasterOnly_SetCreatorTagShown(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetCreatorTagShown)));
                return;
            }

            ShowCreatorTag = isOn;
            RequestSerialization();
        }

        public override void SetPlayerTag(TagType type, bool isOn)
        {
            switch (type)
            {
                case TagType.Staff:
                    MasterOnly_SetStaffTagShown(isOn);
                    break;
                case TagType.Creator:
                    MasterOnly_SetCreatorTagShown(isOn);
                    break;
                case TagType.Team:
                    MasterOnly_SetTeamTagShown(isOn);
                    break;
            }
        }

        public override void SetFriendlyFireMode(FriendlyFireMode mode)
        {
            MasterOnly_SetFriendlyFireMode(mode);
        }


        [Obsolete("Use MasterOnly_SetFriendlyFireMode(FriendlyFireMode) instead")]
        public void MasterOnly_SetFriendlyFire(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetFriendlyFire)));
                return;
            }

            AllowFriendlyFire = isOn;
            RequestSerialization();
        }

        public void MasterOnly_SetFriendlyFireMode(FriendlyFireMode ffMode)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetFriendlyFireMode)));
                return;
            }

            FriendlyFireMode = ffMode;
            RequestSerialization();
        }

        public void MasterOnly_ResetAllPlayerStats()
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetAllPlayerStats)));
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Invoke_OnResetAllPlayerStats));
        }

        public void MasterOnly_ResetPlayerStats(int playerId)
        {
            if (!Networking.IsMaster)
            {
                logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetPlayerStats)));
                return;
            }

            var instance = GetPlayerById(playerId);

            if (!instance)
            {
                logger.LogError($"{Prefix}Could not reset stats: PlayerBase with such player id not found!");
                return;
            }

            instance.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(instance.ResetStats));
        }

        public void UpdateLocalPlayer()
        {
            var localPlayerId = Networking.LocalPlayer.playerId;

            foreach (var player in GetPlayers())
            {
                if (!player || player.PlayerId != localPlayerId) continue;
                Invoke_OnLocalPlayerChanged(player, player.Index);
                return;
            }

            logger.Log(
                $"{Prefix}Could not find LocalPlayer: Setting index to -1!");
            Invoke_OnLocalPlayerChanged(null, -1);
        }

        public void UpdateTeamPlayerCount()
        {
            var noneTeam = 0;
            var noneTeamMod = 0;
            var redTeam = 0;
            var redTeamMod = 0;
            var yelTeam = 0;
            var yelTeamMod = 0;
            var greTeam = 0;
            var greTeamMod = 0;
            var bluTeam = 0;
            var bluTeamMod = 0;
            var undefinedTeam = 0;
            var undefinedTeamMod = 0;

            foreach (var player in GetPlayers())
            {
                if (!player || !player.IsAssigned) continue;
                switch (player.TeamId)
                {
                    case 0:
                        ++noneTeam;
                        if (player.Role.IsGameStaff())
                            ++noneTeamMod;
                        break;
                    case 1:
                        ++redTeam;
                        if (player.Role.IsGameStaff())
                            ++redTeamMod;
                        break;
                    case 2:
                        ++yelTeam;
                        if (player.Role.IsGameStaff())
                            ++yelTeamMod;
                        break;
                    case 3:
                        ++greTeam;
                        if (player.Role.IsGameStaff())
                            ++greTeamMod;
                        break;
                    case 4:
                        ++bluTeam;
                        if (player.Role.IsGameStaff())
                            ++bluTeamMod;
                        break;
                    default:
                        ++undefinedTeam;
                        if (player.Role.IsGameStaff())
                            ++undefinedTeamMod;
                        break;
                }
            }

            _noneTeamPlayerCount = noneTeam;
            _noneTeamModPlayerCount = noneTeamMod;
            _redTeamPlayerCount = redTeam;
            _redTeamModPlayerCount = redTeamMod;
            _yellowTeamPlayerCount = yelTeam;
            _yellowTeamModPlayerCount = yelTeamMod;
            _greenTeamPlayerCount = greTeam;
            _greenTeamModPlayerCount = greTeamMod;
            _blueTeamPlayerCount = bluTeam;
            _blueTeamModPlayerCount = bluTeamMod;
            _undefinedTeamPlayerCount = undefinedTeam;
            _undefinedTeamModPlayerCount = undefinedTeamMod;
            _isTeamPlayerCountsDirty = false;
        }

        public void UpdateAllPlayerView()
        {
            foreach (var player in GetPlayers())
                if (player)
                    player.UpdateView();
            logger.LogVerbose($"{Prefix}Updated all player view");
        }

        #endregion

        #region Getters

        [PublicAPI]
        [ItemCanBeNull]
        public override PlayerBase[] GetPlayers()
        {
            return playerInstancePool;
        }

        [PublicAPI]
        [CanBeNull]
        public PlayerBase GetPlayer(int index)
        {
            if (index > playerInstancePool.Length || index < 0) return null;
            return GetPlayers()[index];
        }

        [PublicAPI]
        [CanBeNull]
        public override PlayerBase GetLocalPlayer()
        {
            return GetPlayer(GetLocalPlayerIndex());
        }

        public override PlayerBase GetPlayer(VRCPlayerApi player)
        {
            return GetPlayer(player.playerId);
        }

        [PublicAPI]
        public int GetMaxPlayerCount()
        {
            return GetPlayers().Length;
        }

        [PublicAPI]
        public int GetPlayerId(int index)
        {
            var player = GetPlayer(index);
            return player ? player.PlayerId : -1;
        }

        [PublicAPI]
        public int GetLocalPlayerIndex()
        {
            return _localPlayerIndex;
        }

        [PublicAPI]
        public bool CanJoin()
        {
            return !HasLocalPlayer();
        }

        [PublicAPI]
        public bool CanLeave()
        {
            return HasLocalPlayer();
        }

        [PublicAPI]
        public override bool HasLocalPlayer()
        {
            return _localPlayerIndex != -1;
        }

        [PublicAPI]
        [CanBeNull]
        public override PlayerBase GetPlayerById(int playerId)
        {
            foreach (var player in GetPlayers())
                if (player && player.IsAssigned && player.PlayerId == playerId)
                    return player;

            return null;
        }

        [PublicAPI]
        [CanBeNull]
        public PlayerBase GetPlayerByDisplayName(string displayName)
        {
            foreach (var player in GetPlayers())
                if (player && player.IsAssigned && player.VrcPlayer.SafeGetDisplayName() == displayName)
                    return player;

            return null;
        }

        [PublicAPI]
        public bool HasPlayerIdOf(int playerId)
        {
            return GetPlayerById(playerId);
        }

        public override Color GetTeamColor(int teamId)
        {
            if (teamId >= 0 && teamId < teamColors.Length)
                return teamColors[teamId];
            return teamColors[0];
        }

        #endregion
    }
}