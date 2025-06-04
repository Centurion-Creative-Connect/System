using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Objective
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FlagObjective : ObjectiveBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private AudioSource audioSource;

        [SerializeField]
        private float progressStep = 1.0F / 3.0F;

        [SerializeField]
        private float progressPersistDuration = 5.0F;

        [SerializeField]
        private bool ignoreEnemyInteraction = true;

        [SerializeField]
        private bool ignoreWhileAudioSourceIsPlaying = true;

        private float _lastProgressChangedTime;

        public override void Interact()
        {
            if (!IsActiveAndRunning) return;
            if (ignoreEnemyInteraction)
            {
                var player = playerManager.GetLocalPlayer();
                if (!player || player.TeamId != OwningTeamId) return;
            }

            if (ignoreWhileAudioSourceIsPlaying && audioSource.isPlaying) return;
            if (Time.timeSinceLevelLoad - _lastProgressChangedTime > progressPersistDuration)
                SetProgress(0);

            SetProgress(Progress + progressStep);
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        protected override void OnObjectiveProgress()
        {
            _lastProgressChangedTime = Time.timeSinceLevelLoad;
        }

        protected override void OnObjectiveCompleted()
        {
            audioSource.Play();
        }
    }
}