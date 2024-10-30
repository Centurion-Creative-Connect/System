using CenturionCC.System.Gun;
using DerpyNewbie.Common.Invoker;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HolsterableObject : PickupEventSenderCallback
    {
        [SerializeField] private Transform target;
        [SerializeField] private VRC_Pickup pickup;
        [SerializeField] private VRCObjectSync objectSync;
        [SerializeField] private int objectSize;

        private GunHolster _currentHolster;
        private bool _originalIsKinematic;
        private Transform _originalParent;
        private bool _originalUseGravity;

        public bool IsHolsteredLocally { get; private set; }

        private void Start()
        {
            if (target == null) target = transform;
            _originalParent = target.parent;

            if (objectSync == null) return;
            var rb = objectSync.GetComponent<Rigidbody>();
            _originalIsKinematic = rb.isKinematic;
            _originalUseGravity = rb.useGravity;
        }

        private void OnDisable()
        {
            if (IsHolsteredLocally) UnHolster();
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
                objectSync.SetGravity(_originalUseGravity);
                objectSync.SetKinematic(_originalIsKinematic);
                IsHolsteredLocally = false;
                return;
            }

            objectSync.SetGravity(false);
            objectSync.SetKinematic(true);
            target.SetParent(holster.transform);
            IsHolsteredLocally = true;
        }
    }
}