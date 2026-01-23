using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Command.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerManagerCommand : NewbieConsoleCommandHandler
    {
        private const string Prefix = "[PlayerManagerCommand] ";
        private const string RequestedFormat = Prefix + "Requested to {0} player {1} with request version of {2}";
        private const string ReceivedFormat = Prefix + "Received {0} for {1} with request version of {2}";

        [SerializeField]
        private Transform[] teamPositions;

        [SerializeField]
        private Collider[] teamRegions;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        private NewbieConsole _console;
        private int _lastRequestVersion;

        [UdonSynced]
        private int _requestVersion;

        [UdonSynced]
        private int _targetOperation;

        [UdonSynced]
        private int _targetPlayerId;

        [UdonSynced]
        private int _targetTeam;

        public override string Label => "PlayerManager";
        public override string[] Aliases => new[] { "Player", "PM" };

        public override string Usage => "<command>\n" +
                                        "   sync [playerId]\n" +
                                        "   syncAll\n" +
                                        "   stats [playerId]\n" +
                                        "   statsReset <playerId>\n" +
                                        "   statsResetAll\n" +
                                        "   revive [playerId]\n" +
                                        "   reviveAll\n" +
                                        "   teamReset\n" +
                                        "   team <team> [playerId]\n" +
                                        "   shuffleTeam [include moderators] [include green blue team]\n" +
                                        "   regionAdd <regionId> <teamId>\n" +
                                        "   showTeamTag [true|false]\n" +
                                        "   showStaffTag [true|false]\n" +
                                        "   showCreatorTag [true|false]\n" +
                                        "   friendlyFireMode [always|both|reverse|warning|never]" +
                                        "   list\n" +
                                        "   debug [true|false]\n";

        public override string Description => "Perform player related manipulation such as add/remove etc.";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return console.PrintUsage(this);

            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "s":
                case "sync":
                {
                    if (vars.Length >= 1)
                    {
                        console.Println("<color=red>Require PlayerId or Name</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    var player = playerManager.GetPlayer(ConsoleParser.TryParsePlayer(vars[1]));
                    if (!player)
                    {
                        console.Println("<color=red>PlayerId or Name is invalid</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    player.RequestSerialization();
                    return ConsoleLiteral.GetTrue();
                }
                case "syncall":
                {
                    if (EnsureModerator()) return ConsoleLiteral.GetFalse();
                    var players = playerManager.GetPlayers();
                    foreach (var playerBase in players) playerBase.RequestSerialization();

                    return ConsoleLiteral.GetTrue();
                }
                case "st":
                case "stats":
                {
                    var player = playerManager.GetLocalPlayer();

                    if (vars.Length >= 2)
                    {
                        var vrcPlayer = ConsoleParser.TryParsePlayer(vars[1]);
                        if (vrcPlayer == null)
                        {
                            console.Println("<color=red>Target player not found.</color>");
                            return ConsoleLiteral.Of(false);
                        }

                        player = playerManager.GetPlayer(vrcPlayer);
                    }

                    if (!player)
                    {
                        console.Println("<color=red>Target player is not in game.</color>");
                        return ConsoleLiteral.Of(false);
                    }

                    var status =
                        $"<color=orange><b>{NewbieUtils.GetPlayerName(player.VrcPlayer)}'s Stats</b></color>\n" +
                        $"K: {player.Kills}\n" +
                        $"D: {player.Deaths}\n" +
                        $"KDR: {(player.Deaths != 0 && player.Deaths != 0 ? $"{player.Kills / player.Deaths:F1}" : "Infinity")}";
                    console.Println(status);
                    return status;
                }
                case "resetstats":
                case "streset":
                case "statsreset":
                {
                    var player = playerManager.GetLocalPlayer();
                    if (vars.Length >= 2)
                    {
                        if (EnsureModerator()) return ConsoleLiteral.GetFalse();
                        player = playerManager.GetPlayer(ConsoleParser.TryParsePlayer(vars[1]));
                    }

                    if (!player)
                    {
                        console.Println("<color=red>PlayerId or Name is invalid</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    player.ResetToDefault();
                    return ConsoleLiteral.GetTrue();
                }
                case "resetstatsall":
                case "stresetall":
                case "statsresetall":
                {
                    if (EnsureModerator()) return ConsoleLiteral.GetFalse();
                    var players = playerManager.GetPlayers();
                    foreach (var playerBase in players) playerBase.ResetToDefault();

                    return ConsoleLiteral.GetTrue();
                }
                case "revive":
                case "rev":
                case "res":
                {
                    if (EnsureModerator()) return ConsoleLiteral.GetFalse();

                    var player = playerManager.GetLocalPlayer();
                    if (vars.Length >= 2)
                    {
                        if (EnsureModerator()) return ConsoleLiteral.GetFalse();
                        player = playerManager.GetPlayer(ConsoleParser.TryParsePlayer(vars[1]));
                    }

                    if (!player)
                    {
                        console.Println("<color=red>PlayerId or Name is invalid</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    player.Revive();
                    return ConsoleLiteral.GetTrue();
                }
                case "reviveall":
                case "revall":
                case "resall":
                {
                    if (EnsureModerator()) return ConsoleLiteral.GetFalse();
                    var players = playerManager.GetPlayers();
                    foreach (var playerBase in players) playerBase.Revive();

                    return ConsoleLiteral.GetTrue();
                }
                case "resetteam":
                case "teamreset":
                {
                    if (EnsureModerator()) return ConsoleLiteral.GetFalse();

                    var players = playerManager.GetPlayers();
                    foreach (var playerBase in players) playerBase.SetTeam(0);

                    return ConsoleLiteral.GetTrue();
                }
                case "t":
                case "team":
                case "changeteam":
                {
                    if (vars.Length <= 1)
                    {
                        console.Println("<color=red>Usage: <command> team <team_id> [playerId]</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    var targetPlayer = Networking.LocalPlayer;
                    var targetTeam = ConsoleParser.TryParseInt(vars[1]);
                    if (targetTeam < 0 || targetTeam > 255)
                    {
                        console.Println("<color=red>Specified team is invalid</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    if (vars.Length >= 3)
                    {
                        if (EnsureModerator()) return ConsoleLiteral.GetNone();

                        targetPlayer = ConsoleParser.TryParsePlayer(vars[2]);
                        if (targetPlayer == null)
                        {
                            console.Println("<color=red>Player id or name is invalid</color>");
                            return ConsoleLiteral.GetFalse();
                        }
                    }

                    var targetPlayerBase = playerManager.GetPlayer(targetPlayer);
                    if (!targetPlayerBase)
                    {
                        console.Println("<color=red>Specified player instance is not found</color>");
                        return ConsoleLiteral.GetFalse();
                    }

                    targetPlayerBase.SetTeam(targetTeam);
                    return ConsoleLiteral.GetTrue();
                }
                case "sh":
                case "shuffle":
                case "shuffleteam":
                {
                    return HandleRequestTeamShuffle(console, vars);
                }
                case "tra":
                case "regionadd":
                case "teamregionadd":
                {
                    return HandleRequestTeamRegionAdd(console, vars);
                }
                case "ttag":
                case "teamtag":
                case "showteamtag":
                {
                    if (vars.Length >= 2)
                    {
                        if (EnsureModerator()) return ConsoleLiteral.GetNone();

                        var isOn = ConsoleParser.TryParseBoolean(vars[1], playerManager.ShowTeamTag);
                        playerManager.SetPlayerTag(TagType.Team, isOn);
                    }

                    console.Println($"ShowTeamTag: {playerManager.ShowTeamTag}");
                    return ConsoleLiteral.Of(playerManager.ShowTeamTag);
                }
                case "stag":
                case "stafftag":
                case "showstafftag":
                {
                    if (vars.Length >= 2)
                    {
                        if (EnsureModerator()) return ConsoleLiteral.GetNone();

                        var isOn = ConsoleParser.TryParseBoolean(vars[1], playerManager.ShowStaffTag);
                        playerManager.SetPlayerTag(TagType.Staff, isOn);
                    }

                    console.Println($"ShowStaffTag: {playerManager.ShowStaffTag}");
                    return ConsoleLiteral.Of(playerManager.ShowStaffTag);
                }
                case "ctag":
                case "creatortag":
                case "showcreatortag":
                {
                    if (vars.Length >= 2)
                    {
                        if (EnsureModerator()) return ConsoleLiteral.GetNone();

                        var isOn = ConsoleParser.TryParseBoolean(vars[1], playerManager.ShowCreatorTag);
                        playerManager.SetPlayerTag(TagType.Creator, isOn);
                    }

                    console.Println($"ShowCreatorTag: {playerManager.ShowCreatorTag}");
                    return ConsoleLiteral.Of(playerManager.ShowCreatorTag);
                }
                case "ffmode":
                case "friendlyfiremode":
                {
                    if (vars.Length >= 2)
                    {
                        var mode = vars[1];
                        FriendlyFireMode modeEnum;
                        switch (mode.ToLower())
                        {
                            case "always":
                                modeEnum = FriendlyFireMode.Always;
                                break;
                            case "reverse":
                                modeEnum = FriendlyFireMode.Reverse;
                                break;
                            case "both":
                                modeEnum = FriendlyFireMode.Both;
                                break;
                            case "warning":
                                modeEnum = FriendlyFireMode.Warning;
                                break;
                            case "never":
                                modeEnum = FriendlyFireMode.Never;
                                break;
                            default:
                                console.Println("could not parse friendly fire mode enum");
                                return ConsoleLiteral.Of(false);
                        }

                        playerManager.SetFriendlyFireMode(modeEnum);
                        return modeEnum.ToEnumName();
                    }

                    var ffModeString = playerManager.FriendlyFireMode.ToEnumName();
                    console.Println(ffModeString);
                    return ffModeString;
                }
                case "l":
                case "list":
                    return HandleList(console, vars);
                case "d":
                case "debug":
                    return HandleDebug(console, label, vars);
                default:
                    return console.PrintUsage(this);
            }
            // ReSharper restore StringLiteralTypo
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
        }

        /// <returns>true when player is not moderator, false otherwise.</returns>
        private bool EnsureModerator()
        {
            if (!_console.IsSuperUser)
            {
                _console.Println("<color=red>Cannot execute this unless you're moderator!</color>");
                return true;
            }

            return false;
        }

        public override void OnPreSerialization()
        {
            ProcessReceivedData();
        }

        public override void OnDeserialization()
        {
            ProcessReceivedData();
        }

        private void ProcessReceivedData()
        {
            if (_console == null)
            {
                Debug.LogError($"{Prefix}Console is null!");
                return;
            }

            if (Networking.IsMaster && _lastRequestVersion != _requestVersion)
            {
                _lastRequestVersion = _requestVersion;
                switch (_targetOperation)
                {
                    case OpTeamShuffle:
                    {
                        var includeMod = _IsBitSet(_targetTeam, 1);
                        var includeGreenBlueTeam = _IsBitSet(_targetTeam, 2);
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpTeamShuffle),
                            $"All{(includeMod ? " +include_moderators" : "")}{(includeGreenBlueTeam ? " +include_greenAndBlue" : "")}",
                            _requestVersion));

                        var activePlayerCount = 0;
                        var players = playerManager.GetPlayers();
                        var activePlayers = new PlayerBase[players.Length];

                        _console.Println($"{Prefix}Shuffle Step 1: Pad players: {players.Length}");

                        foreach (var player in players)
                        {
                            if (!includeMod && playerManager.IsStaffTeamId(player.TeamId))
                                continue;
                            activePlayers[activePlayerCount] = player;
                            ++activePlayerCount;
                        }

                        _console.Println(
                            $"{Prefix}Shuffle Step 2: Trim inactive players: {activePlayerCount}");

                        var trimmedActivePlayers = new PlayerBase[activePlayerCount];
                        for (var i = 0; i < activePlayerCount; i++)
                            trimmedActivePlayers[i] = activePlayers[i];

                        _console.Println($"{Prefix}Shuffle Step 3: Shuffle player array");

                        var len = trimmedActivePlayers.Length;
                        for (var i = 0; i < len - 1; i++)
                        {
                            var rnd = Random.Range(i, len);
                            // ReSharper disable once SwapViaDeconstruction
                            var p = trimmedActivePlayers[rnd];
                            trimmedActivePlayers[rnd] = trimmedActivePlayers[i];
                            trimmedActivePlayers[i] = p;
                        }

                        _console.Println($"{Prefix}Shuffle Step 4: Set player teams");

                        var teamCount = includeGreenBlueTeam ? 4 : 2;
                        var teamSize = activePlayerCount / teamCount;

                        for (var team = 0; team < teamCount; team++)
                            for (var j = 0; j < teamSize; j++)
                            {
                                var i = teamSize * team + j;
                                if (!(i < trimmedActivePlayers.Length)) break;
                                trimmedActivePlayers[i].SetTeam(team + 1);
                            }

                        if (trimmedActivePlayers.Length % 2 != 0)
                            trimmedActivePlayers[trimmedActivePlayers.Length - 1].SetTeam(1);

                        _console.Println($"{Prefix}Shuffle Step 5: Schedule player teleportation");

                        SendCustomEventDelayedSeconds(
                            includeMod
                                ? nameof(_ExecutePlayerTeleportationForAllPlayers)
                                : nameof(_ExecutePlayerTeleportationForAllPlayersExceptMod), 3);

                        return;
                    }
                    case OpTeamRegionChange:
                    {
                        _console.Println(string.Format(ReceivedFormat, nameof(OpTeamRegionChange),
                            $"All inside region id of {_targetPlayerId} to team id of {_targetTeam}", _requestVersion));
                        if (_targetPlayerId < 0 || _targetPlayerId >= teamRegions.Length)
                        {
                            _console.Out.LogError(
                                $"{Prefix}Target region id is invalid! (_targetPlayerId < 0 || _targetPlayerId >= teamRegions.Length)");
                            return;
                        }

                        var players = playerManager.GetPlayers();
                        var region = teamRegions[_targetPlayerId];
                        var bounds = region.bounds;
                        foreach (var player in players)
                        {
                            if (!player || player.TeamId == _targetTeam)
                                continue;

                            var vrcPlayer = player.VrcPlayer;
                            if (vrcPlayer == null)
                                continue;

                            if (bounds.Contains(vrcPlayer.GetPosition()))
                                player.SetTeam(_targetTeam);
                        }

                        _console.Println(
                            $"{Prefix}Successfully finished process of {nameof(OpTeamRegionChange)}");

                        return;
                    }
                    default:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            $"Invalid:{_targetOperation}",
                            NewbieUtils.GetPlayerName(_targetPlayerId),
                            _requestVersion));
                        return;
                    }
                }
            }
        }

        #region SendGeneric
        private string SendGenericRequest(NewbieConsole console,
                                          int targetOp, string targetOpName,
                                          int targetPlayerId, string targetPlayerName,
                                          int targetTeam, bool requireMod)
        {
            if (requireMod && !console.IsSuperUser)
            {
                console.Println("<color=red>Cannot execute this unless you're moderator!</color>");
                return ConsoleLiteral.Of(false);
            }

            _targetOperation = targetOp;
            _targetTeam = targetTeam;
            _targetPlayerId = targetPlayerId;
            ++_requestVersion;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            console.Println(string.Format(RequestedFormat,
                targetOpName,
                targetPlayerName,
                _requestVersion));
            return ConsoleLiteral.Of(true);
        }

        private string SendPlayerRequest(NewbieConsole console, string[] arguments,
                                         int targetOp, string targetOpName,
                                         int targetTeam, bool requireModForTarget, bool requireModForExecute)
        {
            var target = Networking.LocalPlayer;
            if (arguments != null && arguments.Length >= 2)
            {
                if (requireModForTarget && !console.CurrentRole.HasPermission())
                {
                    console.Println("<color=red>Cannot specify player id unless you're moderator!</color>");
                    return ConsoleLiteral.Of(false);
                }

                target = ConsoleParser.TryParsePlayer(arguments[1]);
                if (target == null)
                {
                    console.Println("<color=red>Player id or name is invalid</color>");
                    return ConsoleLiteral.Of(false);
                }
            }

            return SendGenericRequest
            (
                console,
                targetOp, targetOpName,
                target.playerId, NewbieUtils.GetPlayerName(target),
                targetTeam, requireModForExecute
            );
        }

        private string SendAllRequest(NewbieConsole console, int targetOp, string targetOpName, bool requireMod)
        {
            return SendGenericRequest
            (
                console,
                targetOp, targetOpName,
                -1, "All",
                -1, requireMod
            );
        }

        private string SendBoolRequest(
            NewbieConsole console,
            string[] arguments,
            int targetOp, string targetOpName, string targetName,
            bool currentState, bool requireMod)
        {
            var hasValue = arguments.Length >= 2;
            if (!hasValue)
            {
                console.Println($"{targetName}: {currentState}");
                return ConsoleLiteral.Of(currentState);
            }

            if (!console.CurrentRole.HasPermission() && requireMod)
            {
                console.Println($"You are not allowed to change {targetName} unless you're moderator!");
                return ConsoleLiteral.GetNone();
            }

            var target = ConsoleParser.TryParseBoolean(arguments[1], currentState) ? 1 : 0;
            return SendGenericRequest(
                console,
                targetOp,
                targetOpName,
                -1,
                $"{targetName} to {target}",
                target,
                requireMod
            );
        }
        #endregion

        #region SubcommandHandler
        private string HandleDebug(NewbieConsole console, string label, string[] arguments)
        {
            if (arguments != null && arguments.Length >= 2)
            {
                var request = ConsoleParser.TryParseBoolean(arguments[1], playerManager.IsDebug);
                playerManager.IsDebug = request;
                if (request)
                    console.Println(
                        "<color=red>Warning: You must not enable this feature if you're currently playing.</color>\n" +
                        $"<color=red>   To disable this, simply type '{label} debug false' in console.</color>\n" +
                        "<color=red>警告: プレイ中はこの機能を有効化しないでください</color>\n" +
                        $"<color=red>    無効化するには '{label} debug false' と入力してください</color>\n");
            }

            console.Println($"IsPMDebug: {playerManager.IsDebug}");
            return ConsoleLiteral.Of(playerManager.IsDebug);
        }

        private string HandleRequestTeamShuffle(NewbieConsole console, string[] args)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to shuffle team unless you're moderator!");
                return ConsoleLiteral.Of(false);
            }

            // 2 bool compressed into int, in byte: ...00xy where y = include moderators, x = include green and blue.
            var shuffleMode = 0;
            if (args.Length == 2)
                shuffleMode = ConsoleParser.TryParseBoolAsInt(args[1], false);
            if (args.Length == 3)
                shuffleMode = ConsoleParser.TryParseBoolAsInt(args[1], false) +
                              ConsoleParser.TryParseBoolAsInt(args[2], false) * 2;

            return SendGenericRequest(
                console,
                OpTeamShuffle, nameof(OpTeamShuffle),
                -1,
                $"All({shuffleMode}){(_IsBitSet(shuffleMode, 1) ? " +include_moderators" : "")}{(_IsBitSet(shuffleMode, 2) ? " +include_greenAndBlue" : "")}",
                shuffleMode,
                true
            );
        }

        private string HandleRequestTeamRegionAdd(NewbieConsole console, string[] arguments)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to region add team unless you're moderator!");
                return ConsoleLiteral.Of(false);
            }

            if (arguments.Length <= 1)
            {
                console.Println(
                    "<color=red>Please specify region ID and team ID: <command> <regionId> <teamId></color>");
                return ConsoleLiteral.Of(false);
            }

            var regionId = ConsoleParser.TryParseInt(arguments[1]);
            var targetTeamId = ConsoleParser.TryParseInt(arguments[2]);

            if (regionId < 0 || regionId >= teamRegions.Length)
            {
                _console.Println($"<color=red>Target region ID is invalid: {regionId}</color>");
                return ConsoleLiteral.Of(false);
            }

            return SendGenericRequest(
                console,
                OpTeamRegionChange,
                nameof(OpTeamRegionChange),
                regionId,
                $"All inside region id of {regionId} to team id of {targetTeamId}",
                targetTeamId,
                true
            );
        }

        private string HandleList(NewbieConsole console, string[] arguments)
        {
            var players = playerManager.GetPlayers();
            const string format = "{0, -20}, {1, 7}, {2, 7}, {3, 7}, {4, 7}, {5, 20}\n";
            var list = "Player Instance Statuses:\n";
            list += string.Format(format, "Name", "IsDead", "Team", "KD", "Role", "Id:DisplayName");

            foreach (var player in players)
            {
                if (!player)
                {
                    list += string.Format(format, "null", "null", "null", "null", "null", "null");
                    continue;
                }

                var roleName = "";
                if (player.Roles != null)
                    foreach (var playerRole in player.Roles)
                        roleName += playerRole.RoleName;

                list += string.Format(
                    format,
                    player.name,
                    player.IsDead,
                    GetTeamNameByInt(player.TeamId),
                    $"{player.Kills}/{player.Deaths}",
                    roleName,
                    NewbieUtils.GetPlayerName(player.VrcPlayer)
                );
            }

            list += $"\nThere is <color=green>{players.Length}</color> active instances";

            console.Println(list);

            return list;
        }
        #endregion

        #region StaticMethod
        private static string GetTeamNameByInt(int teamId)
        {
            return teamId == 0 ? "non" :
                teamId == 1 ? "red" :
                teamId == 2 ? "yel" :
                teamId == 3 ? "gre" :
                teamId == 4 ? "blu" : $"?:{teamId}";
        }

        private static bool _IsBitSet(int n, int p)
        {
            return ((n >> (p - 1)) & 1) == 1;
        }
        #endregion

        #region TeamTeleportationLogics
        public void _ExecutePlayerTeleportationForAllPlayers()
        {
            _console.Println($"{Prefix}Teleporting All players to team position");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TeleportToTeamPositionAll));
        }

        public void _ExecutePlayerTeleportationForAllPlayersExceptMod()
        {
            _console.Println($"{Prefix}Teleporting All players except moderator to team position");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(TeleportToTeamPositionNonMods));
        }

        public void TeleportToTeamPositionAll()
        {
            _TeleportToTeamPosition(true);
        }

        public void TeleportToTeamPositionNonMods()
        {
            _TeleportToTeamPosition(false);
        }

        private void _TeleportToTeamPosition(bool includeModerators)
        {
            var local = playerManager.GetLocalPlayer();
            if (local == null)
            {
                _console.Println($"{Prefix}Ignoring teleport call because no local player found");
                return;
            }

            if (!includeModerators && local.Roles.HasPermission())
            {
                _console.Println($"{Prefix}Ignoring teleport call because I'm moderator");
                return;
            }

            if (teamPositions.Length == 0)
            {
                _console.Println($"{Prefix}Ignoring teleport call because no team positions were set");
                return;
            }

            var teleportDestId = local.TeamId;
            if (teleportDestId <= 0 || teleportDestId >= teamPositions.Length)
            {
                _console.Println(
                    $"{Prefix}Ignoring teleport call because there is no destination set for such team: {teleportDestId}");
                return;
            }

            _console.Println($"{Prefix}Teleporting to Team Position...");

            var teleportDestOrigin = teamPositions[teleportDestId];
            var teleportDest = teleportDestOrigin.position + teleportDestOrigin.forward;
            var lp = Networking.LocalPlayer;
            lp.TeleportTo(teleportDest, lp.GetRotation(),
                VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, false);
        }
        #endregion

        #region Operations
        private const int OpReset = -1;
        private const int OpAdd = 1;
        private const int OpRemove = 2;
        private const int OpAddAll = 3;
        private const int OpSync = 5;
        private const int OpSyncAll = 6;
        private const int OpStatsReset = 7;
        private const int OpStatsResetAll = 8;
        private const int OpFriendlyFireChange = 9;
        private const int OpTeamReset = 10;
        private const int OpTeamChange = 11;
        private const int OpTeamShuffle = 12;
        private const int OpTeamTagChange = 13;
        private const int OpStaffTagChange = 14;
        private const int OpTeamRegionChange = 15;
        private const int OpCreatorTagChange = 16;
        private const int OpFriendlyFireModeChange = 17;
        private const int OpRevive = 18;
        #endregion
    }
}
