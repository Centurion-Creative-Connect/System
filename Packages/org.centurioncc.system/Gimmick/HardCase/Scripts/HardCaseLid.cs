using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.HardCase
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HardCaseLid : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private UpdateManager updateManager;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private VRCPickup pickup;

        [SerializeField] [NewbieInject(SearchScope.Parents)]
        private Animator animator;

        [SerializeField] private Transform pivotReference;
        [SerializeField] private Transform actualReference;
        [SerializeField] private HardCaseLock[] locks;
        [SerializeField] private float lockedMargin = 0.05F;
        [SerializeField] private float maxAngle = 0.55F;
        private readonly int _animLeftLocked = Animator.StringToHash("LockLeft");

        private readonly int _animOpen = Animator.StringToHash("Open");
        private readonly int _animRightLocked = Animator.StringToHash("LockRight");
        private int _callbackCount;

        private UdonSharpBehaviour[] _callbacks;

        private Vector3 _initialRelativePos;
        private Quaternion _initialRelativeRot;

        [UdonSynced] private float _openProgress;

        [PublicAPI] public bool IsLocked { get; set; }

        [PublicAPI] public float OpenProgressNormalized => _openProgress / maxAngle;

        private void Start()
        {
            foreach (var caseLock in locks) caseLock.Subscribe(this);

            var worldToLocal = actualReference.worldToLocalMatrix;
            _initialRelativePos = worldToLocal.MultiplyPoint(transform.position);
            _initialRelativeRot = worldToLocal.rotation * transform.rotation;

            UpdateLockedState();
        }

        private void OnDisable()
        {
            _openProgress = 0;
            UpdatePosition();
            if (Networking.IsOwner(gameObject)) RequestSerialization();
            _PostLateUpdate();
        }

        public override void OnDeserialization()
        {
            UpdateAnimator();
            Invoke_OpenProgressUpdated();
        }

        [PublicAPI] // Callback from UpdateManager
        public void _PostLateUpdate()
        {
            UpdateLockedState();
            UpdateOpenProgress();
            UpdateAnimator();
        }

        [PublicAPI] // Callback from HardCaseLock
        public void OnLockUpdated()
        {
            _PostLateUpdate();
        }

        public bool Subscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.AddBehaviour(behaviour, ref _callbackCount, ref _callbacks);
        }

        public bool Unsubscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.RemoveBehaviour(behaviour, ref _callbackCount, ref _callbacks);
        }

        private void Invoke_OpenProgressUpdated()
        {
            for (var i = 0; i < _callbackCount; ++i)
                if (_callbacks[i] != null)
                    _callbacks[i].SendCustomEvent("OnOpenProgressUpdated");
        }

        private void UpdateLockedState()
        {
            var isLocked = false;
            foreach (var caseLock in locks)
            {
                if (!caseLock.IsLocked) continue;
                isLocked = true;
            }

            if (IsLocked != isLocked) IsLocked = isLocked;
            SetPickupable(!IsLocked || _openProgress >= lockedMargin);
        }

        private void UpdateOpenProgress()
        {
            var point = pivotReference.worldToLocalMatrix.MultiplyPoint(transform.position);
            var dot = Vector3.Dot(new Vector3(0, Mathf.Max(point.y, 0), point.z).normalized, Vector3.back);
            var wasOpen = _openProgress >= lockedMargin;
            var min = IsLocked && wasOpen ? lockedMargin : 0;
            var max = IsLocked && !wasOpen ? 0 : maxAngle;

            _openProgress = Mathf.Clamp((dot + 1) / 2, min, max);

            Debug.Log($"OpenProg: {point:F2}, {dot:F2}, {min:F2} <= {_openProgress:F2} <= {max:F2}");
            Invoke_OpenProgressUpdated();
            RequestSerialization();
        }

        private void UpdateAnimator()
        {
            animator.SetFloat(_animOpen, _openProgress);
            animator.SetBool(_animLeftLocked, locks[0].IsLocked);
            animator.SetBool(_animRightLocked, locks[1].IsLocked);
        }

        private void UpdatePosition()
        {
            var localToWorld = actualReference.localToWorldMatrix;
            var rot = localToWorld.rotation * _initialRelativeRot;
            transform.SetPositionAndRotation(
                localToWorld.MultiplyPoint3x4(_initialRelativePos),
                rot
            );
        }

        public void SetPickupable(bool isPickupable)
        {
            var hasDiff = pickup.pickupable != isPickupable;
            pickup.pickupable = isPickupable;
            if (!hasDiff) return;

            if (!isPickupable) pickup.Drop();
            else UpdatePosition();
        }

        public override void OnPickup()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            updateManager.SubscribePostLateUpdate(this);
        }

        public override void OnDrop()
        {
            updateManager.UnsubscribePostLateUpdate(this);

            _PostLateUpdate();

            UpdatePosition();
        }
    }
}
