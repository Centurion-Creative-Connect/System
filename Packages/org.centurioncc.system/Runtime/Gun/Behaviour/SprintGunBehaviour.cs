using CenturionCC.System.UI;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using System;
using UnityEngine;
namespace CenturionCC.System.Gun.Behaviour
{
    public class SprintGunBehaviour : GunBehaviourBase
    {
        [Header("Sprint Settings")]
        [SerializeField]
        private float sprintSpeedMultiplier = 2F;
        [SerializeField]
        private float sprintDurationInSeconds = 5F;
        [SerializeField]
        private float sprintCooldownInSeconds = 10F;
        [SerializeField]
        private KeyCode desktopSprintKey = KeyCode.Q;
        [Header("Sprint Messages")]
        [SerializeField]
        private bool showMessages = true;
        [SerializeField] [Range(2, 10)]
        private float messageDuration = 2F;
        [SerializeField]
        private TranslatableMessage startingSprint;
        [SerializeField]
        private TranslatableMessage endingSprint;
        [SerializeField]
        private TranslatableMessage sprintInCooldown;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerController playerController;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notificationProvider;

        private DateTime _lastSprintDateTime = DateTime.MinValue;
        private bool _wasInSprint;

        public override void OnAction(GunBase instance)
        {
            TryStartSprint();
        }

        public override void OnGunUpdate(GunBase instance)
        {
            if (Input.GetKeyDown(desktopSprintKey))
            {
                TryStartSprint();
            }
        }

        public override void OnGunDrop(GunBase instance)
        {
            EndSprint();
        }

        public void TryStartSprint()
        {
            var fullCooldown = sprintDurationInSeconds + sprintCooldownInSeconds;
            if (DateTime.Now.Subtract(_lastSprintDateTime).TotalSeconds < fullCooldown)
            {
                var cooldownDuration = fullCooldown - DateTime.Now.Subtract(_lastSprintDateTime).TotalSeconds;
                if (showMessages && sprintInCooldown != null)
                    notificationProvider.ShowHelp(string.Format(sprintInCooldown.Message, cooldownDuration),
                        messageDuration, 1683812782);
                return;
            }

            StartSprint();
        }

        public void StartSprint()
        {
            if (showMessages && startingSprint != null)
                notificationProvider.ShowInfo(startingSprint.Message, duration: messageDuration);

            _lastSprintDateTime = DateTime.Now;
            _wasInSprint = true;
            playerController.CustomEffectMultiplier = sprintSpeedMultiplier;
            playerController.UpdateLocalVrcPlayer();
            SendCustomEventDelayedSeconds(nameof(EndSprint), sprintDurationInSeconds);
        }

        public void EndSprint()
        {
            if (showMessages && endingSprint != null && _wasInSprint)
                notificationProvider.ShowInfo(endingSprint.Message, duration: messageDuration);

            playerController.CustomEffectMultiplier = 1F;
            playerController.UpdateLocalVrcPlayer();
            _wasInSprint = false;
        }
    }
}
