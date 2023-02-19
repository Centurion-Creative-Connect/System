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

        private bool _hasPoppedUpAfterJoin;

        private UpdateManager _updateManager;

        private void Start()
        {
            _updateManager = CenturionSystemReference.GetUpdateManager();

            // Don't subscribe to update events if not desired (for perf reasons)
            if (!doPopUpForNewPlayer) return;

            _updateManager.SubscribeSlowUpdate(this);
            helpPanel.SetActive(false);
        }

        public void _SlowUpdate()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
                return;

            if (_hasPoppedUpAfterJoin)
            {
                _updateManager.UnsubscribeSlowUpdate(this);
                return;
            }

            if (Vector3.Distance(Networking.LocalPlayer.GetPosition(), helpPanel.transform.position) < 5F)
            {
                PopUp();
                _updateManager.UnsubscribeSlowUpdate(this);
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