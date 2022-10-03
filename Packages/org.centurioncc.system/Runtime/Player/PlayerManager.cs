using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
using CenturionCC.System.Utils.Watchdog;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Role;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
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
        private PlayerBase[] playerInstancePool;
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
        private bool _isDebug;

        private bool _isTeamPlayerCountsDirty;

        private int _localPlayerIndex = -1;
        private bool _showCollider;
        [UdonSynced] [FieldChangeCallback(nameof(ShowStaffTag))]
        private bool _showStaffTag = true;

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
        public bool AllowFriendlyFire { get; private set; }

        public bool UseBaseCollider
        {
            get => _useBaseCollider;
            set
            {
                _useBaseCollider = value;
                UpdateAllPlayerView();
            }
        }

        public bool UseAdditionalCollider
        {
            get => _useAdditionalCollider;
            set
            {
                _useAdditionalCollider = value;
                UpdateAllPlayerView();
            }
        }

        public bool UseLightweightCollider
        {
            get => _useLightweightCollider;
            set
            {
                _useLightweightCollider = value;
                UpdateAllPlayerView();
            }
        }

        public bool AlwaysUseLightweightCollider
        {
            get => _alwaysUseLightweightCollider;
            set
            {
                _alwaysUseLightweightCollider = value;
                UpdateAllPlayerView();
            }
        }

        public bool IsDebug
        {
            get => _isDebug;
            set
            {
                _isDebug = value;
                UpdateAllPlayerView();
            }
        }

        public bool ShowTeamTag
        {
            get => _showTeamTag;
            private set
            {
                var shouldNotify = _showTeamTag != value;
                _showTeamTag = value;
                if (shouldNotify)
                    Invoke_OnPlayerTagChanged(TagType.Team, value);
            }
        }

        public bool ShowStaffTag
        {
            get => _showStaffTag;
            private set
            {
                var shouldNotify = _showStaffTag != value;
                _showStaffTag = value;
                if (shouldNotify)
                    Invoke_OnPlayerTagChanged(TagType.Staff, value);
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
                var players = new PlayerBase[transform.childCount];
                for (var i = 0; i < players.Length; i++)
                    players[i] = transform.GetChild(i).GetComponent<PlayerBase>();

                playerInstancePool = players;
            }

            Logger.Log($"{Prefix}Updating ShooterPlayer index");
            for (var i = 0; i < playerInstancePool.Length; i++)
            {
                var player = GetPlayer(i);
                if (player != null) player.SetId(i);
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
                if (player && player.PlayerId != -1) continue;
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
                if (player && player.PlayerId != playerId) continue;
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

            if (instance == null)
            {
                Logger.LogError($"{Prefix}Could not set player: Retrieved player instance was null!");
                return;
            }

            instance.SetPlayer(newPlayerId);
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

            if (instance == null)
            {
                Logger.LogError($"{Prefix}Could not set player: Retrieved player instance was null!");
                return;
            }

            instance.SetTeam(newTeam);
        }

        public void MasterOnly_ResetAllPlayer()
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_ResetAllPlayer)));
                return;
            }

            foreach (var player in GetPlayers())
                if (player != null)
                    player.ResetPlayer();

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

            foreach (var vrcPlayer in VRCPlayerApi.GetPlayers(new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]))
                if (vrcPlayer != null && vrcPlayer.IsValid() && !HasPlayerIdOf(vrcPlayer.playerId))
                    MasterOnly_AddPlayer(vrcPlayer.playerId);

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
                if (player != null)
                    player.Sync();

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
                if (player != null)
                    player.SetTeam(0);

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

        public void MasterOnly_SetStaffTagShown(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetStaffTagShown)));
                return;
            }

            ShowStaffTag = isOn;
            RequestSerialization();
        }

        public void MasterOnly_SetFriendlyFire(bool isOn)
        {
            if (!Networking.IsMaster)
            {
                Logger.LogError(string.Format(MustBeMasterError, nameof(MasterOnly_SetFriendlyFire)));
                return;
            }

            AllowFriendlyFire = isOn;
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

            var instance = GetPlayerById(playerId);

            if (instance == null)
            {
                Logger.LogError($"{Prefix}Could not reset stats: ShooterPlayer with such player id not found!");
                return;
            }

            instance.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(instance.ResetPlayer));
        }

        public void UpdateLocalPlayer()
        {
            var localPlayerId = Networking.LocalPlayer.playerId;

            foreach (var player in GetPlayers())
            {
                if (player == null || player.PlayerId != localPlayerId) continue;
                Invoke_OnLocalPlayerChanged(player, player.Index);
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
                if (player == null || !player.IsAssigned) continue;
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

        public void UpdateAllPlayerView()
        {
            foreach (var player in GetPlayers())
                if (player != null)
                    player.UpdateView();
            Logger.LogVerbose($"{Prefix}Updated all player view");
        }

        #endregion

        #region Getters

        [PublicAPI] [ItemCanBeNull]
        public PlayerBase[] GetPlayers()
        {
            return playerInstancePool;
        }

        [PublicAPI] [CanBeNull]
        public PlayerBase GetPlayer(int index)
        {
            if (index > playerInstancePool.Length || index < 0) return null;
            return GetPlayers()[index];
        }

        [PublicAPI] [CanBeNull]
        public PlayerBase GetLocalPlayer()
        {
            return GetPlayer(GetLocalPlayerIndex());
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
            return player != null ? player.PlayerId : -1;
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
        public bool HasLocalPlayer()
        {
            return _localPlayerIndex != -1;
        }

        [PublicAPI]
        public PlayerBase GetPlayerById(int playerId)
        {
            foreach (var player in GetPlayers())
                if (player != null && player.PlayerId == playerId)
                    return player;

            return null;
        }

        [PublicAPI]
        public bool HasPlayerIdOf(int playerId)
        {
            return GetPlayerById(playerId) != null;
        }

        [PublicAPI]
        public string GetTeamColorString(int teamId)
        {
            return _ToHtmlStringRGBA(GetTeamColor(teamId));
        }

        [PublicAPI]
        public Color GetTeamColor(int teamId)
        {
            if (teamId <= 0 || teamId >= teamColors.Length) return teamColors[0];
            return teamColors[teamId];
        }

        [PublicAPI]
        public string GetTeamColoredName(PlayerBase player)
        {
            if (player == null) return "Invalid Player (null)";
            return
                $"<color=#{GetTeamColorString(player.TeamId)}>{GameManager.GetPlayerName(player.VrcPlayer)}</color>";
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
                if (player != null)
                    player.ResetStats();

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnResetAllPlayerStats();
            }
        }

        public void Invoke_OnResetPlayerStats(PlayerBase player)
        {
            if (!player)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnResetPlayerStats called with player null");
                return;
            }

            Logger.Log(
                $"{Prefix}Invoke_OnResetAllPlayerStats: {player.name}, {GameManager.GetPlayerNameById(player.PlayerId)}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnResetPlayerStats(player);
            }
        }

        public void Invoke_OnPlayerChanged(PlayerBase player,
            int lastId, bool lastIsMod, bool lastActive)
        {
            if (!player)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnPlayerChanged called with player null");
                return;
            }

            if (lastId == player.PlayerId)
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
                $"{Prefix}Invoke_OnPlayerChanged: {player.name}, {GameManager.GetPlayerNameById(lastId)}, {GameManager.GetPlayerNameById(player.PlayerId)}");

            if (player.IsAssigned && !lastActive)
            {
                ++PlayerCount;
                if (player.Role.HasPermission())
                    ++ModeratorPlayerCount;
            }
            else if (!player.IsAssigned && lastActive)
            {
                --PlayerCount;
                if (lastIsMod)
                    --ModeratorPlayerCount;
            }
            else if (player.IsAssigned)
            {
                if (player.Role.HasPermission() && !lastIsMod)
                    ++ModeratorPlayerCount;
                else if (!player.Role.HasPermission() && lastIsMod)
                    --ModeratorPlayerCount;
            }

            UpdateAllPlayerView();

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnPlayerChanged(player, lastId, player.PlayerId);
            }

            if (player.PlayerId == Networking.LocalPlayer.playerId)
                Invoke_OnLocalPlayerChanged(player, player.Index);

            if (lastId == Networking.LocalPlayer.playerId) Invoke_OnLocalPlayerChanged(player, -1);
        }

        public void Invoke_OnLocalPlayerChanged(PlayerBase playerNullable, int index)
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

        public void Invoke_OnTeamChanged(PlayerBase player, int lastTeam)
        {
            if (lastTeam == player.TeamId)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnTeamChanged called without actual team not changed");
                return;
            }

            Logger.Log(
                $"{Prefix}Invoke_OnTeamChanged: {player.name}, {GameManager.GetPlayerNameById(player.PlayerId)}, {lastTeam}, {player.TeamId}");

            _isTeamPlayerCountsDirty = true;
            UpdateAllPlayerView();

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnTeamChanged(player, lastTeam);
            }
        }

        public void Invoke_OnFriendlyFire(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            Logger.Log(
                $"{Prefix}Invoke_OnFriendlyFire: {GameManager.GetPlayerNameById(firedPlayer.PlayerId)}, {hitPlayer.TeamId}");

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnFriendlyFire(firedPlayer, hitPlayer);
            }
        }

        public void Invoke_OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint,
            bool isShooterDetection)
        {
            if (playerCollider == null || damageData == null)
            {
                Logger.LogWarn($"{Prefix}Invoke_OnHitDetection called without actual data.");
                return;
            }

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
        }

        public void Invoke_OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
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

        public void Invoke_OnPlayerTagChanged(TagType type, bool isOn)
        {
            Logger.Log(
                $"{Prefix}Invoke_OnPlayerTagChanged: {type}, {isOn}");

            UpdateAllPlayerView();

            foreach (var callback in _eventCallbacks)
            {
                if (callback == null) continue;
                ((PlayerManagerCallbackBase)callback).OnPlayerTagChanged(type, isOn);
            }
        }

        #endregion
    }
}