using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QuickGunCameraControllerUI : UdonSharpBehaviour
    {
        [Header("Base")]
        [SerializeField]
        private GunCamera instance;
        [Header("UI")]
        [SerializeField]
        private Toggle enableGunCamera;
        [SerializeField]
        private Button changeOffsetPos;
        [SerializeField]
        private Text changeOffsetPosText;
        [SerializeField]
        private Toggle showGunCamera;
        [SerializeField]
        private Toggle enableEditPosition;

        private void Start()
        {
            UpdateUI();
        }

        [PublicAPI]
        public void UpdateUI()
        {
            if (instance == null)
            {
                _SetInteractable(false, false);
                return;
            }

            enableGunCamera.isOn = instance.IsOn;
            showGunCamera.isOn = instance.IsVisible;
            enableEditPosition.isOn = instance.IsPickupable;

            _SetInteractable(instance.IsOn, instance.IsPickupable);

            changeOffsetPosText.text =
                $"プリセットの位置を変更する: {(instance.IsPickupable ? "CUSTOM" : $"{instance.OffsetIndex}")}";
        }

        [PublicAPI]
        public void ApplyChanges()
        {
            instance.IsOn = enableGunCamera.isOn;
            instance.IsVisible = showGunCamera.isOn;
            instance.IsPickupable = enableEditPosition.isOn;
            UpdateUI();
        }

        [PublicAPI]
        public void CycleCameraOffset()
        {
            ++instance.OffsetIndex;
            UpdateUI();
        }

        private void _SetInteractable(bool isInteractable, bool isPickupable)
        {
            changeOffsetPos.interactable = isInteractable && !isPickupable;
            showGunCamera.interactable = isInteractable;
            enableEditPosition.interactable = isInteractable;
        }
    }
}