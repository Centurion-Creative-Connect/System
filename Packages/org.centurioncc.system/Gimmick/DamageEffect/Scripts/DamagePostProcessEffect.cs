using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
namespace CenturionCC.System.Gimmick.DamageEffect
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DamagePostProcessEffect : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;
        [SerializeField]
        private PostProcessVolume damageVolume;
        [SerializeField]
        private PostProcessVolume deadVolume;

        [SerializeField] private float damageAdditionalWeight = 0.2f;
        [SerializeField] private float weightLerpSpeed = 10f;

        private bool _isDead;
        private float _weightTarget;

        private void Update()
        {
            if (!damageVolume)
            {
                return;
            }

            var t = 1 - Mathf.Exp(-weightLerpSpeed * Time.deltaTime);
            damageVolume.weight = Mathf.Lerp(damageVolume.weight, _weightTarget, t);
        }

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
            if (!player.IsLocal) return;

            _weightTarget = 1 - (player.Health / player.MaxHealth);
            _isDead = player.IsDead;

            if (deadVolume)
            {
                deadVolume.weight = _isDead ? 1 : 0;
            }

            // do not apply damage effect boost if health has increased
            if (player.Health > previousHealth)
            {
                return;
            }

            if (damageVolume)
            {
                damageVolume.weight = _isDead ? 0 : Mathf.Clamp01(_weightTarget + damageAdditionalWeight);
            }
        }
    }
}
