using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Command
{
    // TODO: fully integrate newbie console
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerManagerCommand : BoolCommandHandler
    {
        private const string Prefix = "[PlayerManagerCommand] ";
        private const string RequestedFormat = Prefix + "Requested to {0} player {1} with request version of {2}";
        private const string ReceivedFormat = Prefix + "Received {0} for {1} with request version of {2}";

        [SerializeField]
        private Transform[] teamPositions;
        [SerializeField]
        private Collider[] teamRegions;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

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
        public override string[] Aliases => new[] { "Player" };

        public override string Usage => "<command>\n" +
                                        "   reset\n" +
                                        "   add [playerId]\n" +
                                        "   remove [playerId]\n" +
                                        "   sync [shooterPlayerId]\n" +
                                        "   addAll\n" +
                                        "   syncAll\n" +
                                        "   stats [playerId]\n" +
                                        "   statsReset <playerId>\n" +
                                        "   statsResetAll\n" +
                                        "   teamReset\n" +
                                        "   team <team> [playerId]\n" +
                                        "   shuffleTeam [include moderators] [include green blue team]\n" +
                                        "   regionAdd <regionId> <teamId>\n" +
                                        "   showTeamTag [true|false]\n" +
                                        "   friendlyFire [true|false]\n" +
                                        "   disguise [true|false]\n" +
                                        "   roleSpecific [true|false]\n" +
                                        "   update\n" +
                                        "   localPlayer\n" +
                                        "   list [-non-joined]\n" +
                                        "   collider <collider name> [true|false]\n" +
                                        "   debug [true|false]\n";
        public override string Description => "Perform player related manipulation such as add/remove etc.";

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
                    case OpReset:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpReset),
                            "All",
                            _requestVersion));
                        playerManager.MasterOnly_ResetAllPlayer();
                        return;
                    }
                    case OpAdd:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpAdd),
                            NewbieUtils.GetPlayerName(_targetPlayerId),
                            _requestVersion));
                        playerManager.MasterOnly_AddPlayer(_targetPlayerId);
                        return;
                    }
                    case OpRemove:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpRemove),
                            NewbieUtils.GetPlayerName(_targetPlayerId),
                            _requestVersion));
                        playerManager.MasterOnly_RemovePlayer(_targetPlayerId);
                        return;
                    }
                    case OpAddAll:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpAddAll),
                            "All",
                            _requestVersion));
                        playerManager.MasterOnly_AddAllPlayer();
                        return;
                    }
                    case OpStatsReset:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpStatsReset),
                            NewbieUtils.GetPlayerName(_targetPlayerId),
                            _requestVersion));
                        playerManager.MasterOnly_ResetPlayerStats(_targetPlayerId);
                        return;
                    }
                    case OpStatsResetAll:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpStatsResetAll),
                            "All",
                            _requestVersion));
                        playerManager.MasterOnly_ResetAllPlayerStats();
                        return;
                    }
                    case OpFriendlyFireChange:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpFriendlyFireChange),
                            "All",
                            _requestVersion));
                        playerManager.MasterOnly_SetFriendlyFire(_targetTeam == 1);
                        return;
                    }
                    case OpTeamReset:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpTeamReset),
                            "All",
                            _requestVersion));
                        playerManager.MasterOnly_ResetTeam();
                        return;
                    }
                    case OpSync:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpSync),
                            $"BasePlayer:{_targetPlayerId}",
                            _requestVersion));
                        var targetPlayer = playerManager.GetPlayer(_targetPlayerId);
                        if (targetPlayer == null)
                        {
                            _console.LogError($"{Prefix}Specified player index is invalid");
                            return;
                        }

                        targetPlayer.Sync();
                        return;
                    }
                    case OpSyncAll:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpSyncAll),
                            "All",
                            _requestVersion));
                        playerManager.MasterOnly_SyncAllPlayer();
                        return;
                    }
                    case OpTeamChange:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpTeamChange),
                            $"{NewbieUtils.GetPlayerName(_targetPlayerId)} to {_targetTeam}",
                            _requestVersion));
                        var targetPlayer = playerManager.GetPlayerById(_targetPlayerId);
                        if (targetPlayer == null)
                        {
                            _console.LogError($"{Prefix}Could not find target player to change team!");
                            return;
                        }

                        playerManager.MasterOnly_SetTeam(targetPlayer.Index, _targetTeam);
                        return;
                    }
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
                            if (player == null || !player.IsAssigned ||
                                !includeMod && playerManager.IsStaffTeamId(player.TeamId))
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
                            playerManager.MasterOnly_SetTeam(trimmedActivePlayers[i].Index, team + 1);
                        }

                        if (trimmedActivePlayers.Length % 2 != 0)
                            playerManager.MasterOnly_SetTeam(
                                trimmedActivePlayers[trimmedActivePlayers.Length - 1].Index,
                                1);

                        _console.Println($"{Prefix}Shuffle Step 5: Schedule player teleportation");

                        SendCustomEventDelayedSeconds(
                            includeMod
                                ? nameof(_ExecutePlayerTeleportationForAllPlayers)
                                : nameof(_ExecutePlayerTeleportationForAllPlayersExceptMod), 3);

                        return;
                    }
                    case OpTeamTagChange:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpTeamTagChange),
                            $"All to {_targetTeam}",
                            _requestVersion));
                        playerManager.MasterOnly_SetTeamTagShown(_targetTeam == 1);
                        return;
                    }
                    case OpStaffTagChange:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpStaffTagChange),
                            $"All to {_targetTeam}",
                            _requestVersion));
                        playerManager.MasterOnly_SetStaffTagShown(_targetTeam == 1);
                        return;
                    }
                    case OpCreatorTagChange:
                    {
                        _console.Println(string.Format(ReceivedFormat,
                            nameof(OpCreatorTagChange),
                            $"All to {_targetTeam}",
                            _requestVersion));
                        playerManager.MasterOnly_SetCreatorTagShown(_targetTeam == 1);
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
                            if (player == null || !player.IsAssigned || player.TeamId == _targetTeam)
                                continue;

                            var vrcPlayer = player.VrcPlayer;
                            if (vrcPlayer == null)
                                continue;

                            if (bounds.Contains(vrcPlayer.GetPosition()))
                                playerManager.MasterOnly_SetTeam(player.Index, _targetTeam);
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

        private bool HandleLocalPlayer(NewbieConsole console)
        {
            var hasLocal = playerManager.HasLocalPlayer();
            var player = playerManager.GetLocalPlayer();
            var playerName =
                player != null ? NewbieUtils.GetPlayerName(player.VrcPlayer) : NewbieUtils.GetPlayerName(null);
            var index = playerManager.GetLocalPlayerIndex();

            console.Println(
                $"You are <color=green>{(hasLocal ? "in" : "not in")}</color> " +
                $"game with index of <color=green>{index}</color>, " +
                $"which is <color=green>{playerName}</color>!");
            return true;
        }

        private bool HandleDebug(NewbieConsole console, string label, string[] arguments)
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
            return true;
        }

        private bool HandleUpdate(NewbieConsole console)
        {
            playerManager.UpdateLocalPlayer();
            playerManager.UpdateAllPlayerView();

            console.Println(
                "<color=green>Successfully </color><color=orange>updated</color><color=green> local player</color>");
            return true;
        }

        private bool SendGenericRequest(NewbieConsole console,
            int targetOp, string targetOpName,
            int targetPlayerId, string targetPlayerName,
            int targetTeam, bool requireMod)
        {
            if (requireMod && !console.IsSuperUser)
            {
                console.Println("<color=red>Cannot execute this unless you're moderator!</color>");
                return true;
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
            return true;
        }

        private bool SendPlayerRequest(NewbieConsole console, string[] arguments,
            int targetOp, string targetOpName,
            int targetTeam, bool requireModForTarget, bool requireModForExecute)
        {
            var target = Networking.LocalPlayer;
            if (arguments != null && arguments.Length >= 2)
            {
                if (requireModForTarget && !console.CurrentRole.HasPermission())
                {
                    console.Println("<color=red>Cannot specify player id unless you're moderator!</color>");
                    return true;
                }

                target = ConsoleParser.TryParsePlayer(arguments[1]);
                if (target == null)
                {
                    console.Println("<color=red>Player id or name is invalid</color>");
                    return true;
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

        private bool SendAllRequest(NewbieConsole console, int targetOp, string targetOpName, bool requireMod)
        {
            return SendGenericRequest
            (
                console,
                targetOp, targetOpName,
                -1, "All",
                -1, requireMod
            );
        }

        private bool HandleRequestSync(NewbieConsole console, string[] arguments)
        {
            if (arguments == null || arguments.Length < 2)
            {
                console.Println(
                    $"<color=red>Please specify shooter player's index: 0 ~ {playerManager.GetMaxPlayerCount() - 1}");
                return true;
            }

            _targetOperation = OpSync;
            _targetPlayerId = ConsoleParser.TryParseInt(arguments[1]);
            _targetTeam = -1;
            ++_requestVersion;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            console.Println(string.Format(RequestedFormat,
                nameof(OpSync),
                $"ShooterPlayer:{_targetPlayerId}",
                _requestVersion));
            return true;
        }

        private bool HandleRequestTeamChange(NewbieConsole console, string[] arguments)
        {
            if (arguments == null || arguments.Length <= 1)
            {
                console.Println("<color=red>Usage: <command> team <team_id> [playerId]</color>");
                return false;
            }

            var targetPlayer = Networking.LocalPlayer;
            var targetTeam = ConsoleParser.TryParseInt(arguments[1]);
            if (targetTeam == -1)
            {
                console.Println("<color=red>Specified team is invalid</color>");
                return true;
            }

            if (arguments.Length >= 3 && console.CurrentRole.HasPermission())
            {
                targetPlayer = ConsoleParser.TryParsePlayer(arguments[2]);
                if (targetPlayer == null)
                {
                    console.Println("<color=red>Player id or name is invalid</color>");
                    return true;
                }
            }

            _targetOperation = OpTeamChange;
            _targetPlayerId = targetPlayer.playerId;
            _targetTeam = targetTeam;
            ++_requestVersion;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            console.Println(string.Format(RequestedFormat,
                nameof(OpTeamChange),
                $"{NewbieUtils.GetPlayerName(targetPlayer)} to {_targetTeam}",
                _requestVersion));
            return true;
        }

        private bool HandleRequestTeamShuffle(NewbieConsole console, string[] args)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to shuffle team unless you're moderator!");
                return true;
            }

            _targetOperation = OpTeamShuffle;
            _targetPlayerId = -1;
            // 2 bool compressed into int, in byte: ...00xy where y = include moderators, x = include green and blue.
            _targetTeam = 0;
            if (args.Length == 2)
                _targetTeam = ConsoleParser.TryParseBoolAsInt(args[1], false);
            if (args.Length == 3)
                _targetTeam = ConsoleParser.TryParseBoolAsInt(args[1], false) +
                              ConsoleParser.TryParseBoolAsInt(args[2], false) * 2;
            ++_requestVersion;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            console.Println(string.Format(RequestedFormat,
                nameof(OpTeamShuffle),
                $"All({_targetTeam}){(_IsBitSet(_targetTeam, 1) ? " +include_moderators" : "")}{(_IsBitSet(_targetTeam, 2) ? " +include_greenAndBlue" : "")}",
                _requestVersion));
            return true;
        }

        private bool HandleRequestTeamRegionAdd(NewbieConsole console, string[] arguments)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to region add team unless you're moderator!");
                return true;
            }

            if (arguments.Length <= 1)
            {
                console.Println(
                    "<color=red>Please specify region ID and team ID: <command> <regionId> <teamId></color>");
                return true;
            }

            _targetOperation = OpTeamRegionChange;
            // playerId == regionId
            _targetPlayerId = ConsoleParser.TryParseInt(arguments[1]);
            _targetTeam = ConsoleParser.TryParseInt(arguments[2]);

            if (_targetPlayerId < 0 || _targetPlayerId >= teamRegions.Length)
            {
                _console.Println(
                    $"<color=red>Target region ID is invalid: {_targetPlayerId}</color>");
                return true;
            }

            _requestVersion++;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            _console.Println(string.Format(RequestedFormat, nameof(OpTeamRegionChange),
                $"All inside region id of {_targetPlayerId} to team id of {_targetTeam}", _requestVersion));
            return true;
        }

        private bool HandleRequestShowTeamTag(NewbieConsole console, string[] arguments)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to change team tag on/off unless you're moderator!");
                return true;
            }

            if (arguments.Length <= 1)
            {
                console.Println("<color=red>On/Off not specified</color>");
                return true;
            }

            return SendGenericRequest
            (
                console,
                OpTeamTagChange, nameof(OpTeamTagChange),
                -1, $"All to {(ConsoleParser.TryParseBoolean(arguments[1], playerManager.ShowTeamTag) ? 1 : 0)}",
                _targetTeam = ConsoleParser.TryParseBoolean(arguments[1], playerManager.ShowTeamTag) ? 1 : 0, true
            );
        }

        private bool HandleRequestShowStaffTag(NewbieConsole console, string[] arguments)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to change staff tag on/off unless you're moderator!");
                return true;
            }

            if (arguments.Length <= 1)
            {
                console.Println("<color=red>On/Off not specified</color>");
                return true;
            }

            return SendGenericRequest
            (
                console,
                OpStaffTagChange, nameof(OpStaffTagChange),
                -1, $"All to {(ConsoleParser.TryParseBoolean(arguments[1], playerManager.ShowStaffTag) ? 1 : 0)}",
                _targetTeam = ConsoleParser.TryParseBoolean(arguments[1], playerManager.ShowStaffTag) ? 1 : 0, true
            );
        }

        private bool HandleRequestShowCreatorTag(NewbieConsole console, string[] arguments)
        {
            if (!console.CurrentRole.HasPermission())
            {
                console.Println("You are not allowed to change creator tag on/off unless you're moderator!");
                return true;
            }

            if (arguments.Length <= 1)
            {
                console.Println("<color=red>On/Off not specified</color>");
                return true;
            }

            return SendGenericRequest
            (
                console,
                OpCreatorTagChange, nameof(OpCreatorTagChange),
                -1, $"All to {(ConsoleParser.TryParseBoolean(arguments[1], playerManager.ShowCreatorTag) ? 1 : 0)}",
                _targetTeam = ConsoleParser.TryParseBoolean(arguments[1], playerManager.ShowCreatorTag) ? 1 : 0, true
            );
        }

        private bool HandleList(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2 && (arguments[1].Equals("-n") || arguments[1].Equals("-non-joined")))
                return HandleNonJoinedList(console);

            var shooterPlayers = playerManager.GetPlayers();
            const string format = "{0, -20}, {1, 7}, {2, 7}, {3, 7}, {4, 7}, {5, 20}";
            var activePlayerCount = 0;

            console.NewLine();
            console.Println("Player Instance Statuses:");
            console.Println(string.Format(format, "Name", "Active", "Team", "KD", "Role", "Id:DisplayName"));
            foreach (var shooterPlayer in shooterPlayers)
            {
                if (shooterPlayer == null)
                {
                    console.Println(string.Format(format, "null", "null", "null", "null", "null", "null"));
                    continue;
                }

                console.Println(
                    string.Format(format,
                        shooterPlayer.name,
                        shooterPlayer.IsAssigned,
                        GetTeamNameByInt(shooterPlayer.TeamId),
                        $"{shooterPlayer.Kills}/{shooterPlayer.Deaths}",
                        shooterPlayer.Role != null ? shooterPlayer.Role.RoleName : "NULL",
                        NewbieUtils.GetPlayerName(shooterPlayer.VrcPlayer)));
                if (shooterPlayer.IsAssigned)
                    ++activePlayerCount;
            }

            console.NewLine();
            console.Println(
                $"There is <color=green>{shooterPlayers.Length}</color> possible instances, <color=green>{activePlayerCount}</color> instances active, <color=green>{shooterPlayers.Length - activePlayerCount}</color> instances stored");
            return true;
        }

        private bool HandleNonJoinedList(NewbieConsole console)
        {
            const string format = "{0, -20}, {1, 7}, {2, 7}, {3, 7}";
            var nonJoinedCount = 0;
            var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            players = VRCPlayerApi.GetPlayers(players);
            console.NewLine();
            console.Println("Non-Joined Players:");
            console.Println(string.Format(format, "Id:DisplayName", "Master", "Local", "Mod"));

            foreach (var player in players)
            {
                if (playerManager.HasPlayerIdOf(player.playerId)) continue;
                console.Println(string.Format(format,
                    NewbieUtils.GetPlayerName(player),
                    player.isMaster,
                    player.isLocal,
                    playerManager.RoleManager.GetPlayerRole(player).HasPermission()));
                ++nonJoinedCount;
            }

            console.NewLine();
            console.Println($"There is <color=green>{nonJoinedCount}</color> non-joined players!");
            return true;
        }

        private bool HandleStats(NewbieConsole console, string[] args)
        {
            var player = playerManager.GetLocalPlayer();

            if (args != null && args.Length >= 2)
            {
                var vrcPlayer = ConsoleParser.TryParsePlayer(args[1]);
                if (vrcPlayer == null)
                {
                    console.Println("<color=red>Target player not found.</color>");
                    return true;
                }

                player = playerManager.GetPlayerById(vrcPlayer.playerId);
            }

            if (player == null)
            {
                console.Println("<color=red>Target player is not in game.</color>");
                return true;
            }

            console.Println(
                $"<color=orange><b>{NewbieUtils.GetPlayerName(player.VrcPlayer)}'s Stats</b></color>\n" +
                $"K: {player.Kills}\n" +
                $"D: {player.Deaths}\n" +
                $"KDR: {(player.Deaths != 0 && player.Deaths != 0 ? $"{player.Kills / player.Deaths:F1}" : "Infinity")}");
            return true;
        }

        private bool HandleCollider(NewbieConsole console, string[] arguments)
        {
            if (arguments.Length >= 2)
            {
                var targetName = arguments[1];
                if (arguments.Length >= 3)
                {
                    var targetStatus = ConsoleParser.TryParseBoolean(arguments[2],
                        GetColliderStatusByString(playerManager, targetName) == 1);
                    var result = SetColliderStatusByString(playerManager, targetName, targetStatus);
                    if (result == -1)
                    {
                        console.Println("Error: Collider name is invalid");
                        return true;
                    }
                }

                console.Println(
                    $"Collider {targetName}: {ColStatusToString(GetColliderStatusByString(playerManager, targetName))}");
                return true;
            }

            console.Println("ColliderInfo:\n" +
                            $"  useBase: {playerManager.UseBaseCollider}\n" +
                            $"  useAddi: {playerManager.UseAdditionalCollider}\n" +
                            $"  useLWCo: {playerManager.UseLightweightCollider}\n" +
                            $"  alwLWCo: {playerManager.AlwaysUseLightweightCollider}");
            return true;
        }

        private string ColStatusToString(int status)
        {
            return status == 0 ? "false" : status == 1 ? "true" : "invalid";
        }

        private int GetColliderStatusByString(PlayerManager playerManager, string str)
        {
            switch (str.ToLower())
            {
                case "base":
                    return playerManager.UseBaseCollider ? 1 : 0;
                case "body":
                case "add":
                case "addi":
                case "additional":
                    return playerManager.UseAdditionalCollider ? 1 : 0;
                case "lw":
                case "lwc":
                case "lightweight":
                case "lightweightcollider":
                    return playerManager.UseLightweightCollider ? 1 : 0;
                case "alw":
                case "alwc":
                case "alwayslightweight":
                case "alwayslightwightcollider":
                    return playerManager.AlwaysUseLightweightCollider ? 1 : 0;
                default:
                    return -1;
            }
        }

        private int SetColliderStatusByString(PlayerManager playerManager, string str, bool isEnabled)
        {
            // ReSharper disable AssignmentInConditionalExpression
            switch (str.ToLower())
            {
                case "base":
                    return (playerManager.UseBaseCollider = isEnabled) ? 1 : 0;
                case "body":
                case "add":
                case "addi":
                case "additional":
                    return (playerManager.UseAdditionalCollider = isEnabled) ? 1 : 0;
                case "lw":
                case "lwc":
                case "lightweight":
                case "lightweightcollider":
                    return (playerManager.UseLightweightCollider = isEnabled) ? 1 : 0;
                case "alw":
                case "alwc":
                case "alwayslightweight":
                case "alwayslightwightcollider":
                    return (playerManager.AlwaysUseLightweightCollider = isEnabled) ? 1 : 0;
                default:
                    return -1;
            }
            // ReSharper restore AssignmentInConditionalExpression
        }

        private string GetTeamNameByInt(int teamId)
        {
            return teamId == 0 ? "non" :
                teamId == 1 ? "red" :
                teamId == 2 ? "yel" :
                teamId == 3 ? "gre" :
                teamId == 4 ? "blu" : $"?:{teamId}";
        }

        private bool _IsBitSet(int n, int p)
        {
            return ((n >> (p - 1)) & 1) == 1;
        }

        public override bool OnBoolCommand(NewbieConsole console, string label, ref string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0)
            {
                console.PrintUsage(this);
                return false;
            }

            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "r":
                case "reset":
                    return SendAllRequest(console, OpReset, nameof(OpReset), true);
                case "join":
                case "a":
                case "add":
                    return SendPlayerRequest(console, vars, OpAdd, nameof(OpAdd), -1, true, false);
                case "leave":
                case "rm":
                case "remove":
                    return SendPlayerRequest(console, vars, OpRemove, nameof(OpRemove), -1, true, false);
                case "addall":
                    return SendAllRequest(console, OpAddAll, nameof(OpAddAll), true);
                case "teamreset":
                    return SendAllRequest(console, OpTeamReset, nameof(OpTeamReset), true);
                case "s":
                case "sync":
                    return HandleRequestSync(console, vars);
                case "syncall":
                    return SendAllRequest(console, OpSyncAll, nameof(OpSyncAll), false);
                case "st":
                case "stats":
                    return HandleStats(console, vars);
                case "streset":
                case "statsreset":
                    return SendPlayerRequest(console, vars, OpStatsReset, nameof(OpStatsReset), -1, true, true);
                case "stresetall":
                case "statsresetall":
                    return SendAllRequest(console, OpStatsResetAll, nameof(OpStatsResetAll), true);
                case "t":
                case "team":
                case "changeteam":
                    return HandleRequestTeamChange(console, vars);
                case "sh":
                case "shuffle":
                case "shuffleteam":
                    return HandleRequestTeamShuffle(console, vars);
                case "tra":
                case "regionadd":
                case "teamregionadd":
                    return HandleRequestTeamRegionAdd(console, vars);
                case "ttag":
                case "teamtag":
                case "showteamtag":
                    return HandleRequestShowTeamTag(console, vars);
                case "stag":
                case "stafftag":
                case "showstafftag":
                    return HandleRequestShowStaffTag(console, vars);
                case "ctag":
                case "creatortag":
                case "showcreatortag":
                    return HandleRequestShowCreatorTag(console, vars);
                case "ff":
                case "friendlyfire":
                case "allowfriendlyfire":
                    if (vars.Length >= 2)
                        return SendGenericRequest(console, OpFriendlyFireChange, nameof(OpFriendlyFireChange), -1,
                            "All", ConsoleParser.TryParseBoolAsInt(vars[1], playerManager.AllowFriendlyFire), true);
                    console.Println($"Allow Friendly Fire: {playerManager.AllowFriendlyFire}");
                    return playerManager.AllowFriendlyFire;
                case "u":
                case "update":
                    return HandleUpdate(console);
                case "lp":
                case "localplayer":
                    return HandleLocalPlayer(console);
                case "l":
                case "list":
                    return HandleList(console, vars);
                case "d":
                case "debug":
                    return HandleDebug(console, label, vars);
                case "c":
                case "collider":
                    return HandleCollider(console, vars);
                default:
                    console.PrintUsage(this);
                    return false;
            }
            // ReSharper restore StringLiteralTypo
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
        }

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

            if (!includeModerators && local.Role.HasPermission())
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
            var teleportDest = teleportDestOrigin.position + teleportDestOrigin.forward * local.Index;
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

        #endregion
    }
}