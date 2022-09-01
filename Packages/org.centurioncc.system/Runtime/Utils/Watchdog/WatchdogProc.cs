using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils.Watchdog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WatchdogProc : UdonSharpBehaviour
    {
        private const string Prefix = "[<color=maroon>WATCHDOG</color>-<color=teal>proc</color>]";
        public bool hasCrashed;
        public float timeout = 12F;
        public float tick = 4F;
        public int chunk = 5;
        public int childChunk = 5;
        public UdonSharpBehaviour[] behaviours;
        public WatchdogErrorCallbackBase errorCallback;
        public bool writeVerboseLog;

        private WatchdogCallbackBase[] _callbackBases;
        private int[] _lastChildIndexes;
        private int _lastKeepAliveIndex;
        private float _tickTimer;

        private void Start()
        {
            Debug.Log($"{Prefix} proc init start");
            var callbacks = new WatchdogCallbackBase[behaviours.Length];
            for (var i = 0; i < behaviours.Length; i++)
                callbacks[i] = (WatchdogCallbackBase)behaviours[i];
            _callbackBases = callbacks;
            _lastChildIndexes = new int[behaviours.Length];

            SendCustomEventDelayedFrames(nameof(LateStart), 1);
        }

        private void FixedUpdate()
        {
            if (hasCrashed) return;

            _tickTimer += Time.deltaTime;
            if (tick < _tickTimer)
            {
                SendKeepAliveTick();
                _tickTimer = 0F;
            }
        }

        public void LateStart()
        {
            Debug.Log($"{Prefix} proc init complete");
            errorCallback.OnInitialized(this);
        }

        private void SendKeepAliveTick()
        {
            if (writeVerboseLog)
                Debug.Log($"{Prefix} proc performing tick from {_lastKeepAliveIndex}");

            for (var i = 0; i < chunk; i++)
            {
                if (_lastKeepAliveIndex >= behaviours.Length) break;
                SendKeepAliveSafely(_lastKeepAliveIndex);
                if (hasCrashed)
                {
                    Debug.Log($"{Prefix} proc aborting tick at {_lastKeepAliveIndex} because crash was detected");
                    break;
                }

                _lastKeepAliveIndex++;
            }

            if (_lastKeepAliveIndex >= behaviours.Length) _lastKeepAliveIndex = 0;
            if (writeVerboseLog)
                Debug.Log(
                    $"{Prefix} proc ended tick at {(_lastKeepAliveIndex == 0 ? $"{_lastKeepAliveIndex} (looped)" : $"{_lastKeepAliveIndex}")}");
        }

        private void SendKeepAliveSafely(int i)
        {
            var b = TryGetUdonBehaviour(i);

            if (!b)
            {
                Debug.LogError($"{Prefix} KeepAlive NotNull ensure failed at {i}. aborting!");
                return;
            }

            var nonce = Random.Range(int.MinValue, int.MaxValue);
            if (b.KeepAlive(this, nonce) != nonce)
            {
                NotifyCrash(1000 + i);
                return;
            }

            var children = b.GetChildren();
            if (children != null)
            {
                if (writeVerboseLog)
                    Debug.Log($"{Prefix} proc performing children tick for {i} at {_lastChildIndexes[i]}");
                for (var j = 0; j < childChunk; j++)
                {
                    if (_lastChildIndexes[i] >= children.Length) break;
                    var child = children[_lastChildIndexes[i]];
                    if (child != null)
                    {
                        var childNonce = Random.Range(int.MinValue, int.MaxValue);
                        if (child.ChildKeepAlive(this, childNonce) != childNonce)
                        {
                            NotifyCrash(20000 + i * 100 + j);
                            break;
                        }
                    }

                    ++_lastChildIndexes[i];
                }

                if (_lastChildIndexes[i] >= children.Length) _lastChildIndexes[i] = 0;
                if (writeVerboseLog)
                    Debug.Log(
                        $"{Prefix} proc ended children tick for {i} at {(_lastChildIndexes[i] == 0 ? $"{_lastChildIndexes[i]} (looped)" : $"{_lastChildIndexes[i]}")}");
            }
        }

        private void NotifyCrash(int code)
        {
            hasCrashed = true;
            Debug.LogErrorFormat("{0} UdonBehaviour crashed with code {1}", Prefix, code);
            if (errorCallback) errorCallback.OnException(this, code);
        }

        private WatchdogCallbackBase TryGetUdonBehaviour(int i)
        {
            if (i >= _callbackBases.Length || 0 > i)
            {
                Debug.LogError($"{Prefix} GetBehaviourSafely Index ensure failed at {i}. returning null!");
                return null;
            }

            return _callbackBases[i];
        }

        public static void TryNotifyCrash(int code)
        {
            var go = GameObject.Find("WatchdogProc");
            if (go == null)
            {
                Debug.LogError($"{Prefix} Tried to notify crash but WatchdogProc instance was not found.");
                return;
            }

            var wd = go.GetComponent<WatchdogProc>();
            if (wd == null)
            {
                Debug.LogError($"{Prefix} Tried to notify crash but WatchdogProc instance was not found.");
                return;
            }

            wd.NotifyCrash(code);
        }
    }

    public abstract class WatchdogErrorCallbackBase : UdonSharpBehaviour
    {
        public abstract void OnException(WatchdogProc wd, int errCode);

        public abstract void OnInitialized(WatchdogProc wd);
    }
}