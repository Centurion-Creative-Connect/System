using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Utils.Watchdog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ShooterErrorHandler : WatchdogErrorCallbackBase
    {
        public const string ErrorMessageFormatEn = "An runtime error has occurred.\n" +
                                                   "Please re-join or create new instance.\n \n" +
                                                   "{0}\n \n" +
                                                   "If possible:\n" +
                                                   "Report this issue with screenshot and VRChat's log file\n \n" +
                                                   "Twitter: @VRSGF_Centurion\n" +
                                                   "VRChat : Nuinomi or DerpyNewbie";
        public const string ErrorMessageFormatJp = "エラーが発生しました\n" +
                                                   "インスタンスに入りなおすか, 立てなおしてください\n \n" +
                                                   "{0}\n \n" +
                                                   "スクリーンショットとVRChatのログを\n" +
                                                   "送ってもらえるとものすごく助かります!\n \n" +
                                                   "Twitter: @VRSGF_Centurion\n" +
                                                   "VRChat : Nuinomi or DerpyNewbie";
        public const string ErrorDescriptionFormat =
            "Error Code: {0}\n" +
            "Caused by : {1}\n" +
            "Message   : {2}\n" +
            "Timestamp : {3}";

        private const string Prefix = "[<color=maroon>WATCHDOG</color>-<color=yellow>callback</color>]";
        [HideInInspector]
        public int errorCode;
        [HideInInspector]
        public string errorCause = "Unknown";
        public Text errorHumorMessageDisplay;
        public Text errorMessageDisplayEn;
        public Text errorMessageDisplayJp;
        public GameObject[] inactiveAfterError;
        public GameObject[] activeAfterError;
        private readonly string[] _errorHumorMessages =
        {
            "I crashed. :(",
            "I will always be with you. - Crash Screen (2021 ~ 2022)",
            "Gimme that milk!",
            "I love these kind of messages appearing.",
            "Sugar Sokushinbutsu Nuinomi",
            "I want to blame someone else, but all I can think of is my own face.",
            "Pat me or else I will throw an exception at you." +
            "This is fine.",
            "(LCTRL + LSHIFT + Q, \"watchdog clear\", ENTER) Hey look there was no crashes!",
            "Do you know da wae?",
            "I will fix this in the summer"
        };
        private bool _hasProcInitialized;
        private WatchdogProc _initializedProc;


        private void Start()
        {
            Debug.Log($"{Prefix} callback init check started");
            ActivateErrorObject(true);
            SendCustomEventDelayedFrames(nameof(ProcInitCheck), 5);
        }

        public void ProcInitCheck()
        {
            if (_hasProcInitialized == false)
            {
                Debug.LogError($"{Prefix} proc init failed");
                errorCode = 10;
                errorCause = "ProcInitCheck failed (inner error)";
                Catch(null);
            }
        }


        private void Catch(WatchdogProc wd)
        {
            ActivateErrorObject(true);

            var errorDescription = string.Format(
                ErrorDescriptionFormat,
                errorCode,
                errorCause,
                GetErrorCodeDescriptionEn(errorCode, wd),
                GetTimestamp());
            if (errorHumorMessageDisplay)
                errorHumorMessageDisplay.text =
                    $"// {_errorHumorMessages[UnityEngine.Random.Range(0, _errorHumorMessages.Length - 1)]}";

            if (errorMessageDisplayEn)
                errorMessageDisplayEn.text =
                    string.Format(
                        ErrorMessageFormatEn,
                        errorDescription);
            if (errorMessageDisplayJp)
                errorMessageDisplayJp.text =
                    string.Format(
                        ErrorMessageFormatJp,
                        errorDescription);
        }

        private void ActivateErrorObject(bool isError)
        {
            foreach (var o in inactiveAfterError)
                if (o != null)
                    o.SetActive(!isError);

            foreach (var o in activeAfterError)
                if (o != null)
                    o.SetActive(isError);
        }

        public string GetErrorCodeDescriptionEn(int code, WatchdogProc wd)
        {
            // Watchdog inner error scope
            if (code >= 0 && code < 100)
                switch (code)
                {
                    case 0:
                        return "Unknown intercept or errorCode corrupted";
                    case 10:
                        return "Watchdog initialization failed (detected at callback)";
                }

            // Timeout error scope
            // Code follows as 1PPP: where P = behaviour index
            if (code >= 1000 && code < 2000)
            {
                var index = code - 1000;
                UdonSharpBehaviour o = null;
                if (wd != null && wd.behaviours.Length > index)
                    o = wd.behaviours[index];
                return o != null ? $"{o.gameObject.name} crashed" : $"Behaviour at {index} crashed";
            }

            // Child crash scope
            // Code follows as 2CCPP: where C = child index, P = parent index
            if (code >= 20000 && code < 30000)
            {
                var data = code - 20000; // to CCPP
                var childIndex = data / 100; // to CC
                var parentIndex = data - childIndex * 100; // to PP
                WatchdogCallbackBase parent = null;
                WatchdogChildCallbackBase child = null;
                if (wd != null)
                {
                    if (wd.behaviours.Length > parentIndex)
                        parent = (WatchdogCallbackBase)wd.behaviours[parentIndex];
                    if (parent != null && parent.GetChildren() != null && parent.GetChildren().Length > childIndex)
                        child = parent.GetChildren()[childIndex];
                }

                return
                    $"parent:{(parent != null ? parent.name : $"index of {parentIndex}")}'s child:{(child != null ? child.name : $"index of {childIndex}")} crashed";
            }

            if (code == 0xB11DEAD)
            {
                return "Gun couldn't get GunBullet from pool";
            }

            return "Unknown";
        }

        public string GetTimestamp()
        {
            return $"{Time.timeSinceLevelLoad:F2} {DateTime.Now:s}";
        }

        #region ProcCallback

        public override void OnException(WatchdogProc wd, int errCode)
        {
            errorCode = errCode;
            if (errorCode > 999 && errorCode < 2000)
            {
                errorCause = errorCode == 1000
                    ? "Watchdog_KeepAlive_Timeout(TimeoutByCrashTest)"
                    : "Watchdog_KeepAlive_Timeout";
                Debug.LogError(
                    $"{Prefix} An timeout occurred while watchdogProc sending keepalive: {errorCode}, {errorCause}");
            }
            else if (errorCode > 19999 && errorCode < 30000)
            {
                errorCause = errorCode == 20000
                    ? "Watchdog_KeepAlive_ChildCrash(ChildCrashByCrashTest)"
                    : "Watchdog_KeepAlive_ChildCrash";
                Debug.LogError(
                    $"{Prefix} An child crash detected while watchdogProc sending keepalive: {errorCode}, {errorCause}");
            }
            else
            {
                Debug.LogError(
                    $"{Prefix} An exception has thrown at watchdogProc: {errorCode}");
            }

            if (wd == null)
                Debug.LogWarning($"{Prefix} Caller is null!");

            Catch(wd == null ? _initializedProc : wd);
        }

        public override void OnInitialized(WatchdogProc wd)
        {
            _hasProcInitialized = true;
            if (wd != null)
                _initializedProc = wd;
            Debug.Log($"{Prefix} proc init complete");
            ActivateErrorObject(false);
            if (wd == null)
                Debug.LogWarning($"{Prefix} callback init with wd null!");
            Debug.Log($"{Prefix} callback init complete");
        }

        #endregion
    }
}