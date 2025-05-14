using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Objective
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FlagObjective : ObjectiveBase
    {
        [SerializeField] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private AudioSource audioSource;

        private float _lastPlayedTime;

        public override void Interact()
        {
            if (!IsActiveAndRunning) return;
        }

        public override void OnObjectiveStart()
        {
        }

        public override void OnObjectiveEnd()
        {
        }
    }
}