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

        [SerializeField]
        private float[] autoPresetChangeIntervals;

        [SerializeField]
        private string[] autoPresetChangeIntervalsNames;

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

        [SerializeField]
        private Toggle enableAutoPresetChange;

        [SerializeField]
        private Button changeAutoPresetChangeInterval;

        [SerializeField]
        private Text changeAutoPresetChangeIntervalText;

        private int _autoPresetChangeIntervalIndex = 0;

        private int AutoPresetChangeIntervalIndex
        {
            set => _autoPresetChangeIntervalIndex = value % autoPresetChangeIntervals.Length;
            get => _autoPresetChangeIntervalIndex;
        }

        private void Start()
        {
            UpdateUI();
        }

        [PublicAPI]
        public void UpdateUI()
        {
            if (instance == null)
            {
                _SetInteractable(false, false, false);
                return;
            }

            enableGunCamera.isOn = instance.IsOn;
            showGunCamera.isOn = instance.IsVisible;
            enableEditPosition.isOn = instance.IsPickupable;
            enableAutoPresetChange.isOn = instance.UseAutoPresetChange;

            _SetInteractable(instance.IsOn, instance.IsPickupable, instance.UseAutoPresetChange);

            changeOffsetPosText.text =
                $"プリセットの位置を変更する: {(instance.IsPickupable ? "CUSTOM" : $"{instance.OffsetIndex}")}";

            var isPreset = Mathf.Approximately(
                autoPresetChangeIntervals[AutoPresetChangeIntervalIndex],
                instance.AutoPresetChangeInterval
            );

            changeAutoPresetChangeIntervalText.text = isPreset
                ? $"プリセット変更時間: {autoPresetChangeIntervalsNames[AutoPresetChangeIntervalIndex]}"
                : $"プリセット変更時間: CUSTOM({instance.AutoPresetChangeInterval:F1} secs)";
        }

        [PublicAPI]
        public void ApplyChanges()
        {
            instance.IsOn = enableGunCamera.isOn;
            instance.IsVisible = showGunCamera.isOn;
            instance.IsPickupable = enableEditPosition.isOn;
            instance.UseAutoPresetChange = enableAutoPresetChange.isOn;
            instance.AutoPresetChangeInterval = autoPresetChangeIntervals[AutoPresetChangeIntervalIndex];
            UpdateUI();
        }

        [PublicAPI]
        public void CycleCameraOffset()
        {
            ++instance.OffsetIndex;
            UpdateUI();
        }

        [PublicAPI]
        public void CyclePresetChangeInterval()
        {
            ++AutoPresetChangeIntervalIndex;
            instance.AutoPresetChangeInterval = autoPresetChangeIntervals[AutoPresetChangeIntervalIndex];
            UpdateUI();
        }

        private void _SetInteractable(bool isInteractable, bool isPickupable, bool useAutoPresetChange)
        {
            changeOffsetPos.interactable = isInteractable && !isPickupable;
            showGunCamera.interactable = isInteractable;
            enableEditPosition.interactable = isInteractable;
            enableAutoPresetChange.interactable = isInteractable;
            changeAutoPresetChangeInterval.interactable = isInteractable && useAutoPresetChange;
        }
    }
}