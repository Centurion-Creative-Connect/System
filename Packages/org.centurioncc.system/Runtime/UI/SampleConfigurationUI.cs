using CenturionCC.System.Player;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleConfigurationUI : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private NewbieConsole console;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        [SerializeField]
        private Toggle teamTagToggle;
        [SerializeField]
        private Toggle staffTagToggle;
        [SerializeField]
        private Dropdown friendlyFireDropdown;
        [SerializeField]
        private Toggle debugToggle;

        [SerializeField]
        private Button applyButton;

        private int _yieldStep;

        private void Start()
        {
            SendCustomEventDelayedSeconds(nameof(ReloadPanel), 10F);
        }

        public void OnReloadButtonPressed()
        {
            ReloadPanel();
        }

        public void OnApplyButtonPressed()
        {
            ApplyPanel();
        }

        private void ReloadPanel()
        {
            // TODO: dont reference PlayerManager directly, get from console command instead.
            teamTagToggle.isOn = playerManager.ShowTeamTag;
            staffTagToggle.isOn = playerManager.ShowStaffTag;
            friendlyFireDropdown.value = (int)playerManager.FriendlyFireMode;
            debugToggle.isOn = playerManager.IsDebug;
        }

        private void ApplyPanel()
        {
            applyButton.interactable = false;
            _yieldStep = 0;
            _Internal_YieldApplyStep();
        }

        public void _Internal_YieldApplyStep()
        {
            switch (_yieldStep)
            {
                case 0:
                    console.Evaluate($"PlayerManager showTeamTag {teamTagToggle.isOn}");
                    break;
                case 1:
                    console.Evaluate($"PlayerManager showStaffTag {staffTagToggle.isOn}");
                    break;
                case 2:
                    console.Evaluate(
                        $"PlayerManager friendlyFireMode {((FriendlyFireMode)friendlyFireDropdown.value).ToEnumName()}");
                    break;
                case 3:
                    console.Evaluate($"PlayerManager debug {debugToggle.isOn}");
                    break;
                default:
                    applyButton.interactable = true;
                    return;
            }

            ++_yieldStep;
            SendCustomEventDelayedSeconds(nameof(_Internal_YieldApplyStep), .5F);
        }
    }
}