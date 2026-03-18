using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
namespace CenturionCC.System.Gimmick.UserControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UserControlPanel : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private GunController gunController;
        [SerializeField]
        private Slider zRotationOffsetSlider;
        [SerializeField]
        private VRActionTypeDropdown fireModeChangeDropdown;
        [SerializeField]
        private VRActionTypeDropdown reloadChangeDropdown;

        [Header("Default")]
        [SerializeField]
        private VRActionType defaultReloadAction = VRActionType.InputJump | VRActionType.GunDirectionDown;
        [SerializeField]
        private VRActionType defaultFireModeAction = VRActionType.InputJump;
        [SerializeField]
        private float defaultZRotationOffset = 0;

        private bool _isUpdatingDisplay;

        private void Start()
        {
            if (fireModeChangeDropdown) fireModeChangeDropdown.SetCallback(this, "OnFireModeChange");
            if (reloadChangeDropdown) reloadChangeDropdown.SetCallback(this, "OnReloadChange");

            UpdateDisplay();
        }

        private void OnMouseEnter()
        {
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_isUpdatingDisplay) return;
            _isUpdatingDisplay = true;

            if (zRotationOffsetSlider) zRotationOffsetSlider.value = gunController.CurrentPrimaryXOffset;
            if (fireModeChangeDropdown) fireModeChangeDropdown.SetValueWithoutNotify((int)gunController.FireModeAction);
            if (reloadChangeDropdown) reloadChangeDropdown.SetValueWithoutNotify((int)gunController.ReloadAction);

            _isUpdatingDisplay = false;
        }

        public void OnPrimaryXOffsetChange()
        {
            gunController.CurrentPrimaryXOffset = zRotationOffsetSlider.value;
        }

        public void OnFireModeChange()
        {
            gunController.FireModeAction = (VRActionType)fireModeChangeDropdown.GetValue();
        }

        public void OnReloadChange()
        {
            gunController.ReloadAction = (VRActionType)reloadChangeDropdown.GetValue();
        }

        public void ResetToDefault()
        {
            gunController.CurrentPrimaryXOffset = defaultZRotationOffset;
            gunController.ReloadAction = defaultReloadAction;
            gunController.FireModeAction = defaultFireModeAction;

            UpdateDisplay();
        }
    }
}
