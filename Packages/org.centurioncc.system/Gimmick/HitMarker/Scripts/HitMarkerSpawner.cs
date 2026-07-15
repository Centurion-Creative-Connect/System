using CenturionCC.System.Audio;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.HitMarker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HitMarkerSpawner : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;
        [SerializeField] [NewbieInject]
        private AudioManager audioManager;
        [SerializeField]
        private ParticleSystem particle;
        [SerializeField]
        private AudioDataStore audioData;
        [SerializeField]
        private Color normalHitColor = Color.white;
        [SerializeField]
        private Color criticalHitColor = Color.red;

        public Color NormalHitColor { get => normalHitColor; set => normalHitColor = value; }
        public Color CriticalHitColor { get => criticalHitColor; set => criticalHitColor = value; }

        private void OnEnable()
        {
            playerManager.Subscribe(this);
        }

        private void OnDisable()
        {
            playerManager.Unsubscribe(this);
        }

        public override void OnPlayerHealthChanged(PlayerBase player, float previousHealth)
        {
            if (player.Health > previousHealth) return;
            if (player.LastDamageInfo.AttackerId() != Networking.LocalPlayer.playerId) return;

            var parts = player.LastDamageInfo.HitParts();
            PlayHitMarker(player.LastDamageInfo.HitPosition(), parts == BodyParts.Head ? CriticalHitColor : NormalHitColor);
        }

        private void PlayHitMarker(Vector3 position, Color color)
        {
            if (particle)
            {
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = position;
                emitParams.startColor = color;
                emitParams.applyShapeToPosition = false;

                particle.Emit(emitParams, 1);
            }

            if (audioData)
            {
                audioManager.PlayAudioAtPosition(audioData, Networking.LocalPlayer.GetPosition());
            }
        }
    }
}
