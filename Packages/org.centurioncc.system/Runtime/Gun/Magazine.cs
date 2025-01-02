using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    [RequireComponent(typeof(Rigidbody), typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Magazine : UdonSharpBehaviour
    {
        [SerializeField] protected int type;
        [SerializeField] protected int roundsCapacity;
        [SerializeField] protected int roundsRemaining;
        [SerializeField] protected GameObject model;
        [SerializeField] protected Transform leftHandOffset;
        [SerializeField] protected Transform rightHandOffset;
        [SerializeField] protected BoxCollider secondaryMagazineCollider;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        protected Rigidbody rb;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        protected VRCPickup pickup;

        private bool _hasSetOriginal;

        private bool _isPullingTrigger;
        private bool _originalGravity;
        private bool _originalKinematic;

        private Transform _originalParent;

        protected Transform PickupTarget;


        [PublicAPI] public bool IsHeld => pickup.IsHeld;
        [PublicAPI] public VRCPlayerApi CurrentPlayer => pickup.currentPlayer;
        [PublicAPI] public VRC_Pickup.PickupHand CurrentHand => pickup.currentHand;

        [PublicAPI] public bool Pickupable
        {
            get => pickup.pickupable;
            set => pickup.pickupable = value;
        }

        [PublicAPI] public Rigidbody Rigidbody => rb;


        [PublicAPI] public int Type => type;
        [PublicAPI] public int RoundsCapacity => roundsCapacity;
        [PublicAPI] public int RoundsRemaining => roundsRemaining;
        [PublicAPI] public bool IsAttached { get; protected set; }
        [PublicAPI] public bool IsAttachedToReceiver { get; protected set; }
        [PublicAPI] public bool IsAttachedToMagazine { get; protected set; }

        [PublicAPI] public MagazineReceiver ParentReceiver { get; protected set; }
        [PublicAPI] public Magazine ParentMagazine { get; set; }
        [PublicAPI] public Magazine ChildMagazine { get; set; }

        private void Start()
        {
            Debug.Log($"[Magazine-{name}] Start");
            PickupTarget = pickup.ExactGun;

            if (!_hasSetOriginal)
            {
                Debug.Log($"[Magazine-{name}] Init original: g: {rb.useGravity}, k: {rb.isKinematic}");
                _hasSetOriginal = true;
                _originalParent = transform.parent;
                _originalGravity = rb.useGravity;
                _originalKinematic = rb.isKinematic;
            }
            else
            {
                Debug.Log($"[Magazine-{name}] Original has already been set. Preserving!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var receiver = other.GetComponent<MagazineReceiver>();
            if (receiver != null && !IsAttachedToReceiver)
            {
                receiver.InsertMagazine(this);
                return;
            }

            var magazine = other.GetComponent<Magazine>();
            if (magazine != null && _isPullingTrigger && !IsAttached &&
                !magazine.IsAttached && magazine.ParentMagazine != this)
            {
                magazine.AttachToMagazine(this);
                return;
            }
        }

        public override void OnPickup()
        {
            var rightPickup = pickup.currentHand == VRC_Pickup.PickupHand.Right;
            var offset = rightPickup ? rightHandOffset : leftHandOffset;
            PickupTarget.localPosition = offset.localPosition;
            PickupTarget.localRotation = offset.localRotation;

            if (IsAttachedToReceiver && ParentReceiver != null)
            {
                Debug.Log($"[Magazine-{name}] Requesting to release magazine as magazine has been picked up");
                ParentReceiver.ReleaseMagazine();
            }

            if (IsAttached)
            {
                Debug.Log($"[Magazine-{name}] Detaching because its being picked up");
                Detach();
            }
        }

        public override void OnPickupUseDown()
        {
            _isPullingTrigger = true;
        }

        public override void OnPickupUseUp()
        {
            _isPullingTrigger = false;
        }

        public override void OnDrop()
        {
            Debug.Log(
                $"[Magazine-{name}] OnDrop: magReceiver: {ParentReceiver != null} parentMagazine: {ParentMagazine != null}, childMagazine: {ChildMagazine != null}");

            _isPullingTrigger = false;
            if (ChildMagazine != null)
            {
                ChildMagazine.Detach();
            }

            // HACK: Workaround for VRCPickup reverting changes while it's picked up
            if (!IsAttached)
            {
                Debug.Log($"[Magazine-{name}] Setting to original state as it was no longer attached");
                transform.SetParent(_originalParent);
                rb.useGravity = _originalGravity;
                rb.isKinematic = _originalKinematic;
            }
        }

        [PublicAPI]
        public bool HasNextBullet()
        {
            return roundsRemaining > 0;
        }

        [PublicAPI]
        public bool ConsumeBullet()
        {
            if (!HasNextBullet()) return false;
            roundsRemaining--;
            return true;
        }

        [PublicAPI]
        public Magazine AttachToReceiver(MagazineReceiver target)
        {
            Debug.Log($"[Magazine-{name}] AttachToReceiver");

            if (ChildMagazine != null)
            {
                Debug.Log($"[Magazine-{name}] Attaching child magazine");
                var childMagazine = ChildMagazine;
                ChildMagazine = null;
                Swap(childMagazine);
                return childMagazine.AttachToReceiver(target);
            }

            Attach(target.transform);

            pickup.pickupable = false;

            ParentReceiver = target;
            IsAttachedToReceiver = true;
            return this;
        }

        [PublicAPI]
        public void AttachToMagazine(Magazine target)
        {
            Debug.Log($"[Magazine-{name}] AttachToMagazine");

            Attach(target.transform);

            transform.localPosition =
                new Vector3(
                    (leftHandOffset.localPosition.x + target.leftHandOffset.localPosition.x) *
                    Mathf.Sign(target.PickupTarget.localPosition.x), 0, 0);
            transform.localRotation = Quaternion.identity;

            ParentMagazine = target;
            ParentMagazine.ChildMagazine = this;
            IsAttachedToMagazine = true;
        }

        [PublicAPI]
        public void Attach(Transform target, bool preserveOriginal = true)
        {
            if (IsAttached) Detach();
            Debug.Log($"[Magazine-{name}] Attach: {target.name}, g: {rb.useGravity}, k: {rb.isKinematic}");

            IsAttached = true;

            if (!preserveOriginal || !_hasSetOriginal)
            {
                Debug.Log($"[Magazine-{name}] Updating original");
                _hasSetOriginal = true;
                _originalParent = transform.parent;
                _originalGravity = rb.useGravity;
                _originalKinematic = rb.isKinematic;
            }
            else
            {
                Debug.Log($"[Magazine-{name}] Preserving original");
            }

            rb.useGravity = false;
            rb.isKinematic = true;

            pickup.Drop();

            transform.SetParent(target, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        [PublicAPI]
        public void Detach()
        {
            Debug.Log(
                $"[Magazine-{name}] Detach: {transform.name}->{(_originalParent != null ? _originalParent.name : "null")}, g: {_originalGravity}, k: {_originalKinematic}");

            transform.SetParent(_originalParent);

            rb.useGravity = _originalGravity;
            rb.isKinematic = _originalKinematic;

            pickup.pickupable = true;

            IsAttachedToReceiver = false;
            ParentReceiver = null;

            IsAttachedToMagazine = false;
            if (ParentMagazine != null) ParentMagazine.ChildMagazine = null;
            ParentMagazine = null;

            IsAttached = false;
        }

        public void SetVariantData(MagazineVariantDataStore dataStore, bool loadMagazine = true)
        {
            if (dataStore == null) return;

            type = dataStore.Type;
            roundsCapacity = dataStore.RoundsCapacity;
            roundsRemaining = loadMagazine ? roundsCapacity : 0;
            leftHandOffset = dataStore.LeftHandOffset;
            rightHandOffset = dataStore.RightHandOffset;

            secondaryMagazineCollider.center = dataStore.SecondaryMagazineDetectionCollider.center;
            secondaryMagazineCollider.size = dataStore.SecondaryMagazineDetectionCollider.size;

            model = Instantiate(dataStore.Model, transform);
            model.transform.localPosition = dataStore.ModelOffsetPosition;
            model.transform.localRotation = dataStore.ModelOffsetRotation;
        }

        public void Swap(Magazine other)
        {
            if (IsAttached) Detach();
            if (other.IsAttached) Detach();

            var prevParent = transform.parent;
            var prevPosition = transform.position;
            var prevRotation = transform.rotation;
            var prevModel = model;
            var prevType = type;
            var prevChildMagazine = ChildMagazine;
            var prevRoundsCapacity = roundsCapacity;
            var prevRoundsRemaining = roundsRemaining;
            var prevLeftHandOffset = leftHandOffset;
            var prevRightHandOffset = rightHandOffset;
            var prevSecondaryMagazineColliderCenter = secondaryMagazineCollider.center;
            var prevSecondaryMagazineColliderSize = secondaryMagazineCollider.size;
            var prevRbGravity = _originalGravity;
            var prevRbKinematic = _originalKinematic;

            transform.SetParent(other.transform.parent);
            transform.SetPositionAndRotation(other.transform.position, other.transform.rotation);
            model = other.model;
            model.transform.SetParent(transform, false);
            type = other.type;
            ChildMagazine = other.ChildMagazine;
            if (ChildMagazine != null) ChildMagazine.ParentMagazine = this;
            roundsCapacity = other.roundsCapacity;
            roundsRemaining = other.roundsRemaining;
            leftHandOffset = other.leftHandOffset;
            rightHandOffset = other.rightHandOffset;
            secondaryMagazineCollider.center = other.secondaryMagazineCollider.center;
            secondaryMagazineCollider.size = other.secondaryMagazineCollider.size;
            rb.useGravity = other._originalGravity;
            rb.isKinematic = other._originalKinematic;

            other.transform.SetParent(prevParent);
            other.transform.SetPositionAndRotation(prevPosition, prevRotation);
            other.model = prevModel;
            other.model.transform.SetParent(other.transform, false);
            other.type = prevType;
            other.ChildMagazine = prevChildMagazine;
            if (other.ChildMagazine != null) other.ChildMagazine.ParentMagazine = other;
            other.roundsCapacity = prevRoundsCapacity;
            other.roundsRemaining = prevRoundsRemaining;
            other.leftHandOffset = prevLeftHandOffset;
            other.rightHandOffset = prevRightHandOffset;
            other.secondaryMagazineCollider.center = prevSecondaryMagazineColliderCenter;
            other.secondaryMagazineCollider.size = prevSecondaryMagazineColliderSize;
            other.rb.useGravity = prevRbGravity;
            other.rb.isKinematic = prevRbKinematic;
        }
    }
}