using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace CenturionCC.System.Gimmick.ResetStatusDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ResetStatusDisplay : GunManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField]
        private double resetDonePeriodInMinutes = 3D;

        [SerializeField]
        private float statusUpdateFrequencyInSeconds = 10F;

        [SerializeField]
        private Image resetStatusImage;

        [SerializeField]
        private Text lastResetText;

        [SerializeField]
        private TMP_Text lastResetTextTmp;

        [SerializeField]
        private TranslatableMessage translatableLastResetMessage;

        [FormerlySerializedAs("lastResetMessage")]
        [SerializeField]
        private string fallbackLastResetMessage = "{0}以上前にリセットしました!";

        [SerializeField]
        private Sprite resetNotYet;

        [SerializeField]
        private Sprite resetDone;

        private bool _currentResetStatusImage;

        private DateTime _lastResetTime = DateTime.MinValue;

        private void Start()
        {
            gunManager.SubscribeCallback(this);
            _RecursiveUpdateResetStatusText();
        }


        public void _RecursiveUpdateResetStatusText()
        {
            UpdateResetStatusText();
            SendCustomEventDelayedSeconds(nameof(_RecursiveUpdateResetStatusText), statusUpdateFrequencyInSeconds);
        }

        public override void OnGunsReset()
        {
            _lastResetTime = DateTime.Now;
            UpdateResetStatusText();
        }

        private static string JapaneseTimeText(TimeSpan diff)
        {
            return diff.TotalSeconds < 60 ? diff.ToString(@"s' 秒'") :
                diff.TotalMinutes < 60 ? diff.ToString(@"m' 分'") :
                diff.TotalHours < 24 ? diff.ToString(@"h' 時間'") :
                "1日";
        }

        private static string EnglishTimeText(TimeSpan diff)
        {
            return diff.TotalSeconds < 60 ? diff.ToString(@"s' second(s)'") :
                diff.TotalMinutes < 60 ? diff.ToString(@"m' minute(s)'") :
                diff.TotalHours < 24 ? diff.ToString(@"h' hour(s)'") :
                "a day";
        }

        private void UpdateResetStatusText()
        {
            var diff = DateTime.Now.Subtract(_lastResetTime);

            var timeText = NewbieUtils.IsJapaneseTimeZone() ? JapaneseTimeText(diff) : EnglishTimeText(diff);
            var msg = string.Format(
                translatableLastResetMessage
                    ? translatableLastResetMessage.Message
                    : fallbackLastResetMessage, timeText
            );

            if (lastResetText) lastResetText.text = msg;
            if (lastResetTextTmp) lastResetTextTmp.text = msg;

            if (resetStatusImage)
            {
                if (diff.TotalMinutes > resetDonePeriodInMinutes && _currentResetStatusImage)
                {
                    _currentResetStatusImage = false;
                    resetStatusImage.sprite = resetNotYet;
                }
                else if (diff.TotalMinutes < resetDonePeriodInMinutes && !_currentResetStatusImage)
                {
                    _currentResetStatusImage = true;
                    resetStatusImage.sprite = resetDone;
                }
            }
        }
    }
}
