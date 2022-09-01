using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SendVariableSyncEvent : UdonSharpBehaviour
    {
        [SerializeField]
        private UdonSharpBehaviour callback;
        [SerializeField]
        private string callbackMethod;
        [SerializeField]
        private bool dontInvokeOnInitialSync = true;

        private bool _hasInitSynced;
        [NonSerialized] [UdonSynced] [FieldChangeCallback(nameof(EventInvoker))]
        private byte _eventInvoker;

        private byte EventInvoker
        {
            get => _eventInvoker;
            set
            {
                _eventInvoker = value;
                if (!_hasInitSynced && dontInvokeOnInitialSync)
                {
                    _hasInitSynced = true;
                    return;
                }

                Internal_Invoke();
            }
        }

        private void Start()
        {
            if (Networking.LocalPlayer.isMaster)
            {
                ++_eventInvoker;
                RequestSerialization();
                _hasInitSynced = true;
            }
        }

        public void SetCallback(UdonSharpBehaviour behaviour, string method)
        {
            callback = behaviour;
            callbackMethod = method;
        }

        public void Invoke()
        {
            if (++EventInvoker == byte.MaxValue)
                EventInvoker = byte.MinValue;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        private void Internal_Invoke()
        {
            if (callback)
                callback.SendCustomEvent(callbackMethod);
        }
    }
}