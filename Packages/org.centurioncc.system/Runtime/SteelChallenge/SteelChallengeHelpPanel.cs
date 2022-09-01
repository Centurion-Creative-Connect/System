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

        private bool _hasPoppedUpAfterJoin;

        private UpdateManager _updateManager;

        private void Start()
        {
            _updateManager = GameManagerHelper.GetUpdateManager();
            _updateManager.SubscribeSlowFixedUpdate(this);
            helpPanel.SetActive(false);
        }

        public void _SlowFixedUpdate()
        {
            if (!Utilities.IsValid(Networking.LocalPlayer))
                return;

            if (_hasPoppedUpAfterJoin)
            {
                _updateManager.UnsubscribeSlowFixedUpdate(this);
                return;
            }

            if (Vector3.Distance(Networking.LocalPlayer.GetPosition(), helpPanel.transform.position) < 5F)
            {
                PopUp();
                _updateManager.UnsubscribeSlowFixedUpdate(this);
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