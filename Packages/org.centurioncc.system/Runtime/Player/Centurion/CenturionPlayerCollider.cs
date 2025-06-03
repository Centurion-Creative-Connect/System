using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionPlayerCollider : PlayerColliderBase
    {
        [SerializeField]
        private PlayerBase player;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private MeshRenderer meshRenderer;

        [SerializeField]
        private CapsuleCollider capsule;

        [SerializeField]
        private float heightOffset = 0.25F;

        [SerializeField]
        private BodyParts parts;

        [SerializeField]
        private HumanBodyBones boneFrom;

        [SerializeField]
        private HumanBodyBones boneTo;

        private Vector3 _calibratedPosOffset;
        private Quaternion _calibratedRotOffset;

        private bool _isVisible;
        private VRCPlayerApi _vrcPlayer;

        public override bool IsDebugVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                UpdateVisibility();
            }
        }

        public override BodyParts BodyParts => parts;
        public override Collider ActualCollider => capsule;

        private void Start()
        {
            _vrcPlayer = Networking.GetOwner(gameObject);
        }

        public void OnCollisionEnter(Collision other)
        {
            var damageData = other.gameObject.GetComponentInChildren<DamageData>();
            var contact = other.GetContact(0);
            OnDamage(damageData, contact.point);
        }

        public void OnTriggerEnter(Collider other)
        {
            var damageData = other.gameObject.GetComponentInChildren<DamageData>();
            var closestPoint = other.ClosestPoint(transform.position);
            OnDamage(damageData, closestPoint);
        }

        public override void OnAvatarChanged(VRCPlayerApi vrcPlayer)
        {
            if (vrcPlayer == _vrcPlayer)
            {
                Calibrate();
            }
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi vrcPlayer, float prevEyeHeightAsMeters)
        {
            if (vrcPlayer == _vrcPlayer)
            {
                Calibrate();
            }
        }

        public override void PostLateUpdate()
        {
            var rot = _vrcPlayer.GetBoneRotation(boneFrom) * _calibratedRotOffset;
            transform.SetPositionAndRotation(
                _vrcPlayer.GetBonePosition(boneFrom) + rot * _calibratedPosOffset,
                rot
            );
        }

        private void Calibrate()
        {
            meshRenderer.enabled = _isVisible;
            capsule.enabled = true;

            var from = _vrcPlayer.GetBonePosition(boneFrom);
            var to = _vrcPlayer.GetBonePosition(boneTo);

            if (from == Vector3.zero || to == Vector3.zero)
            {
                Debug.LogError(
                    $"[CPlayerCollider] Bone positions are zero for {boneFrom} -> {boneTo} on {_vrcPlayer.displayName}");
                meshRenderer.enabled = false;
                capsule.enabled = false;
                return;
            }

            var len = Vector3.Distance(from, to);
            transform.localScale = new Vector3(1, 1, len + heightOffset);
            _calibratedRotOffset = Quaternion.Inverse(_vrcPlayer.GetBoneRotation(boneFrom)) *
                                   Quaternion.LookRotation(to - from);
            _calibratedPosOffset = Vector3.forward * ((len + heightOffset) / 2.0F);
        }

        private void UpdateVisibility()
        {
            if (!meshRenderer) return;

            var isEnabled = capsule.enabled && IsDebugVisible;
            meshRenderer.gameObject.SetActive(isEnabled);
            meshRenderer.enabled = isEnabled;
        }

        private void OnDamage(DamageData damageData, Vector3 contactPoint)
        {
            if (!damageData || !player) return;

            player.OnLocalHit(this, damageData, contactPoint);
        }
    }
}