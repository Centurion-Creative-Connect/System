using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
namespace CenturionCC.System.Gimmick.HardCase
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HardCaseKeyboard : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private Animator animator;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private VRCPickup pickup;

        private int _callbackCount;

        private UdonSharpBehaviour[] _callbacks;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private void Start()
        {
            _initialPosition = transform.localPosition;
            _initialRotation = transform.localRotation;
        }

        public override void OnPickupUseDown()
        {
            for (var i = 0; i < _callbackCount; i++)
                if (_callbacks[i] != null)
                    _callbacks[i].SendCustomEvent("OnKeyboardUseDown");
        }

        public override void OnPickupUseUp()
        {
            for (var i = 0; i < _callbackCount; i++)
                if (_callbacks[i] != null)
                    _callbacks[i].SendCustomEvent("OnKeyboardUseUp");
        }

        public override void OnDrop()
        {
            for (var i = 0; i < _callbackCount; i++)
                if (_callbacks[i] != null)
                    _callbacks[i].SendCustomEvent("OnKeyboardUseUp");

            MoveToInitialPositionAndRotation();
        }

        public bool Subscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.AddBehaviour(behaviour, ref _callbackCount, ref _callbacks);
        }

        public bool Unsubscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.RemoveBehaviour(behaviour, ref _callbackCount, ref _callbacks);
        }


        private void MoveToInitialPositionAndRotation()
        {
            transform.SetLocalPositionAndRotation(_initialPosition, _initialRotation);
        }

        public void SetInteractable(bool interactable)
        {
            pickup.pickupable = interactable;
            if (!interactable) pickup.Drop();
        }
    }
}
