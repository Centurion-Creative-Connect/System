using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HitLogger : DamageDataSyncerManagerCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerMgr;
        [SerializeField] [HideInInspector] [NewbieInject]
        private DamageDataSyncerManager syncerMgr;
        [SerializeField] [HideInInspector] [NewbieInject]
        private LineManager lineManager;

        public bool onlyShooterDetection = true;
        public bool logOnKilled;
        public bool drawOnHitDetection;
        public bool drawOnKilled;
        public bool drawGlobally;
        public float linePersistTime = -1F;

        private void Start()
        {
            playerMgr.SubscribeCallback(this);
            syncerMgr.SubscribeCallback(this);
        }

        public override void OnSyncerReceived(DamageDataSyncer syncer)
        {
            if (drawOnHitDetection)
                DrawLine2(syncer.ActivatedPosition, syncer.HitPosition, Color.yellow);
        }

        public override void OnKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (logOnKilled)
                Debug.Log(
                    $"[HitLogger] OnKilled: {NewbieUtils.GetPlayerName(attacker.VrcPlayer)} " +
                    $"killed {NewbieUtils.GetPlayerName(victim.VrcPlayer)} " +
                    $"at {victim.LastHitData.HitTime:s}");

            if (drawOnKilled)
                DrawLine1(victim.LastHitData.ActivatedPosition, victim.LastHitData.HitPosition, Color.red);
        }

        private void DrawLine1(Vector3 v1, Vector3 v2, Color color)
        {
            const float arrowLen = 0.5F;
            const float lineWidth = 0.01F;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0F), new GradientColorKey(color, 1F) },
                new[] { new GradientAlphaKey(1F, 0F), new GradientAlphaKey(1F, 0F) }
            );

            lineManager.Draw(v1, v2, linePersistTime, lineWidth, lineWidth, gradient);

            var forward = v2 - v1;
            var a1 = Quaternion.LookRotation(forward) * Quaternion.Euler(0F, 170F, 0F) * Vector3.forward;
            var a2 = Quaternion.LookRotation(forward) * Quaternion.Euler(0F, 190F, 0F) * Vector3.forward;

            lineManager.Draw(v2, v2 + a1 * arrowLen, linePersistTime, lineWidth, lineWidth, gradient);
            lineManager.Draw(v2, v2 + a2 * arrowLen, linePersistTime, lineWidth, lineWidth, gradient);
        }

        private void DrawLine2(Vector3 v1, Vector3 v2, Color color)
        {
            const float arrowLen = 0.5F;
            const float lineWidth = 0.01F;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0F), new GradientColorKey(color, 1F) },
                new[] { new GradientAlphaKey(1F, 0F), new GradientAlphaKey(1F, 0F) }
            );

            lineManager.Draw(v1, v2, linePersistTime, lineWidth, lineWidth, gradient);

            var forward = v2 - v1;
            var a1 = Quaternion.LookRotation(forward) * Quaternion.Euler(170F, 0F, 0F) * Vector3.forward;
            var a2 = Quaternion.LookRotation(forward) * Quaternion.Euler(190F, 0F, 0F) * Vector3.forward;

            lineManager.Draw(v2, v2 + a1 * arrowLen, linePersistTime, lineWidth, lineWidth, gradient);
            lineManager.Draw(v2, v2 + a2 * arrowLen, linePersistTime, lineWidth, lineWidth, gradient);
        }
    }
}