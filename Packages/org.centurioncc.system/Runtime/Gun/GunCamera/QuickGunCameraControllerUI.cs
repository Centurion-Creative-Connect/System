using DerpyNewbie.Common;
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QuickGunCameraControllerUI : UdonSharpBehaviour
    {
        [Header("Base")]
        [SerializeField] [NewbieInject]
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
        private TMP_Text changeOffsetPosTextTmp;

        [SerializeField]
        private TMP_Dropdown changeTargetDropdown;

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

        [SerializeField]
        private TMP_Text changeAutoPresetChangeIntervalTextTmp;

        private int _autoPresetChangeIntervalIndex;
        private bool _isUpdatingUI;

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
            if (!instance)
            {
                return;
            }

            _isUpdatingUI = true;

            enableGunCamera.isOn = instance.IsOn;
            showGunCamera.isOn = instance.IsVisible;
            enableEditPosition.isOn = instance.IsPickupable;
            enableAutoPresetChange.isOn = instance.UseAutoPresetChange;

            var changeOffsetPosMsg = $"プリセットの位置を変更する: {(instance.IsPickupable ? "CUSTOM" : $"{instance.OffsetIndex}")}";
            if (changeOffsetPosText) changeOffsetPosText.text = changeOffsetPosMsg;
            if (changeOffsetPosTextTmp) changeOffsetPosTextTmp.text = changeOffsetPosMsg;

            var isPreset = Mathf.Approximately(
                autoPresetChangeIntervals[AutoPresetChangeIntervalIndex],
                instance.AutoPresetChangeInterval
            );

            var changeAutoPresetChangeIntervalMsg = isPreset
                ? $"プリセット変更時間: {autoPresetChangeIntervalsNames[AutoPresetChangeIntervalIndex]}"
                : $"プリセット変更時間: CUSTOM({instance.AutoPresetChangeInterval:F1} secs)";
            if (changeAutoPresetChangeIntervalText)
                changeAutoPresetChangeIntervalText.text = changeAutoPresetChangeIntervalMsg;
            if (changeAutoPresetChangeIntervalTextTmp)
                changeAutoPresetChangeIntervalTextTmp.text = changeAutoPresetChangeIntervalMsg;

            changeTargetDropdown.value = instance.CustomTargetIndex;
            changeTargetDropdown.RefreshShownValue();

            _isUpdatingUI = false;
        }

        [PublicAPI]
        public void ApplyChanges()
        {
            if (_isUpdatingUI) return;

            instance.CustomTargetIndex = changeTargetDropdown.value;
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
    }
}