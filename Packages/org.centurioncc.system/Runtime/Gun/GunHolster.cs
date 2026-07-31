using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Gun
{
    // hack
    public abstract class GunHolsterCallback : UdonSharpBehaviour
    {
        public abstract void OnHolsterDeactivated(GunHolster holster);
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(Collider))]
    public class GunHolster : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private GunHolsterSettings holsterSettings;

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
        [SerializeField] [NewbieInject(SearchScope.Children)]
        private Collider col;

        private readonly DataList _highlightingObjects = new DataList();
        private readonly DataList _holsteredObjects = new DataList();
        private Material _defaultMaterial;

        private bool _isEditable;
        private bool _isHighlighting = true;
        private bool _isHolsterActive = true;
        private bool _isVisible;

        [PublicAPI]
        public int HoldableSize => holdableSize;

        [PublicAPI]
        public bool IsEditable
        {
            get => holsterSettings ? holsterSettings.EnableEditing && _isEditable : _isEditable;
            set
            {
                _isEditable = value;
                ApplyChanges();
            }
        }

        [PublicAPI]
        public bool IsVisible
        {
            get => holsterSettings ? holsterSettings.EnableVisual && _isVisible : _isVisible;
            set
            {
                _isVisible = value;
                ApplyChanges();
            }
        }

        [PublicAPI]
        public bool IsHolsterActive
        {
            get => holsterSettings ? holsterSettings.EnableHolster && _isHolsterActive : _isHolsterActive;
            set
            {
                _isHolsterActive = value;
                ApplyChanges();
            }
        }

        [PublicAPI]
        public bool IsHighlighting
        {
            get => holsterSettings ? holsterSettings.EnableHighlighting && ShouldHighlight && _isHighlighting : ShouldHighlight && _isHighlighting;
            set
            {
                _isHighlighting = value;
                ApplyChanges();
            }
        }

        private bool ShouldHighlight => _highlightingObjects.Count > 0;

        private void Start()
        {
            if (holsterRenderer)
                _defaultMaterial = holsterRenderer.material;

            if (holsterSettings)
                holsterSettings.Subscribe(this);

            ApplyChanges();
        }

        [PublicAPI("1.1.0")]
        public void AddHighlightingObject(UdonSharpBehaviour ub)
        {
            if (_highlightingObjects.Contains(ub)) return;

            _highlightingObjects.Add(ub);
            ApplyChanges();
        }

        [PublicAPI("1.1.0")]
        public bool RemoveHighlightingObject(UdonSharpBehaviour ub)
        {
            if (!_highlightingObjects.RemoveAll(ub)) return false;
            ApplyChanges();
            return true;
        }

        [PublicAPI("1.1.0")]
        public bool IsHighlightingObject(UdonSharpBehaviour ub)
        {
            return _highlightingObjects.Contains(ub);
        }

        [PublicAPI("1.1.0")]
        public void AddHolsteredObject(UdonSharpBehaviour ub)
        {
            if (_holsteredObjects.Contains(ub)) return;
            ApplyChanges();
            _holsteredObjects.Add(ub);
        }

        [PublicAPI("1.1.0")]
        public bool RemoveHolsteredObject(UdonSharpBehaviour ub)
        {
            if (!_holsteredObjects.RemoveAll(ub)) return false;
            ApplyChanges();
            return true;
        }

        [PublicAPI("1.1.0")]
        public bool IsHolsteredObject(UdonSharpBehaviour ub)
        {
            return _holsteredObjects.Contains(ub);
        }

        // Used from GunHolsterSettings (Event Callback)
        // ReSharper disable once UnusedMember.Global
        public void OnHolsterSettingsChanged()
        {
            ApplyChanges();
        }

        private void ApplyChanges()
        {
            Debug.Log($"apply changes: {_holsteredObjects.Count}/{_highlightingObjects.Count}");

            if (!IsHolsterActive)
            {
                foreach (var token in _holsteredObjects.ToArray())
                {
                    if (token.TokenType != TokenType.Reference || ((GunHolsterCallback)token.Reference) == null) continue;
                    ((GunHolsterCallback)token.Reference).OnHolsterDeactivated(this);
                }

                foreach (var token in _highlightingObjects.ToArray())
                {
                    if (token.TokenType != TokenType.Reference || ((GunHolsterCallback)token.Reference) == null) continue;
                    ((GunHolsterCallback)token.Reference).OnHolsterDeactivated(this);
                }

                _holsteredObjects.Clear();
                _highlightingObjects.Clear();
            }

            if (holsterRenderer)
            {
                holsterRenderer.enabled = IsVisible || IsHighlighting;
                holsterRenderer.material = IsHighlighting ? highlightMaterial : _defaultMaterial;
            }

            if (col) col.enabled = IsHolsterActive;
            if (pickup) pickup.pickupable = IsEditable;
            if (editHighlightObject) editHighlightObject.SetActive(IsEditable);
        }
    }
}
