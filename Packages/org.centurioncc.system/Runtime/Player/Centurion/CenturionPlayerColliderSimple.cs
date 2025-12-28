using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.Centurion
{
    public class CenturionPlayerColliderSimple : PlayerColliderBase
    {
        [SerializeField]
        private PlayerBase player;

        [SerializeField] [NewbieInject(SearchScope.Children)]
        private MeshRenderer meshRenderer;

        [SerializeField]
        private CapsuleCollider capsule;

        [SerializeField]
        private Transform groundPivot;

        [SerializeField]
        private float heightOffset = 0.25F;

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

        public override BodyParts BodyParts => BodyParts.Body;

        public override Collider ActualCollider => capsule;

        private void Start()
        {
            _vrcPlayer = Networking.GetOwner(gameObject);
        }

        private void OnEnable()
        {
            UpdateVisibility();
        }

        private void OnDisable()
        {
            UpdateVisibility();
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

        public override void PostLateUpdate()
        {
            if (!Utilities.IsValid(_vrcPlayer))
            {
                return;
            }

            groundPivot.SetPositionAndRotation(_vrcPlayer.GetPosition(), _vrcPlayer.GetRotation());

            var head = _vrcPlayer.GetBonePosition(HumanBodyBones.Head);
            var foot = (_vrcPlayer.GetBonePosition(HumanBodyBones.LeftFoot) +
                        _vrcPlayer.GetBonePosition(HumanBodyBones.RightFoot)) / 2F;
            var dir = head - foot;
            var height = dir.magnitude;
            var rot = height == 0 ? Quaternion.identity : Quaternion.LookRotation(dir);
            groundPivot.SetPositionAndRotation(foot, rot);

            groundPivot.localScale = new Vector3(1, 1, height + heightOffset);
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
            if (!damageData || !player || !damageData.ShouldApplyDamage) return;

            player.OnLocalHit(this, damageData, contactPoint);
        }
    }
}
