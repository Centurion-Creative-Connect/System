using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player
{
    [DefaultExecutionOrder(0)] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerCollider : UdonSharpBehaviour
    {
        public PlayerBase player;
        private MeshRenderer _meshRenderer;

        public bool IsVisible
        {
            get => _meshRenderer && _meshRenderer.gameObject.activeSelf;
            set
            {
                if (_meshRenderer)
                    _meshRenderer.gameObject.SetActive(value);
            }
        }
        public Collider ActualCollider { get; private set; }

        private void Start()
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            ActualCollider = GetComponent<Collider>();
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

        public void OnDamage(DamageData damageData, Vector3 contactPoint)
        {
            if (damageData == null || player == null) return;

            player.OnDamage(this, damageData, contactPoint);
        }
    }
}