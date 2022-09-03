using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Player
{
    [DefaultExecutionOrder(20)] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerManager : UdonSharpBehaviour
    {
        private const string Prefix = "[<color=orange>PlayerManager</color>] ";
        private const string MustBeMasterError =
            Prefix + "<color=red>You must be an master to execute this method</color>: {0}";
        [SerializeField]
        private bool autoAddPlayerAtJoin = true;
        [SerializeField]
        private ShooterPlayer[] playerInstancePool;
        [SerializeField]
        private GameManager manager;
        [SerializeField]
        private FootstepAudioStore footstepAudio;
        [SerializeField]
        private Color[] teamColors;

        private bool _alwaysUseLightweightCollider;

        private WatchdogChildCallbackBase[] _callbacks;
        private int _eventCallbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[5];

        private bool _isTeamPlayerCountsDirty;

        private int _localPlayerIndex = -1;
        private bool _showCollider;
        private bool _showDebugNametag;

        [UdonSynced] [FieldChangeCallback(nameof(ShowTeamTag))]
        private bool _showTeamTag;
        private bool _useAdditionalCollider;
        private bool _useBaseCollider;
        private bool _useLightweightCollider;

        public UpdateManager UpdateManager => manager.updateManager;

        public PrintableBase Logger => manager.logger;

        public AudioManager AudioManager => manager.audioManager;

        public RoleProvider RoleManager => manager.roleProvider;

        public FootstepAudioStore FootstepAudio => footstepAudio;

        [field: UdonSynced] [field: FieldChangeCallback(nameof(AllowFriendlyFire))]
        public bool AllowFriendlyFire { get; set; }

        public bool ShowCollider
        {
            get => _showCollider;
            set
            {
                foreach (var player in GetPlayers())
                    player.PlayerHumanoidCollider.IsCollidersVisible = value;
                _showCollider = value;
            }
        }

        public bool UseBaseCollider
        {
            get => _useBaseCollider;
            set
            {
                if (_useBaseCollider != value)
                    foreach (var player in GetPlayers())
                        player.PlayerHumanoidCollider.UseBaseCollider = value;
                _useBaseCollider = value;
            }
        }

        public bool UseAdditionalCollider
        {
            get => _useAdditionalCollider;
            set
            {
                if (_useAdditionalCollider != value)
                    foreach (var player in GetPlayers())
                        player.PlayerHumanoidCollider.UseAdditionalCollider = value;
                _useAdditionalCollider = value;
            }
        }

        public bool UseLightweightCollider
        {
            get => _useLightweightCollider;
            set
            {
                if (_useLightweightCollider != value)
                    foreach (var player in GetPlayers())
                        player.PlayerHumanoidCollider.UseLightweightCollider = value;
                _useLightweightCollider = value;
            }
        }

        public bool AlwaysUseLightweightCollider
        {
            get => _alwaysUseLightweightCollider;
            set
            {
                if (_alwaysUseLightweightCollider != value)
                    foreach (var player in GetPlayers())
                        player.PlayerHumanoidCollider.AlwaysUseLightweightCollider = value;
                _alwaysUseLightweightCollider = value;
            }
        }

        public bool ShowDebugNametag
        {
            get => _showDebugNametag;
            set
            {
                if (_showDebugNametag == value)
                    return;
                _showDebugNametag = value;
                foreach (var player in GetPlayers())
                    player.PlayerTag.SetDebugTagShown(value);
            }
        }

        public bool ShowTeamTag
        {
            get => _showTeamTag;
            private set
            {
                if (_showTeamTag == value) return;
                _showTeamTag = value;
                foreach (var player in GetPlayers())
                    player.PlayerTag.SetTeamTagShown(value);
            }
        }

        private void Start()
        {
            if (!manager)
            {
                Debug.LogError($"{Prefix}GameManager instance not found!");
                return;
            }

            if (playerInstancePool == null || playerInstancePool.Length <= 0)
            {
                Logger.Log($"{Prefix}Getting ShooterPlayer instances");
                var players = new ShooterPlayer[transform.childCount];
                for (var i = 0; i < players.Length; i++)
                    players[i] = transform.GetChild(i).GetComponent<ShooterPlayer>();

                playerInstancePool = players;
            }

            Logger.Log($"{Prefix}Updating ShooterPlayer index");
            for (var i = 0; i < playerInstancePool.Length; i++)
            {
                var player = GetPlayer(i);
                if (player != null) player.Index = i;
            }

            Logger.Log($"{Prefix}Generate Watchdog Callback");
            var callbacks = new WatchdogChildCallbackBase[playerInstancePool.Length];
            for (var i = 0; i < playerInstancePool.Length; i++)
                callbacks[i] = (WatchdogChildCallbackBase)(UdonSharpBehaviour)playerInstancePool[i];
            _callbacks = callbacks;

            UseBaseCollider = true;
            UseAdditionalCollider = true;
            UseLightweightCollider = true;
            AlwaysUseLightweightCollider = false;

            Logger.LogVerbose($"{Prefix}Start complete");
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
            if (IsReady() && Networking.GetOwner(gameObject).IsValid())
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

        #region CallbackRegisterer

        public void SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            if (_eventCallbacks == null)
            {
                _eventCallbacks = new UdonSharpBehaviour[5];
                _eventCallbackCount = 0;
            }

            _eventCallbacks = _AddBehaviour(_eventCallbackCount++, behaviour, _eventCallbacks);
        }

        public void UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            if (_eventCallbacks == null)
            {
                _eventCallbacks = new UdonSharpBehaviour[5];
                _eventCallbackCount = 0;
            }

            var result = _RemoveBehaviour(behaviour, _eventCallbacks);
            if (result == null) return;
            --_eventCallbackCount;
            _eventCallbacks = result;
        }

        /// <summary>
        ///     Adds provided behaviour into provided array
        /// </summary>
        /// <param name="index">index of insert point</param>
        /// <param name="item">an item to insert into <c>arr</c></param>
        /// <param name="arr">an array which <c>item</c> gets inserted</param>
        /// <returns>An array which <c>item</c> is inserted at <c>index</c>. Returns null when invalid params are provided!</returns>
        private UdonSharpBehaviour[] _AddBehaviour(int index, UdonSharpBehaviour item, UdonSharpBehaviour[] arr)
        {
            if (arr == null || item == null || index < 0 || index > arr.Length + 5)
                return null;

            if (arr.Length <= index)
            {
                var newArr = new UdonSharpBehaviour[arr.Length + 5];
                Array.Copy(arr, newArr, arr.Length);
                arr = newArr;
            }

            Debug.Log($"add behaviour at {index} {item.name}");

            arr[index] = item;
            return arr;
        }

        /// <summary>
        ///     Removes provided behaviour from provided array
        /// </summary>
        /// <param name="item">an item to remove</param>
        /// <param name="arr">an array which <c>item</c> will get removed from</param>
        /// <returns>An array which <c>item</c> is removed and items after <c>item</c> is moved to fill space</returns>
        private UdonSharpBehaviour[] _RemoveBehaviour(UdonSharpBehaviour item, UdonSharpBehaviour[] arr)
        {
            if (item == null || arr == null)
                return null;

            var index = Array.IndexOf(arr, item);
            if (index == -1)
                return null;
            Array.ConstrainedCopy(arr, index + 1, arr, index, arr.Length - 1 - index);
            return arr;
        }

        #endregion

        #region Counters

        public int PlayerCount { get; private set; }

        public int ModeratorPlayerCount { get; private set; }

        public int NoneTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _noneTeamPlayerCount;
            }
            private set => _noneTeamPlayerCount = value;
        }
        public int NoneTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _noneTeamModPlayerCount;
            }
            private set => _noneTeamModPlayerCount = value;
        }
        public int RedTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _redTeamPlayerCount;
            }
            private set => _redTeamPlayerCount = value;
        }
        public int RedTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _redTeamModPlayerCount;
            }
            private set => _redTeamModPlayerCount = value;
        }
        public int YellowTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _yellowTeamPlayerCount;
            }
            private set => _yellowTeamPlayerCount = value;
        }
        public int YellowTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _yellowTeamModPlayerCount;
            }
            private set => _yellowTeamModPlayerCount = value;
        }
        public int GreenTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _greenTeamPlayerCount;
            }
            private set => _greenTeamPlayerCount = value;
        }
        public int GreenTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _greenTeamModPlayerCount;
            }
            private set => _greenTeamModPlayerCount = value;
        }
        public int BlueTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _blueTeamPlayerCount;
            }
            private set => _blueTeamPlayerCount = value;
        }
        public int BlueTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _blueTeamModPlayerCount;
            }
            private set => _blueTeamModPlayerCount = value;
        }
        public int UndefinedTeamPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _undefinedTeamPlayerCount;
            }
            private set => _undefinedTeamPlayerCount = value;
        }
        public int UndefinedTeamModeratorPlayerCount
        {
            get
            {
                if (_isTeamPlayerCountsDirty)
                    UpdateTeamPlayerCount();
                return _undefinedTeamModPlayerCount;
            }
            private set => _undefinedTeamModPlayerCount = value;
        }

        #endregion

        #region PlayerCounts

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
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_AddPlayer)));
                return -1;
            }

            if (HasPlayerIdOf(playerId))
            {
                Logger.LogError($"{Prefix}Could not add player {playerId}: Player is already added");
                return -1;
            }

            var result = -1;
            for (var i = 0; i < GetPlayers().Length; i++)
            {
                var player = GetPlayer(i);
                if (player && player.IsActive) continue;
                MasterOnly_SetPlayer(i, playerId);
                result = i;
                return result;
            }

            Logger.LogWarn($"{Prefix}Could not add player {playerId}: All instances are already active");
            return result;
        }

        public bool MasterOnly_RemovePlayer(int playerId)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_RemovePlayer)));
                return false;
            }

            var result = false;
            for (var i = 0; i < GetPlayers().Length; i++)
            {
                var player = GetPlayer(i);
                if (player && player.SyncedPlayerId != playerId) continue;
                MasterOnly_SetPlayer(i, -1);
                result = true;
                break;
            }

            if (result == false)
                Logger.LogWarn($"{Prefix}Could not remove player {playerId}: Player not found");

            return result;
        }

        public void MasterOnly_SetPlayer(int index, int newPlayerId)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetPlayer)));
                return;
            }

            if (index < 0 || GetPlayers().Length < index)
            {
                Logger.LogError($"{Prefix}Could not set player: Index out of bounds!");
                return;
            }

            var instance = GetPlayer(index);

            instance.MasterOnly_SetPlayer(newPlayerId);
        }

        public void MasterOnly_SetTeam(int index, int newTeam)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetTeam)));
                return;
            }

            if (index < 0 || GetPlayers().Length < index)
            {
                Logger.LogError($"{Prefix}Could not set team: Index out of bounds!");
                return;
            }

            var instance = GetPlayer(index);
            instance.MasterOnly_SetTeam(newTeam);
        }

        public void MasterOnly_ResetAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetAllPlayer)));
                return;
            }

            for (var i = 0; i < GetPlayers().Length; i++)
                GetPlayer(i).MasterOnly_Reset();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateLocalPlayer));
            Logger.LogVerbose($"{Prefix}All player instance's reset complete");
        }

        public void MasterOnly_AddAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_AddAllPlayer)));
                return;
            }

            foreach (var vrcPlayer in GetVRCPlayers())
                if (vrcPlayer == null || !vrcPlayer.IsValid()) continue;
                else if (!HasPlayerIdOf(vrcPlayer.playerId)) MasterOnly_AddPlayer(vrcPlayer.playerId);

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateLocalPlayer));
            Logger.LogVerbose($"{Prefix}Added all vrc players");
        }

        public void MasterOnly_SyncAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SyncAllPlayer)));
                return;
            }

            foreach (var player in GetPlayers())
                if (player == null) continue;
                else player.MasterOnly_Sync();

            Logger.LogVerbose($"{Prefix}Synced all shooter players");
            if (Networking.IsClogged)
                Logger.LogWarn($"{Prefix}Network is clogged. there might be death run!");
        }

        public void MasterOnly_ResetTeam()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetTeam)));
                return;
            }

            foreach (var player in GetPlayers())
                player.MasterOnly_SetTeam(0);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UpdateLocalPlayer));
            Logger.LogVerbose($"{Prefix}Cleared all player teams");
        }

        public void MasterOnly_SetTeamTagShown(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetTeamTagShown)));
                return;
            }

            ShowTeamTag = isOn;
            RequestSerialization();
        }

        public void MasterOnly_ResetAllPlayerStats()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetAllPlayerStats)));
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Invoke_OnResetAllPlayerStats));
        }

        public void MasterOnly_ResetPlayerStats(int playerId)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetAllPlayerStats)));
                return;
            }

            var instance = GetShooterPlayerByPlayerId(playerId);

            if (instance == null)
            {
                Logger.LogError($"{Prefix}Could not reset stats: ShooterPlayer with such player id not found!");
                return;
            }

            instance.PlayerStats.SendCustomNetworkEvent(NetworkEventTarget.All,
                nameof(instance.PlayerStats.ResetStats));
        }

        public void UpdateLocalPlayer()
        {
            for (var i = 0; i < GetPlayers().Length; i++)
            {
                var shooterPlayer = GetPlayer(i);
                if (shooterPlayer.SyncedPlayerId != Networking.LocalPlayer.playerId) continue;
                Invoke_OnLocalPlayerChanged(shooterPlayer, i);
                return;
            }

            Logger.Log(
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
                if (!player.IsActive) continue;
                switch (player.Team)
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

            NoneTeamPlayerCount = noneTeam;
            NoneTeamModeratorPlayerCount = noneTeamMod;
            RedTeamPlayerCount = redTeam;
            RedTeamModeratorPlayerCount = redTeamMod;
            YellowTeamPlayerCount = yelTeam;
            YellowTeamModeratorPlayerCount = yelTeamMod;
            GreenTeamPlayerCount = greTeam;
            GreenTeamModeratorPlayerCount = greTeamMod;
            BlueTeamPlayerCount = bluTeam;
            BlueTeamModeratorPlayerCount = bluTeamMod;
            UndefinedTeamPlayerCount = undefinedTeam;
            UndefinedTeamModeratorPlayerCount = undefinedTeamMod;
            _isTeamPlayerCountsDirty = false;
        }

        #endregion

        #region Getters

        // ReSharper disable once InconsistentNaming
        public VRCPlayerApi[] GetVRCPlayers()
        {
            return VRCPlayerApi.GetPlayers(new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]);
        }

        public ShooterPlayer[] GetPlayers()
        {
            return playerInstancePool;
        }

        public ShooterPlayer GetPlayer(int index)
        {
            if (index > playerInstancePool.Length || index < 0) return null;
            return GetPlayers()[index];
        }

        public ShooterPlayer GetLocalPlayer()
        {
            if (GetLocalPlayerIndex() == -1) return null;
            return GetPlayer(GetLocalPlayerIndex());
        }

        public int GetMaxPlayerCount()
        {
            return GetPlayers().Length;
        }

        public int GetPlayerId(int index)
        {
            return GetPlayers()[index].SyncedPlayerId;
        }

        public int GetLocalPlayerIndex()
        {
            return _localPlayerIndex;
        }

        public bool CanJoin()
        {
            return !HasLocalPlayer();
        }

        public bool CanLeave()
        {
            return HasLocalPlayer();
        }

        public bool HasLocalPlayer()
        {
            return _localPlayerIndex != -1;
        }

        public bool IsReady()
        {
            return manager;
        }

        public ShooterPlayer GetShooterPlayerByPlayerId(int playerId)
        {
            foreach (var player in GetPlayers())
                if (player.SyncedPlayerId == playerId)
                    return player;

            return null;
        }

        public bool HasPlayerIdOf(int playerId)
        {
            foreach (var player in GetPlayers())
                if (player.SyncedPlayerId == playerId)
                    return true;

            return false;
        }

        public string GetTeamColorString(int teamId)
        {
            return _ToHtmlStringRGBA(GetTeamColor(teamId));
        }

        public Color GetTeamColor(int teamId)
        {
            if (teamId <= 0 || teamId >= teamColors.Length) return teamColors[0];
            return teamColors[teamId];
        }

        public string GetTeamColoredName(ShooterPlayer player)
        {
            if (player == null) return "Invalid Player (null)";
            return $"<color=#{GetTeamColorString(player.Team)}>{GameManager.GetPlayerName(player.VrcPlayer)}</color>";
        }

        // From UnityEngine.ColorUtility
        private string _ToHtmlStringRGBA(Color color)
        {
            var color32 = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(color.r * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * byte.MaxValue), 0, byte.MaxValue),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * byte.MaxValue), 0, byte.MaxValue));
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
        }

        #endregion

        #region PlayerManagerEvents

        public void Invoke_OnResetAllPlayerStats()
        {
            Logger.Log($"{Prefix}Invoke_OnResetAllPlayerStats");

            foreach (var player in GetPlayers())
                player.PlayerStats.ResetStats();

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnResetAllPlayerStats();
            }
        }

        public void Invoke_OnResetPlayerStats(ShooterPlayer player)
        {
            if (!player)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnResetPlayerStats called with player null");
                return;
            }

            Logger.Log(
                $"{Prefix}Invoke_OnResetAllPlayerStats: {player.name}, {GameManager.GetPlayerNameById(player.SyncedPlayerId)}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnResetPlayerStats(player);
            }
        }

        public void Invoke_OnPlayerChanged(ShooterPlayer player,
            int lastId, bool lastIsMod, bool lastActive)
        {
            if (!player)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnPlayerChanged called with player null");
                return;
            }

            if (lastId == player.SyncedPlayerId)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnPlayerChanged called without actual player id not changed");
                return;
            }

            if (Networking.LocalPlayer == null)
            {
                Logger.LogError($"{Prefix}Invoke_OnPlayerChanged during world unload");
                return;
            }

            Logger.Log(
                $"{Prefix}Invoke_OnPlayerChanged: {player.name}, {GameManager.GetPlayerNameById(lastId)}, {GameManager.GetPlayerNameById(player.SyncedPlayerId)}");

            if (player.IsActive && !lastActive)
            {
                ++PlayerCount;
                if (player.Role.HasPermission())
                    ++ModeratorPlayerCount;
            }
            else if (!player.IsActive && lastActive)
            {
                --PlayerCount;
                if (lastIsMod)
                    --ModeratorPlayerCount;
            }
            else if (player.IsActive)
            {
                if (player.Role.HasPermission() && !lastIsMod)
                    ++ModeratorPlayerCount;
                else if (!player.Role.HasPermission() && lastIsMod)
                    --ModeratorPlayerCount;
            }

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnPlayerChanged(player, lastId, player.SyncedPlayerId);
            }

            if (player.SyncedPlayerId == Networking.LocalPlayer.playerId)
                Invoke_OnLocalPlayerChanged(player, player.Index);

            if (lastId == Networking.LocalPlayer.playerId) Invoke_OnLocalPlayerChanged(player, -1);
        }

        public void Invoke_OnLocalPlayerChanged(ShooterPlayer playerNullable, int index)
        {
            Logger.Log(
                $"{Prefix}Invoke_OnLocalPlayerChanged: {(playerNullable != null ? playerNullable.name : "null")}. {index}");

            _localPlayerIndex = index;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnLocalPlayerChanged(playerNullable, index);
            }
        }

        public void Invoke_OnTeamChanged(ShooterPlayer player, int lastTeam)
        {
            if (lastTeam == player.Team)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnTeamChanged called without actual team not changed");
                return;
            }

            Logger.Log(
                $"{Prefix}Invoke_OnTeamChanged: {player.name}, {GameManager.GetPlayerNameById(player.SyncedPlayerId)}, {lastTeam}, {player.Team}");

            _isTeamPlayerCountsDirty = true;

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnTeamChanged(player, lastTeam);
            }
        }

        public void Invoke_OnFriendlyFire(ShooterPlayer firedPlayer, ShooterPlayer hitPlayer)
        {
            Logger.Log(
                $"{Prefix}Invoke_OnFriendlyFire: {GameManager.GetPlayerNameById(firedPlayer.SyncedPlayerId)}, {hitPlayer.Team}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnFriendlyFire(firedPlayer, hitPlayer);
            }
        }

        public void Invoke_OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
            Logger.Log($"{Prefix}Invoke_OnHitDetection: " +
                       $"{(playerCollider != null ? playerCollider.name : "null")}, " +
                       $"{(damageData != null ? damageData.DamageType : "null")} , " +
                       $"{isShooterDetection}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnHitDetection(
                    playerCollider,
                    damageData,
                    contactPoint,
                    isShooterDetection);
            }


            if (playerCollider == null || damageData == null)
            {
                Logger.LogError($"{Prefix}Invoke_OnHitDetection: Either of objects were null! will not check!");
                return;
            }

            if (!damageData.ShouldApplyDamage)
            {
                Logger.Log($"{Prefix}Will ignore hit detection because DamageData.ShouldApplyDamage is false.");
                return;
            }

            var hitPlayer = playerCollider.player;
            var firedPlayer = GetShooterPlayerByPlayerId(damageData.DamagerPlayerId);
            var localPlayerId = Networking.LocalPlayer.playerId;

            if (hitPlayer == null)
            {
                Logger.LogWarn($"{Prefix}Will ignore hit detection because hit player is null.");
                return;
            }

            if (firedPlayer == null)
            {
                Logger.LogWarn(
                    $"{Prefix}Will ignore hit detection to {GameManager.GetPlayerName(hitPlayer.VrcPlayer)} because shooter '{GameManager.GetPlayerNameById(damageData.DamagerPlayerId)}' is not in game");
                return;
            }

            if (hitPlayer.SyncedPlayerId == firedPlayer.SyncedPlayerId)
            {
                Logger.LogWarn(
                    $"{Prefix}Will ignore hit detection to {GameManager.GetPlayerName(hitPlayer.VrcPlayer)} because shooter is same player.");
                return;
            }

            if (hitPlayer.SyncedPlayerId != localPlayerId && firedPlayer.SyncedPlayerId != localPlayerId)
            {
                Logger.LogWarn(
                    $"{Prefix}Will ignore hit detection to {GameManager.GetPlayerName(hitPlayer.VrcPlayer)} because neither is local player.");
                return;
            }

            if (hitPlayer.Team == firedPlayer.Team)
            {
                Invoke_OnFriendlyFire(firedPlayer, hitPlayer);
                if (hitPlayer.Team != 0 && !AllowFriendlyFire)
                    return;
            }

            var hitPlayerStats = hitPlayer.PlayerStats;

            if (Networking.GetNetworkDateTime().Subtract(hitPlayerStats.LastHitTime).TotalSeconds < 5F)
            {
                Logger.Log(
                    $"{Prefix}Will ignore hit detection to {GameManager.GetPlayerName(hitPlayer.VrcPlayer)} because that player has been hit recently.");
                return;
            }

            hitPlayerStats.LastDamagerPlayerId = damageData.DamagerPlayerId;
            hitPlayerStats.LastHitTime = Networking.GetNetworkDateTime();
            ++hitPlayerStats.Death;
            hitPlayerStats.Sync();
        }

        public void Invoke_OnKilled(ShooterPlayer firedPlayer, ShooterPlayer hitPlayer)
        {
            if (firedPlayer == null || hitPlayer == null)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnKilled called without actual player.");
                return;
            }

            Logger.Log(
                $"{Prefix}Invoke_OnKilled: {(firedPlayer != null ? GameManager.GetPlayerName(firedPlayer.VrcPlayer) : "null")}, {(hitPlayer != null ? GameManager.GetPlayerName(hitPlayer.VrcPlayer) : "null")}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnKilled(firedPlayer, hitPlayer);
            }
        }

        public void Invoke_OnPlayerTagChanged(ShooterPlayer player, TagType type, bool isOn)
        {
            Logger.Log(
                $"{Prefix}Invoke_OnPlayerTagChanged: {(player != null ? GameManager.GetPlayerName(player.VrcPlayer) : "null")}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnPlayerTagChanged(player, type, isOn);
            }
        }

        #endregion
    }
}