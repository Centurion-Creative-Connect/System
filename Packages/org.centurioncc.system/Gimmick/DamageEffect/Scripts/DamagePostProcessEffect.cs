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
        [Tooltip("If enabled, the weight will decay over specified time.")]
        [SerializeField] private bool doDecay = true;
        [Tooltip("The decay delay is the time in seconds to wait before the weight starts to decay.")]
        [SerializeField] private float decayDelaySecs = 3f;
        [SerializeField] private float decayLerpSpeed = 1f;
        [Tooltip("The decay rate is the multiplier of weights to subtract. current weight * decay rate = new weight")] [Range(0, 1)]
        [SerializeField] private float decayRate = 1f;
        private float _decayTargetTime;

        private bool _isDead;
        private float _lerpSpeed;
        private float _weightTarget;

        private void Update()
        {
            if (!damageVolume)
            {
                return;
            }

            if (doDecay && Time.time > _decayTargetTime)
            {
                _weightTarget = Mathf.Clamp01(_weightTarget - _weightTarget * decayRate);
                _lerpSpeed = decayLerpSpeed;
                _decayTargetTime = float.PositiveInfinity;
            }

            var t = 1 - Mathf.Exp(-_lerpSpeed * Time.deltaTime);
            damageVolume.weight = Mathf.Lerp(damageVolume.weight, _weightTarget, t);
        }

        private void OnEnable()
        {
            _lerpSpeed = weightLerpSpeed;
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
            _lerpSpeed = weightLerpSpeed;
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

            if (doDecay)
            {
                _decayTargetTime = Time.time + decayDelaySecs;
            }
        }
    }
}
