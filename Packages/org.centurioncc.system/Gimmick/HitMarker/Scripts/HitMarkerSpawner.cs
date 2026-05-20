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

            PlayHitMarker(player.LastDamageInfo.HitPosition());
        }

        private void PlayHitMarker(Vector3 position)
        {
            if (particle)
            {
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = position;
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
