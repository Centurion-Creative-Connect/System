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

        public override void Interact()
        {
            if (!IsActiveAndRunning) return;
            Progress = 1;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}