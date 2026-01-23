using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Command.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunManagerCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        public override string Label => "GunManager";
        public override string[] Aliases => new[] { "Gun" };

        public override string Usage =>
            "<command> <reset|slowReset|reload|trail|optimizationRange|rePickupDelay|collisionCheck|debug|summon|list|info>";

        public override string Description => "Perform gun related manipulation such as summon/reset/list etc.";


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

        private static string GetVariantList(GunManagerBase gunManager)
        {
            var variants = gunManager.GetVariantDataInstances();
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

        private static string GetRemoteList(GunManagerBase gunManager)
        {
            var managedGuns = gunManager.GetGunInstances();
            //                         Name,  Local,   Pick,  Holst,  VarId,    User,     Pos,  State,FireMode, Trigger,   Shots, IDH
            const string format = "{0, -16}, {1, 5}, {2, 5}, {3, 5}, {4, 5}, {5, 20}, {6, 20}, {7, 8}, {8, 8}, {9, 8}, {10, 5}, {11, 5}";
            var activeCount = 0;
            var result = "Gun Statuses:";

            result = string.Join
            (
                "\n",
                result,
                string.Format
                (
                    format,
                    "Name",
                    "Local",
                    "Pick",
                    "Holst",
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
                            m.IsPickedUp,
                            m.IsHolstered,
                            m.VariantData != null ? $"{m.VariantData.UniqueId}" : "null",
                            NewbieUtils.GetPlayerName(m.CurrentHolder),
                            m.transform.position.ToString("F2"),
                            m.State.GetStateString(),
                            m.FireMode.GetStateString(),
                            m.Trigger.GetStateString(),
                            m.ShotCount,
                            m.CanBeTwoHanded
                        )
                    );

                    if (m.VariantData != null)
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

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return console.PrintUsage(this);
            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "r":
                case "reset":
                case "sr":
                case "slowreset":
                    if (vars.ContainsString("-unused"))
                    {
                        console.Println("Requesting unused reset to master");
                        gunManager._RequestResetAll(GunManagerResetType.Unused);
                        return ConsoleLiteral.GetNone();
                    }

                    console.Println("Requesting reset to master");
                    gunManager._RequestResetAll(GunManagerResetType.All);
                    return ConsoleLiteral.GetNone();
                case "reload":
                case "refresh":
                    console.Println("Requesting refresh to master");
                    gunManager._RequestRefresh();
                    return ConsoleLiteral.GetNone();
                case "trail":
                    if (vars.Length >= 2)
                        gunManager.UseDebugBulletTrail = ConsoleParser.TryParseBoolean(vars[1], gunManager.UseDebugBulletTrail);

                    console.Println($"UseTrail: {gunManager.UseDebugBulletTrail}");
                    return ConsoleLiteral.Of(gunManager.UseDebugBulletTrail);
                case "arc":
                case "ricochetcount":
                case "allowedricochetcount":
                    if (vars.Length >= 2)
                    {
                        gunManager.AllowedRicochetCount = ConsoleParser.TryParseInt(vars[1]);
                        Networking.SetOwner(Networking.LocalPlayer, gunManager.gameObject);
                        gunManager.RequestSerialization();
                    }

                    console.Println($"AllowedRicochetCount: {gunManager.AllowedRicochetCount}");
                    return ConsoleLiteral.Of(gunManager.AllowedRicochetCount);
                case "cc":
                case "collision":
                case "collisioncheck":
                    if (vars.Length >= 2)
                        gunManager.UseCollisionCheck = ConsoleParser.TryParseBoolean(vars[1], gunManager.UseCollisionCheck);

                    console.Println($"CollisionCheck: {gunManager.UseCollisionCheck}");
                    return ConsoleLiteral.Of(gunManager.UseCollisionCheck);
                case "debug":
                    if (vars.Length >= 2)
                        gunManager.IsDebugGunHandleVisible = ConsoleParser.TryParseBoolean(vars[1], gunManager.IsDebugGunHandleVisible);

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

                    var targetVariantId = ConsoleParser.TryParseByte(vars[1]);
                    var targetRot = Networking.LocalPlayer.GetRotation();
                    var targetPos = Networking.LocalPlayer.GetPosition() + Vector3.up + (targetRot * Vector3.forward);
                    gunManager._RequestSpawn(targetVariantId, targetPos, targetRot);

                    console.Println(
                        $"Requested to summon gun as {targetVariantId} in front of you!");
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
                        $"IsDoubleHanded: {variantData.IsDoubleHanded}\n";

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
                            out var damageAmount,
                            out var trailDuration,
                            out var trailCol,
                            out var lifeTime);

                        result +=
                            "\nProjectileData:\n" +
                            $"  Spd: {velocity.ToString("F2")}\n" +
                            $"  Dmg: {damageAmount}\n" +
                            $"  Drg: {drag}\n" +
                            $"  Tor: {torque.ToString("F2")}\n" +
                            $"  POf: {positionOffset.ToString("F2")}\n" +
                            $"  ROf: {rotOffset.eulerAngles.ToString("F2")}\n" +
                            $"  tDr: {trailDuration}\n" +
                            $"  tCl: {trailCol.Evaluate(0).ToString()}" +
                            $"  lt : {lifeTime:F2}";
                    }

                    console.Println(result);

                    return result;
                default:
                    return console.PrintUsage(this);
                // ReSharper restore StringLiteralTypo
            }
        }
    }
}
