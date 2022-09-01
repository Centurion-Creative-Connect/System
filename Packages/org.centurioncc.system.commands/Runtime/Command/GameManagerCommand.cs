using CenturionCC.System.Utils;
using DerpyNewbie.Logger;
using UdonSharp;
using VRC.SDKBase;

namespace CenturionCC.System.Command
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GameManagerCommand : NewbieConsoleCommandHandler
    {
        private GameManager _gameManager;

        private void Start()
        {
            _gameManager = GameManagerHelper.GetGameManager();
        }

        public override string Label => "GameManager";
        public override string[] Aliases => new[] { "Game" };
        public override string Usage =>
            "<command> <canShoot|hitLocal|hitRemote|isMod|useHaptic|version|license>";

        public override string OnCommand(NewbieConsole console, string label, string[] vars, ref string[] envVars)
        {
            if (vars == null || vars.Length == 0) return ConsoleLiteral.GetNone();
            // ReSharper disable StringLiteralTypo
            switch (vars[0].ToLower())
            {
                case "canshoot":
                {
                    console.Println($"CanShoot: {_gameManager.CanShoot()}");
                    return ConsoleLiteral.Of(_gameManager.CanShoot());
                }
                case "hitlocal":
                case "testhitlocal":
                {
                    console.Println("Playing local hit as dummy");
                    _gameManager.PlayHitLocal(null);
                    return ConsoleLiteral.GetNone();
                }
                case "hitremote":
                case "testhitremote":
                {
                    console.Println("Playing remote hit");
                    foreach (var player in _gameManager.players.GetPlayers())
                        _gameManager.PlayHitRemote(player);
                    return ConsoleLiteral.GetNone();
                }
                case "mod":
                case "moderator":
                case "ismod":
                case "ismoderator":
                {
                    console.Println($"IsModerator: {_gameManager.roleProvider.GetPlayerRole().HasPermission()}");
                    return ConsoleLiteral.Of(_gameManager.roleProvider.GetPlayerRole().HasPermission());
                }
                case "modmode":
                {
                    if (vars.Length >= 2 && console.CurrentRole.HasPermission())
                        _gameManager.moderatorTool.IsModeratorMode =
                            ConsoleParser.TryParseBoolean(vars[1], _gameManager.moderatorTool.IsModeratorMode);

                    console.Println($"ModMode: {_gameManager.moderatorTool.IsModeratorMode}");
                    return ConsoleLiteral.Of(_gameManager.moderatorTool.IsModeratorMode);
                }
                case "rc":
                case "arc":
                case "ricochetcount":
                case "allowedricochetcount":
                {
                    if (vars.Length >= 2 && console.CurrentRole.HasPermission())
                    {
                        _gameManager.guns.AllowedRicochetCount = ConsoleParser.TryParseInt(vars[1]);
                        Networking.SetOwner(Networking.LocalPlayer, _gameManager.gameObject);
                        _gameManager.RequestSerialization();
                    }

                    console.Println($"AllowedRicochetCount: {_gameManager.guns.AllowedRicochetCount}");
                    return ConsoleLiteral.Of(_gameManager.guns.AllowedRicochetCount);
                }
                case "haptic":
                case "usehaptic":
                {
                    if (vars.Length >= 2)
                        _gameManager.hitEffect.useHaptic =
                            ConsoleParser.TryParseBoolean(vars[1], _gameManager.hitEffect.useHaptic);

                    console.Println($"UseHaptic: {_gameManager.hitEffect.useHaptic}");
                    return ConsoleLiteral.Of(_gameManager.hitEffect.useHaptic);
                }
                case "version":
                {
                    console.Println($"Centurion System - v{GameManager.GetVersion()}");
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