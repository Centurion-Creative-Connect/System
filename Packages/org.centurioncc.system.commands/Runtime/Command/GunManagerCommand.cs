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
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        private NewbieConsole _console;

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
            "<command> <reset|slowReset|reload|trail|optimizationRange|rePickupDelay|collisionCheck|debug|summon|list|info>";
        public override string Description => "Perform gun related manipulation such as summon/reset/list etc.";

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
                var variantData = gunManager.GetVariantData(_targetVariantId);
                gunManager.MasterOnly_SpawnWithData(variantData, destPos, destRot);
            }
        }

        private string HandleList(NewbieConsole console, string[] args)
        {
            string msg;

            if (args.Length < 2)
                msg = string.Join
                (
                    "\n",
                    GetVariantList(gunManager),
                    GetRemoteList(gunManager)
                );
            else
                switch (args[1])
                {
                    case "variant":
                        msg = GetVariantList(gunManager);
                        break;
                    case "instance":
                        msg = GetRemoteList(gunManager);
                        break;
                    default:
                        msg = string.Join
                        (
                            "\n",
                            GetVariantList(gunManager),
                            GetRemoteList(gunManager)
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
                gunManager.MasterOnly_SlowlyResetRemoteGuns();
            }
        }

        public void RequestFastReset()
        {
            if (Networking.IsMaster && _console)
            {
                _console.Println("[GunManagerCommand] Received fast reset request");
                gunManager.MasterOnly_ResetRemoteGuns();
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
                    gunManager.ReloadGuns();
                    console.Println("<color=green>Reload complete</color>");
                    return ConsoleLiteral.GetNone();
                case "trail":
                    if (vars.Length >= 2)
                        gunManager.useDebugBulletTrail =
                            ConsoleParser.TryParseBoolean(vars[1], gunManager.useDebugBulletTrail);

                    console.Println($"UseTrail: {gunManager.useDebugBulletTrail}");
                    return ConsoleLiteral.Of(gunManager.useDebugBulletTrail);
                case "or":
                case "optimization":
                case "optimizationrange":
                    if (vars.Length >= 2)
                        gunManager.optimizationRange = ConsoleParser.TryParseFloat(vars[1]);

                    console.Println($"OptimizationRange: {gunManager.optimizationRange}");
                    return ConsoleLiteral.Of(gunManager.optimizationRange);
                case "arc":
                case "ricochetcount":
                case "allowedricochetcount":
                    if (vars.Length >= 2)
                    {
                        gunManager.allowedRicochetCount = ConsoleParser.TryParseInt(vars[1]);
                        Networking.SetOwner(Networking.LocalPlayer, gunManager.gameObject);
                        gunManager.RequestSerialization();
                    }

                    console.Println($"AllowedRicochetCount: {gunManager.allowedRicochetCount}");
                    return ConsoleLiteral.Of(gunManager.allowedRicochetCount);
                case "rep":
                case "repickup":
                case "repickupdelay":
                    if (vars.Length >= 2)
                        gunManager.handleRePickupDelay = ConsoleParser.TryParseFloat(vars[1]);

                    console.Println($"RePickupDelay: {gunManager.handleRePickupDelay}");
                    return ConsoleLiteral.Of(gunManager.handleRePickupDelay);
                case "cc":
                case "collision":
                case "collisioncheck":
                    if (vars.Length >= 2)
                        gunManager.useCollisionCheck =
                            ConsoleParser.TryParseBoolean(vars[1], gunManager.useCollisionCheck);

                    console.Println($"CollisionCheck: {gunManager.useCollisionCheck}");
                    return ConsoleLiteral.Of(gunManager.useCollisionCheck);
                case "debug":
                    if (vars.Length >= 2)
                        gunManager.IsDebugGunHandleVisible =
                            ConsoleParser.TryParseBoolean(vars[1], gunManager.IsDebugGunHandleVisible);

                    console.Println($"Debug: {gunManager.IsDebugGunHandleVisible}");
                    return ConsoleLiteral.Of(gunManager.IsDebugGunHandleVisible);
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
                case "i":
                case "info":
                    if (vars.Length < 2)
                    {
                        console.Println("<color=red>Error: Syntax error</color>\n" +
                                        "<color=red>Please specify variant id</color>");
                        return ConsoleLiteral.GetNone();
                    }

                    var index = ConsoleParser.TryParseByte(vars[1]);
                    var variantData = gunManager.GetVariantData(index);

                    if (variantData == null)
                    {
                        console.Println("<color=red>Error: Could not retrieve vairant data</color>");
                        return ConsoleLiteral.GetNone();
                    }

                    // Base variant data information
                    var result =
                        $"UniqueId      : {variantData.UniqueId}\n" +
                        $"WeaponName    : {variantData.WeaponName}\n" +
                        $"HolsterSize   : {variantData.HolsterSize}\n" +
                        $"IsDoubleHanded: {variantData.IsDoubleHanded}\n" +
                        $"MaxRPS/MaxRPM : {variantData.MaxRoundsPerSecond}/{variantData.MaxRoundsPerSecond * 60}";

                    // Append ProjectileData information if provided
                    if (variantData.ProjectileData != null)
                    {
                        variantData.ProjectileData.Get(
                            0,
                            out var positionOffset,
                            out var velocity,
                            out var rotOffset,
                            out var torque,
                            out var drag,
                            out var trailDuration,
                            out var trailCol);

                        result +=
                            "\nProjectileData:\n" +
                            $"  Spd: {velocity.ToString("F2")}\n" +
                            $"  Drg: {drag}\n" +
                            $"  Tor: {torque.ToString("F2")}\n" +
                            $"  POf: {positionOffset.ToString("F2")}\n" +
                            $"  ROf: {rotOffset.eulerAngles.ToString("F2")}\n" +
                            $"  tDr: {trailDuration}\n" +
                            $"  tCl: {trailCol.Evaluate(0).ToString()}";
                    }

                    console.Println(result);

                    return result;
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