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
        private GameManager gameManager;

        public override string Label => "GameManager";
        public override string[] Aliases => new[] { "Game" };
        public override string Usage =>
            "<command> <canShoot|testHitRemote|testHitLocal|isMod|useHaptic|version|license>";
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
                    console.Println($"CanShoot: {gameManager.CanShoot()}");
                    return ConsoleLiteral.Of(gameManager.CanShoot());
                }
                case "testhitremote":
                {
                    console.Println("Playing onDeath for all players except local");
                    foreach (var player in gameManager.players.GetPlayers())
                        if (player != null && player.IsAssigned && !player.IsLocal)
                            player.OnDeath();
                    return ConsoleLiteral.GetNone();
                }
                case "testhitlocal":
                {
                    console.Println("Playing onDeath for local player if exist");
                    var localPlayer = gameManager.players.GetLocalPlayer();
                    if (localPlayer != null)
                        localPlayer.OnDeath();
                    return ConsoleLiteral.GetNone();
                }
                case "mod":
                case "moderator":
                case "ismod":
                case "ismoderator":
                {
                    console.Println($"IsModerator: {gameManager.roleProvider.GetPlayerRole().HasPermission()}");
                    return ConsoleLiteral.Of(gameManager.roleProvider.GetPlayerRole().HasPermission());
                }
                case "modmode":
                {
                    if (vars.Length >= 2 && console.CurrentRole.HasPermission())
                        gameManager.moderatorTool.IsModeratorMode =
                            ConsoleParser.TryParseBoolean(vars[1], gameManager.moderatorTool.IsModeratorMode);

                    console.Println($"ModMode: {gameManager.moderatorTool.IsModeratorMode}");
                    return ConsoleLiteral.Of(gameManager.moderatorTool.IsModeratorMode);
                }
                case "rc":
                case "arc":
                case "ricochetcount":
                case "allowedricochetcount":
                {
                    if (vars.Length >= 2 && console.CurrentRole.HasPermission())
                    {
                        gameManager.guns.allowedRicochetCount = ConsoleParser.TryParseInt(vars[1]);
                        Networking.SetOwner(Networking.LocalPlayer, gameManager.gameObject);
                        gameManager.RequestSerialization();
                    }

                    console.Println($"AllowedRicochetCount: {gameManager.guns.allowedRicochetCount}");
                    return ConsoleLiteral.Of(gameManager.guns.allowedRicochetCount);
                }
                case "haptic":
                case "usehaptic":
                {
                    if (vars.Length >= 2)
                        gameManager.hitEffect.useHaptic =
                            ConsoleParser.TryParseBoolean(vars[1], gameManager.hitEffect.useHaptic);

                    console.Println($"UseHaptic: {gameManager.hitEffect.useHaptic}");
                    return ConsoleLiteral.Of(gameManager.hitEffect.useHaptic);
                }
                case "version":
                {
                    console.Println($"Centurion System   - v{GameManager.GetVersion()}");
                    console.Println("Centurion System Commands - v0.4.1");
                    return GameManager.GetVersion();
                }
                case "license":
                {
                    console.Println(GameManager.GetLicense());
                    console.Println(
                        "Centurion System Commands © 2022 by Centurion Creative Connect is licensed under CC BY-NC 4.0");
                    return GameManager.GetLicense();
                }
                default:
                    return console.PrintUsage(this);
            }
        }
    }
}