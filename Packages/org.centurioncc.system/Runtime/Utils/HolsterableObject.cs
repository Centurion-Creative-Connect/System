using CenturionCC.System.Gun;
using DerpyNewbie.Common.Invoker;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HolsterableObject : PickupEventSenderCallback
    {
        [SerializeField]
        private Transform target;
        [SerializeField]
        private int objectSize;
        [SerializeField]
        private VRC_Pickup pickup;
        private GunHolster _currentHolster;

        private Transform _originalParent;

        private void Start()
        {
            if (target == null) target = transform;
            _originalParent = target.parent;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.name.ToLower().StartsWith("holster"))
            {
                var holster = other.GetComponent<GunHolster>();
                if (holster.HoldableSize < objectSize)
                    return;
                _currentHolster = holster;
                Networking.LocalPlayer.PlayHapticEventInHand(pickup.currentHand, .5F, 1F, .1F);
                Debug.Log($"[Holsterable-{name}] holster enter");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name.ToLower().StartsWith("holster"))
            {
                _currentHolster = null;
                Networking.LocalPlayer.PlayHapticEventInHand(pickup.currentHand, .5F, 1F, .1F);
                Debug.Log($"[Holsterable-{name}] holster exit");
            }
        }

        public override void OnPickupRelayed()
        {
            Debug.Log($"[Holsterable-{name}] on pickup");
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(UnHolster));
        }

        public override void OnDropRelayed()
        {
            Debug.Log($"[Holsterable-{name}] on drop");
            if (_currentHolster)
                SetHolster(_currentHolster);
        }

        public void UnHolster()
        {
            SetHolster(null);
        }

        private void SetHolster(GunHolster holster)
        {
            if (holster == null)
            {
                target.SetParent(_originalParent);
                return;
            }

            target.SetParent(holster.transform);
        }
    }
}