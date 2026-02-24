using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.UserControlPanel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UserControlPanel : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private GunController gunController;
        [SerializeField]
        private VRActionTypeDropdown fireModeChangeDropdown;
        [SerializeField]
        private VRActionTypeDropdown reloadChangeDropdown;

        private void Start()
        {
            if (fireModeChangeDropdown) fireModeChangeDropdown.SetCallback(this, "OnFireModeChange");
            if (reloadChangeDropdown) reloadChangeDropdown.SetCallback(this, "OnReloadChange");

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (fireModeChangeDropdown) fireModeChangeDropdown.SetValueWithoutNotify((int)gunController.FireModeAction);
            if (reloadChangeDropdown) reloadChangeDropdown.SetValueWithoutNotify((int)gunController.ReloadAction);
        }

        public void OnFireModeChange()
        {
            gunController.FireModeAction = (VRActionType)fireModeChangeDropdown.GetValue();
        }

        public void OnReloadChange()
        {
            gunController.ReloadAction = (VRActionType)reloadChangeDropdown.GetValue();
        }
    }
}
