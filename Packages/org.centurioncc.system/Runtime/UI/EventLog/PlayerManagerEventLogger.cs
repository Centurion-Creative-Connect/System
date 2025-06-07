using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.UI.EventLog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerManagerEventLogger : PlayerManagerCallbackBase
    {
        private const string Prefix = "P";

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private SystemEventLogger systemEventLogger;

        private void Start()
        {
            playerManager.Subscribe(this);
        }

        public override void OnPlayerHealthChanged(PlayerBase player, float previousHealth)
        {
            if (player.Health < previousHealth)
                systemEventLogger.AppendLog(Prefix, "Damage",
                    $"{player.DisplayName} {previousHealth} → {player.Health} <color=red>({previousHealth - player.Health} dmg)</color>");
            else
                systemEventLogger.AppendLog(Prefix, "Heal",
                    $"{player.DisplayName} {previousHealth} → {player.Health} <color=green>({player.Health - previousHealth} heal)</color>");
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            var damageInfo = type != KillType.ReverseFriendlyFire ? victim.LastDamageInfo : attacker.LastDamageInfo;
            var distance = Vector3.Distance(damageInfo.OriginatedPosition(), damageInfo.HitPosition());
            systemEventLogger.AppendLog(Prefix, "Kill",
                $"{attacker.DisplayName} ─({damageInfo.DamageType().Replace("BBBullet: ", "")})─＞ {victim.DisplayName} ({distance:F1}m){(type != KillType.Default ? $" (<color=red>{type.ToEnumName()}</color>)" : "")}");
        }

        public override void OnPlayerRevived(PlayerBase player)
        {
            systemEventLogger.AppendLog(Prefix, "Revive", $"{player.DisplayName}");
        }
    }
}