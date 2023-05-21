using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.SteelChallenge
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SteelChallengeHelpPanel : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject helpPanel;
        [SerializeField]
        private bool doPopUpForNewPlayer = true;

        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        private bool _hasPoppedUpAfterJoin;

        private void Start()
        {
            // Don't subscribe to update events if not desired (for perf reasons)
            if (!doPopUpForNewPlayer) return;

            updateManager.SubscribeSlowUpdate(this);
            helpPanel.SetActive(false);
        }

        public void _SlowUpdate()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
                return;

            if (_hasPoppedUpAfterJoin)
            {
                updateManager.UnsubscribeSlowUpdate(this);
                return;
            }

            if (Vector3.Distance(Networking.LocalPlayer.GetPosition(), helpPanel.transform.position) < 5F)
            {
                PopUp();
                updateManager.UnsubscribeSlowUpdate(this);
            }
        }

        [PublicAPI]
        public void PopUp()
        {
            helpPanel.SetActive(true);
            _hasPoppedUpAfterJoin = true;
        }

        [PublicAPI]
        public void Close()
        {
            helpPanel.SetActive(false);
        }
    }
}