using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    [DefaultExecutionOrder(-1)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GunHandle : UdonSharpBehaviour
    {
        public GunHandleCallbackBase callback;
        public HandleType handleType;
        public Transform target;
        public Transform free;
        public Material pickupableMaterial;
        public float desktopScaleMultiplier = 10F;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private VRCObjectSync objectSync;
        [SerializeField] [NewbieInject(SearchScope.Self)]
        private VRC_Pickup pickup;
        [SerializeField] [NewbieInject(SearchScope.Children)]
        private MeshRenderer mesh;
        [SerializeField] [NewbieInject(SearchScope.Children)]
        private Collider childCollider;

        private Material _defaultMaterial;
        private Vector3 _initialScale;

        private bool _isAttached;

        public bool IsVisible
        {
            get => mesh && mesh.enabled;
            set
            {
                if (mesh) mesh.enabled = value;
            }
        }

        public bool IsAttached
        {
            get => _isAttached || IsHolstered;
            private set => _isAttached = value;
        }

        public bool IsHolstered { get; private set; }
        public bool IsPickupable => pickup && pickup.pickupable;

        public float Proximity
        {
            get => pickup ? pickup.proximity : float.NaN;
            set
            {
                if (pickup) pickup.proximity = value;
            }
        }

        [CanBeNull]
        public string UseText
        {
            get => pickup ? pickup.UseText : null;
            set
            {
                if (pickup) pickup.UseText = value;
            }
        }

        public bool IsPickedUpLocally { get; private set; }

        public bool IsPickedUp => pickup && pickup.IsHeld;
        public VRC_Pickup.PickupHand CurrentHand => pickup ? pickup.currentHand : VRC_Pickup.PickupHand.None;

        [CanBeNull]
        public VRCPlayerApi CurrentPlayer => pickup ? pickup.currentPlayer : null;

        private void Start()
        {
            if (mesh)
            {
                _defaultMaterial = mesh.material;
                if (IsPickupable && pickupableMaterial)
                    mesh.material = pickupableMaterial;
            }

            if (target == null)
                target = transform.parent;
            if (free == null)
                free = transform.root;

            if (childCollider)
                childCollider.enabled = pickup.pickupable;

            _initialScale = transform.localScale;
        }

        public void MoveToLocalPosition(Vector3 pos, Quaternion rot)
        {
            var localToWorld = target.localToWorldMatrix;
            var expectedPos = localToWorld.MultiplyPoint3x4(pos);
            var expectedRot = localToWorld.rotation * rot;
            transform.SetPositionAndRotation(expectedPos, expectedRot);
        }

        public void AdjustScaleForDesktop(bool isVR)
        {
            transform.localScale = isVR ? _initialScale : _initialScale * desktopScaleMultiplier;
        }

        public void SetPickupable(bool isPickupable)
        {
            if (pickup)
                pickup.pickupable = isPickupable;

            if (childCollider)
                childCollider.enabled = isPickupable;

            if (mesh && pickupableMaterial)
                mesh.material = IsPickupable ? pickupableMaterial : _defaultMaterial;
        }

        public void SetPickupable(bool isPickupable, float delay)
        {
            SendCustomEventDelayedSeconds(
                isPickupable
                    ? nameof(_SetPickupableTrueDelayedSeconds)
                    : nameof(_SetPickupableFalseDelayedSeconds),
                delay);
        }

        public void _SetPickupableFalseDelayedSeconds()
        {
            SetPickupable(false);
        }

        public void _SetPickupableTrueDelayedSeconds()
        {
            SetPickupable(true);
        }

        public void ForceDrop()
        {
            if (pickup)
                pickup.Drop();
        }

        public override void OnPickup()
        {
            IsPickedUpLocally = true;
            if (callback)
                callback.OnHandlePickup(this, handleType);
        }

        public override void OnPickupUseDown()
        {
            if (callback)
                callback.OnHandleUseDown(this, handleType);
        }

        public override void OnPickupUseUp()
        {
            if (callback)
                callback.OnHandleUseUp(this, handleType);
        }

        public override void OnDrop()
        {
            IsPickedUpLocally = false;
            if (callback)
                callback.OnHandleDrop(this, handleType);
        }

        public void SetAttached(bool isAttached)
        {
            if (isAttached) Attach();
            else Detach();
        }

        public void Detach()
        {
            transform.SetParent(free, true);
            IsAttached = false;
            IsHolstered = false;
        }

        public void Attach()
        {
            transform.SetParent(target, true);
            IsAttached = true;
            IsHolstered = false;
        }

        public void Holster(GunHolster holster)
        {
            if (holster == null) return;

            holster.IsHighlighting = false;
            transform.SetParent(holster.transform, true);
            IsHolstered = true;
        }

        public void UnHolster()
        {
            transform.SetParent(_isAttached ? target : free);
            IsHolstered = false;
        }

        public void FlagDiscontinuity()
        {
            // Hope 8 seconds is enough time to let VRCObjectSync finish its job
            for (float i = 0; i < 8; i++)
            {
                SendCustomEventDelayedSeconds(nameof(Internal_FlagDiscontinuity), i);
            }
        }

        public void Internal_FlagDiscontinuity()
        {
            if (objectSync) objectSync.FlagDiscontinuity();
        }
    }
}
