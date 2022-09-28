using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunManagerCommand : NewbieConsoleCommandHandler
    {
        private NewbieConsole _console;
        private GunManager _gunManager;

        private int _lastRequestVersion;
        [UdonSynced]
        private int _requestVersion;

        [UdonSynced]
        private int _targetPlayerId;
        [UdonSynced]
        private byte _targetVariantId;

        public override string Label => "GunManager";
        public override string[] Aliases => new[] { "Gun" };
        public override string Usage =>
            "<command> <reset|slowReset|reload|trail|optimizationRange|rePickupDelay|collisionCheck|debug|summon|list>";

        private void Start()
        {
            _gunManager = GameManagerHelper.GetGunManager();
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
            if (Networking.IsMaster && _lastRequestVersion < _requestVersion)
            {
                _lastRequestVersion = _requestVersion;
                var player = VRCPlayerApi.GetPlayerById(_targetPlayerId);
                if (player == null || !player.IsValid())
                {
                    _console.LogError(
                        "[GunManagerCommand] Could not process request: Target player id is invalid!\n" +
                        $"Target PlayerId: {_targetPlayerId}, Target Variant Id: {_targetVariantId}");
                    return;
                }

                var destPos = player.GetRotation() * new Vector3(0F, 0.7F, 1F) + player.GetPosition();
                var destRot = Quaternion.identity;
                var variantData = _gunManager.GetVariantData(_targetVariantId);
                _gunManager.MasterOnly_SpawnWithData(variantData, destPos, destRot);
            }
        }

        private string HandleList(NewbieConsole console, string[] args)
        {
            string msg;

            if (args.Length < 2)
                msg = string.Join
                (
                    "\n",
                    GetVariantList(_gunManager),
                    GetRemoteList(_gunManager)
                );
            else
                switch (args[1])
                {
                    case "variant":
                        msg = GetVariantList(_gunManager);
                        break;
                    case "instance":
                        msg = GetRemoteList(_gunManager);
                        break;
                    default:
                        msg = string.Join
                        (
                            "\n",
                            GetVariantList(_gunManager),
                            GetRemoteList(_gunManager)
                        );
                        break;
                }

            console.Println(msg);
            return msg;
        }

        private static string GetVariantList(GunManager gunManager)
        {
            var variants = gunManager.VariantDataInstances;
            const string format = "{0, -7}, {1, 20}";
            var result = "Available Variants:";

            result = string.Join
            (
                "\n",
                result,
                string.Format(format, "Id", "Name")
            );

            foreach (var variant in variants)
                if (variant != null)
                    result = string.Join("\n", result, string.Format(format, variant.UniqueId, variant.name));
                else
                    result += "\nnull";

            result += $"\nThere is <color=green>{variants.Length}</color> variants";

            return result;
        }

        private static string GetRemoteList(GunManager gunManager)
        {
            var managedGuns = gunManager.ManagedGunInstances;
            //    Name   , Local , Occ,    Pic,  Holst,    Opt,  VarId,    User,     Pos,  State, FireMode, Trigger, Shots, IDH
            const string format =
                "{0, -16}, {1, 5}, {2, 5}, {3, 5}, {4, 5}, {5, 5}, {6, 5}, {7, 20}, {8, 20}, {9, 8}, {10, 8}, {11, 8}, {12, 5}, {13, 5}";
            var activeCount = 0;
            var result = "Remote Gun Statuses:";

            result = string.Join
            (
                "\n",
                result,
                string.Format
                (
                    format,
                    "Name",
                    "Local",
                    "Occ",
                    "Pick",
                    "Holst",
                    "Opt",
                    "VarId",
                    "User",
                    "Pos",
                    "State",
                    "FireMode",
                    "Trigger",
                    "Shots",
                    "IDH"
                )
            );

            foreach (var m in managedGuns)
            {
                if (m != null)
                {
                    result = string.Join
                    (
                        "\n",
                        result,
                        string.Format
                        (
                            format,
                            m.name,
                            m.IsLocal,
                            m.IsOccupied,
                            m.IsPickedUp,
                            m.IsHolstered,
                            m.IsOptimized,
                            m.VariantDataUniqueId,
                            NewbieUtils.GetPlayerName(m.CurrentHolder),
                            m.transform.position.ToString("F2"),
                            m.State.GetStateString(),
                            m.FireMode.GetStateString(),
                            m.Trigger.GetStateString(),
                            m.ShotCount,
                            m.IsDoubleHandedGun
                        )
                    );

                    if (m.IsOccupied)
                        ++activeCount;
                }
                else
                {
                    result += "\nnull";
                }
            }

            result += $"\nThere is <color=green>{managedGuns.Length}</color> managed instances, " +
                      $"<color=green>{activeCount}</color> instances active, " +
                      $"<color=green>{managedGuns.Length - activeCount}</color> instances stored";

            return result;
        }

        public void RequestSlowReset()
        {
            if (Networking.IsMaster && _console)
            {
                _console.Println("[GunManagerCommand] Received slow reset request");
                _gunManager.MasterOnly_SlowlyResetRemoteGuns();
            }
        }

        public void RequestFastReset()
        {
            if (Networking.IsMaster && _console)
            {
                _console.Println("[GunManagerCommand] Received fast reset request");
                _gunManager.MasterOnly_ResetRemoteGuns();
            }
        }

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return console.PrintUsage(this);
            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "r":
                case "reset":
                    console.Println("Requesting fast reset to master");
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RequestFastReset));
                    return ConsoleLiteral.GetNone();
                case "sr":
                case "slowreset":
                    console.Println("Requesting slow reset to master");
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RequestSlowReset));
                    return ConsoleLiteral.GetNone();
                case "reload":
                    _gunManager.ReloadGuns();
                    console.Println("<color=green>Reload complete</color>");
                    return ConsoleLiteral.GetNone();
                case "trail":
                    if (vars.Length >= 2)
                        _gunManager.UseDebugBulletTrail =
                            ConsoleParser.TryParseBoolean(vars[1], _gunManager.UseDebugBulletTrail);

                    console.Println($"UseTrail: {_gunManager.UseDebugBulletTrail}");
                    return ConsoleLiteral.Of(_gunManager.UseDebugBulletTrail);
                case "or":
                case "optimization":
                case "optimizationrange":
                    if (vars.Length >= 2)
                        _gunManager.OptimizationRange = ConsoleParser.TryParseFloat(vars[1]);

                    console.Println($"OptimizationRange: {_gunManager.OptimizationRange}");
                    return ConsoleLiteral.Of(_gunManager.OptimizationRange);
                case "arc":
                case "ricochetcount":
                case "allowedricochetcount":
                    if (vars.Length >= 2)
                    {
                        _gunManager.AllowedRicochetCount = ConsoleParser.TryParseInt(vars[1]);
                        Networking.SetOwner(Networking.LocalPlayer, _gunManager.gameObject);
                        _gunManager.RequestSerialization();
                    }

                    console.Println($"AllowedRicochetCount: {_gunManager.AllowedRicochetCount}");
                    return ConsoleLiteral.Of(_gunManager.AllowedRicochetCount);
                case "rep":
                case "repickup":
                case "repickupdelay":
                    if (vars.Length >= 2)
                        _gunManager.HandleRePickupDelay = ConsoleParser.TryParseFloat(vars[1]);

                    console.Println($"RePickupDelay: {_gunManager.HandleRePickupDelay}");
                    return ConsoleLiteral.Of(_gunManager.HandleRePickupDelay);
                case "cc":
                case "collision":
                case "collisioncheck":
                    if (vars.Length >= 2)
                        _gunManager.UseCollisionCheck =
                            ConsoleParser.TryParseBoolean(vars[1], _gunManager.UseCollisionCheck);

                    console.Println($"CollisionCheck: {_gunManager.UseCollisionCheck}");
                    return ConsoleLiteral.Of(_gunManager.UseCollisionCheck);
                case "debug":
                    if (vars.Length >= 2)
                        _gunManager.IsDebugGunHandleVisible =
                            ConsoleParser.TryParseBoolean(vars[1], _gunManager.IsDebugGunHandleVisible);

                    console.Println($"Debug: {_gunManager.IsDebugGunHandleVisible}");
                    return ConsoleLiteral.Of(_gunManager.IsDebugGunHandleVisible);
                case "su":
                case "spawn":
                case "summon":
                    if (vars.Length < 2)
                    {
                        console.Println("<color=red>Error: Syntax error</color>\n" +
                                        "<color=red>Please specify variant id</color>");
                        return ConsoleLiteral.GetNone();
                    }

                    _targetVariantId = ConsoleParser.TryParseByte(vars[1]);
                    _targetPlayerId = Networking.LocalPlayer.playerId;
                    ++_requestVersion;
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                    RequestSerialization();
                    console.Println(
                        $"Requested to summon gun as {_targetVariantId} in front of {NewbieUtils.GetPlayerName(_targetPlayerId)}!");
                    return ConsoleLiteral.GetNone();
                case "l":
                case "list":
                    return HandleList(console, vars);
                default:
                    return console.PrintUsage(this);
                // ReSharper restore StringLiteralTypo
            }
        }

        public override void OnRegistered(NewbieConsole console)
        {
            _console = console;
        }
    }
}