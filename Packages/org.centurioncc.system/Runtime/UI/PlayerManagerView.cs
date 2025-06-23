using CenturionCC.System.Player;
using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI
{
    [DefaultExecutionOrder(40)] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerManagerView : PlayerManagerCallbackBase
    {
        [SerializeField]
        private NewbieConsole consoleInstance;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;

        public Button joinButton;
        public Button leaveButton;
        public Button resetButton;
        public Button updateButton;
        public Text infoText;

        [TextArea]
        public string youAreInGameMessage =
            "You're in game! index of {0}!";

        [TextArea]
        public string youAreNotInGameMessage =
            "You're not in game!";

        [TextArea]
        public string canNotRequestMessage =
            "Handling requests! Please wait...";

        public void Start()
        {
            joinButton.interactable = false;
            leaveButton.interactable = false;
            resetButton.interactable = false;
            updateButton.interactable = false;

            playerManager.Subscribe(this);
        }

        public void UpdateDisplay(bool hasLocalPlayer, int localPlayerIndex)
        {
            joinButton.interactable = !hasLocalPlayer;
            leaveButton.interactable = hasLocalPlayer;
            resetButton.interactable = consoleInstance.CurrentRole.HasPermission();
            updateButton.interactable = true;

            infoText.text = hasLocalPlayer
                ? string.Format(youAreInGameMessage, localPlayerIndex)
                : youAreNotInGameMessage;
        }

        #region ButtonEvents

        public void HandleJoinButton()
        {
            consoleInstance.Evaluate("player add");
        }

        public void HandleLeaveButton()
        {
            consoleInstance.Evaluate("player remove");
        }

        public void HandleResetButton()
        {
            consoleInstance.Evaluate("player reset");
        }

        public void HandleUpdateButton()
        {
            consoleInstance.Evaluate("player update");
        }

        #endregion
    }
}