using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WallManager : UdonSharpBehaviour
    {
        [SerializeField]
        private GameObject a00;
        [SerializeField]
        private GameObject a01;
        [SerializeField]
        private GameObject b00;
        [SerializeField]
        private GameObject b01;

        [UdonSynced] [FieldChangeCallback(nameof(A00IsActive))]
        private bool _a00IsActive;
        [UdonSynced] [FieldChangeCallback(nameof(A01IsActive))]
        private bool _a01IsActive;
        [UdonSynced] [FieldChangeCallback(nameof(B00IsActive))]
        private bool _b00IsActive;
        [UdonSynced] [FieldChangeCallback(nameof(B01IsActive))]
        private bool _b01IsActive;
        private int _eventCallbackCount;

        private UdonSharpBehaviour[] _eventCallbacks;

        public bool A00IsActive
        {
            get => _a00IsActive;
            set
            {
                _a00IsActive = value;
                a00.SetActive(value);
                RefreshUIs();
            }
        }
        public bool A01IsActive
        {
            get => _a01IsActive;
            set
            {
                _a01IsActive = value;
                a01.SetActive(value);
                RefreshUIs();
            }
        }
        public bool B00IsActive
        {
            get => _b00IsActive;
            set
            {
                _b00IsActive = value;
                b00.SetActive(value);
                RefreshUIs();
            }
        }
        public bool B01IsActive
        {
            get => _b01IsActive;
            set
            {
                _b01IsActive = value;
                b01.SetActive(value);
                RefreshUIs();
            }
        }

        private void RefreshUIs()
        {
            for (var i = 0; i < _eventCallbackCount; i++)
            {
                var t = _eventCallbacks[i];
                t.SetProgramVariable(nameof(A00IsActive), A00IsActive);
                t.SetProgramVariable(nameof(A01IsActive), A01IsActive);
                t.SetProgramVariable(nameof(B00IsActive), B00IsActive);
                t.SetProgramVariable(nameof(B01IsActive), B01IsActive);
                t.SendCustomEvent("OnUIRefresh");
            }
        }

        public void Sync()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void Refresh()
        {
            Debug.Log("[WallManager] Refreshing UI");
            RefreshUIs();
        }

        #region CallbackRegisterer

        public void SubscribeCallback(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            if (_eventCallbacks == null)
            {
                _eventCallbacks = new UdonSharpBehaviour[5];
                _eventCallbackCount = 0;
            }

            _eventCallbacks = _AddBehaviour(_eventCallbackCount++, behaviour, _eventCallbacks);
        }

        public void UnsubscribeCallback(UdonSharpBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            if (_eventCallbacks == null)
            {
                _eventCallbacks = new UdonSharpBehaviour[5];
                _eventCallbackCount = 0;
            }

            var result = _RemoveBehaviour(behaviour, _eventCallbacks);
            if (result == null) return;
            --_eventCallbackCount;
            _eventCallbacks = result;
        }


        /// <summary>
        ///     Adds provided behaviour into provided array
        /// </summary>
        /// <param name="index">index of insert point</param>
        /// <param name="item">an item to insert into <c>arr</c></param>
        /// <param name="arr">an array which <c>item</c> gets inserted</param>
        /// <returns>An array which <c>item</c> is inserted at <c>index</c>. Returns null when invalid params are provided!</returns>
        private UdonSharpBehaviour[] _AddBehaviour(int index, UdonSharpBehaviour item, UdonSharpBehaviour[] arr)
        {
            if (arr == null || item == null || index < 0 || index > arr.Length + 5)
                return null;

            if (arr.Length <= index)
            {
                var newArr = new UdonSharpBehaviour[arr.Length + 5];
                Array.Copy(arr, newArr, arr.Length);
                arr = newArr;
            }

            Debug.Log($"add behaviour at {index} {item.name}");

            arr[index] = item;
            return arr;
        }

        /// <summary>
        ///     Removes provided behaviour from provided array
        /// </summary>
        /// <param name="item">an item to remove</param>
        /// <param name="arr">an array which <c>item</c> will get removed from</param>
        /// <returns>An array which <c>item</c> is removed and items after <c>item</c> is moved to fill space</returns>
        private UdonSharpBehaviour[] _RemoveBehaviour(UdonSharpBehaviour item, UdonSharpBehaviour[] arr)
        {
            if (item == null || arr == null)
                return null;

            var index = Array.IndexOf(arr, item);
            if (index == -1)
                return null;
            Array.ConstrainedCopy(arr, index + 1, arr, index, arr.Length - 1 - index);
            return arr;
        }

        #endregion
    }
}