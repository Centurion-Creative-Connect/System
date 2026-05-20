using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
namespace CenturionCC.System.Gimmick.HealthDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HealthBarDisplay : PlayerManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;
        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private PlayerBase targetPlayer;

        [Header("Text")]
        [SerializeField]
        private Text text;
        [SerializeField]
        private string format = "{0:0} / {1:0}";

        [Header("Slider")]
        [SerializeField]
        private Slider slider;
        [SerializeField]
        private Image sliderImage;
        [SerializeField]
        private Gradient sliderImageGradient = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, .5f),
                new GradientColorKey(Color.green, 1f),
            },
        };

        [SerializeField]
        private float minHealth = 2.5f;

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
            if (!targetPlayer && !player.IsLocal) return;
            if (targetPlayer && targetPlayer != player) return;

            UpdateDisplay(player.Health, player.MaxHealth);
        }

        private void UpdateDisplay(float currentHealth, float maxHealth)
        {
            if (slider)
            {
                var normalizedHealth = currentHealth / maxHealth;
                slider.value = Mathf.Clamp(normalizedHealth * slider.maxValue, currentHealth > 0 ? minHealth : 0, slider.maxValue);
                if (sliderImage) sliderImage.color = sliderImageGradient.Evaluate(normalizedHealth);
            }

            if (text)
            {
                text.text = string.Format(format, currentHealth <= 0 ? 0 : currentHealth, maxHealth);
            }
        }
    }
}
