using CenturionCC.System.Gun;
using DerpyNewbie.Common;
using DerpyNewbie.Common.Invoker;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class HolsterableObject : PickupEventSenderCallback
    {
        [SerializeField] private Transform target;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private VRC_Pickup pickup;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private VRCObjectSync objectSync;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private Rigidbody rb;

        [SerializeField] private int objectSize;
        private readonly DataList _highlightingHolster = new DataList();

        private bool _originalIsKinematic;
        private Transform _originalParent;
        private bool _originalUseGravity;

        public bool IsHolsteredLocally { get; private set; }
        public GunHolster ActiveHolster { get; private set; }

        private void Start()
        {
            if (target == null) target = transform;
            _originalParent = target.parent;

            if (rb == null) return;
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
                if (holster == null)
                    return;

                if (holster.HoldableSize < objectSize)
                    return;

                _AddHighlightingHolster(holster);

                if (_highlightingHolster.Count == 1)
                {
                    Networking.LocalPlayer.PlayHapticEventInHand(pickup.currentHand, .5F, 1F, .1F);
                }

                Debug.Log($"[Holsterable-{name}] holster enter");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.name.ToLower().StartsWith("holster"))
            {
                var holster = other.GetComponent<GunHolster>();
                if (holster == null)
                    return;

                _RemoveHighlightingHolster(holster);

                if (ActiveHolster == holster)
                {
                    _MakeUnholstered();
                }

                if (_highlightingHolster.Count == 0)
                {
                    Networking.LocalPlayer.PlayHapticEventInHand(pickup.currentHand, .5F, 1F, .1F);
                }

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
            if (_highlightingHolster.Count == 0) return;

            _MakeHolstered((GunHolster)_highlightingHolster[_highlightingHolster.Count - 1].Reference);
        }

        private bool _AddHighlightingHolster(GunHolster holster)
        {
            if (holster == null) return false;

            if (holster.HoldableSize < objectSize) return false;

            holster.AddHighlightingObject(this);

            _highlightingHolster.Add(holster);
            return true;
        }

        private bool _RemoveHighlightingHolster(GunHolster holster)
        {
            if (holster == null) return false;

            holster.RemoveHighlightingObject(this);

            return _highlightingHolster.RemoveAll(holster);
        }

        private void _MakeHolstered(GunHolster holster)
        {
            if (holster == null)
            {
                CenturionDiagnostic.LogWarning($"[Holsterable-{name}] _MakeHolstered: target holster is null");
                return;
            }

            if (IsHolsteredLocally)
            {
                _MakeUnholstered();
            }

            holster.AddHolsteredObject(this);
            _RemoveHighlightingHolster(holster);

            ActiveHolster = holster;
            objectSync.SetGravity(false);
            objectSync.SetKinematic(true);
            target.SetParent(holster.transform);
            IsHolsteredLocally = true;
        }

        private void _MakeUnholstered()
        {
            if (!IsHolsteredLocally) return;

            if (ActiveHolster) ActiveHolster.RemoveHolsteredObject(this);

            ActiveHolster = null;
            target.SetParent(_originalParent);
            objectSync.SetGravity(_originalUseGravity);
            objectSync.SetKinematic(_originalIsKinematic);
            IsHolsteredLocally = false;
        }

        public void UnHolster()
        {
            _MakeUnholstered();
        }
    }
}
