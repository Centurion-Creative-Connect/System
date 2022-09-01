using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Collider))]
    public class GunHolster : UdonSharpBehaviour
    {
        [SerializeField]
        private int holdableSize = 100;
        [SerializeField]
        private GameObject editHighlightObject;
        [SerializeField]
        private Material highlightMaterial;
        [SerializeField]
        private MeshRenderer holsterRenderer;
        [SerializeField]
        private VRC_Pickup pickup;

        private Collider _collider;
        private Material _defaultMaterial;
        private bool _isHighlighting;

        public int HoldableSize => holdableSize;
        public bool IsEditable
        {
            get => pickup.pickupable;
            set
            {
                pickup.pickupable = value;
                editHighlightObject.SetActive(value);
            }
        }
        public bool IsVisible
        {
            get => holsterRenderer.enabled;
            set => holsterRenderer.enabled = value;
        }
        public bool IsHolsterActive
        {
            get => _collider.enabled;
            set => _collider.enabled = value;
        }
        public bool IsHighlighting
        {
            get => _isHighlighting;
            set
            {
                _isHighlighting = value;
                if (holsterRenderer)
                    holsterRenderer.material = _isHighlighting ? highlightMaterial : _defaultMaterial;
            }
        }

        private void Start()
        {
            _collider = GetComponent<Collider>();
            if (holsterRenderer)
                _defaultMaterial = holsterRenderer.material;
        }
    }
}