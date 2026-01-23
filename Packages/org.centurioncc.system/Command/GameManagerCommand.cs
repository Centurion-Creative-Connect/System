using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GameManagerCommand : NewbieConsoleCommandHandler
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private CenturionSystem gameManager;

        public override string Label => "GameManager";
        public override string[] Aliases => new[] { "Game" };

        public override string Usage =>
            "<command> <canShoot|isMod|useHaptic|version|license>";

        public override string Description =>
            "Adjusts some game settings or prints out version/license of this project.";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return ConsoleLiteral.GetNone();
            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "canshoot":
                {
                    var locallyHeldGuns = gameManager.guns.GetLocallyHeldGunInstances();
                    if (locallyHeldGuns.Length == 0)
                    {
                        console.Println("No guns are held locally");
                        return ConsoleLiteral.GetFalse();
                    }

                    var canShoot = gameManager.guns.CanShoot(locallyHeldGuns[0], out var ruleId);
                    console.Println($"CanShoot: {canShoot}, {ruleId}");
                    return ConsoleLiteral.Of(canShoot);
                }
                case "mod":
                case "moderator":
                case "ismod":
                case "ismoderator":
                {
                    console.Println($"IsModerator: {gameManager.roleProvider.GetPlayerRole().HasPermission()}");
                    return ConsoleLiteral.Of(gameManager.roleProvider.GetPlayerRole().HasPermission());
                }

                case "rc":
                case "arc":
                case "ricochetcount":
                case "allowedricochetcount":
                {
                    if (vars.Length >= 2 && console.CurrentRole.HasPermission())
                    {
                        gameManager.guns.AllowedRicochetCount = ConsoleParser.TryParseInt(vars[1]);
                        Networking.SetOwner(Networking.LocalPlayer, gameManager.gameObject);
                        gameManager.RequestSerialization();
                    }

                    console.Println($"AllowedRicochetCount: {gameManager.guns.AllowedRicochetCount}");
                    return ConsoleLiteral.Of(gameManager.guns.AllowedRicochetCount);
                }
                case "modmode":
                case "haptic":
                case "usehaptic":
                {
                    console.Println("no-op");
                    return "no-op";
                }
                case "version":
                {
                    console.Println($"Centurion System   - v{gameManager.GetVersion()}");
                    console.Println("Centurion System Commands - v0.7.0-alpha.1");
                    return gameManager.GetVersion();
                }
                case "license":
                {
                    console.Println(gameManager.GetLicense());
                    console.Println(
                        "Centurion System Commands © 2022 by Centurion Creative Connect is licensed under CC BY-NC 4.0");
                    return gameManager.GetLicense();
                }
                default:
                    return console.PrintUsage(this);
            }
        }
    }
}
