using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    [DefaultExecutionOrder(-1)] [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GunHandle : UdonSharpBehaviour
    {
        public GunHandleCallbackBase callback;
        public HandleType handleType;
        public Transform target;
        public Transform free;
        public Material pickupableMaterial;
        private Material _defaultMaterial;
        private MeshRenderer _mesh;
        private VRC_Pickup _pickup;
        private Collider _collider;

        private bool _isAttached;

        public bool IsVisible
        {
            get => _mesh && _mesh.enabled;
            set
            {
                if (_mesh)
                    _mesh.enabled = value;
            }
        }
        public bool IsAttached
        {
            get => _isAttached || IsHolstered;
            private set => _isAttached = value;
        }
        public bool IsHolstered { get; private set; }
        public bool IsPickupable => _pickup.pickupable;

        public string UseText
        {
            get => _pickup.UseText;
            set => _pickup.UseText = value;
        }
        public bool IsPickedUp => _pickup.IsHeld;
        public VRC_Pickup.PickupHand CurrentHand => _pickup.currentHand;
        public VRCPlayerApi CurrentPlayer => _pickup.currentPlayer;

        private void Start()
        {
            _pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
            _collider = GetComponent<Collider>();
            _mesh = GetComponent<MeshRenderer>();

            if (_mesh != null)
            {
                _defaultMaterial = _mesh.material;
                if (IsPickupable && pickupableMaterial)
                    _mesh.material = pickupableMaterial;
            }

            if (target == null)
                target = transform.parent;
            if (free == null)
                free = transform.root;
            if (_collider)
                _collider.enabled = _pickup.pickupable;
        }

        public void MoveToLocalPosition(Vector3 pos, Quaternion rot)
        {
            var expectedPoint = target.TransformPoint(pos);
            transform.SetPositionAndRotation(expectedPoint, rot);
        }

        public void SetPickupable(bool isPickupable)
        {
            Debug.Log($"[GunHandle-{name}] SetPickupable: {isPickupable}, {target.name}");
            _pickup.pickupable = isPickupable;
            if (_collider != null)
                _collider.enabled = isPickupable;

            if (_mesh && pickupableMaterial)
                _mesh.material = IsPickupable ? pickupableMaterial : _defaultMaterial;

            if (!isPickupable && callback)
                MoveToLocalPosition(
                    callback.GetHandleIdlePosition(this, handleType),
                    callback.GetHandleIdleRotation(this, handleType));
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
            if (_pickup)
                _pickup.Drop();
        }

        public override void OnPickup()
        {
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
            holster.IsHighlighting = false;
            transform.SetParent(holster.transform, true);
            IsHolstered = true;
        }

        public void UnHolster()
        {
            transform.SetParent(_isAttached ? target : free);
            IsHolstered = false;
        }
    }
}