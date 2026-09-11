using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.WindZone
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class WindZoneBasic : UdonSharpBehaviour
    {
        [SerializeField] [UdonSynced]
        private Vector3 windDirection;
        [SerializeField] [UdonSynced]
        private float windPower;

        private readonly DataList _rigidbodies = new DataList();

        [PublicAPI]
        public Vector3 WindDirection => windDirection;
        [PublicAPI]
        public float WindPower => windPower;

        private void Start()
        {
            windDirection = windDirection.normalized;
        }

        private void FixedUpdate()
        {
            var windForce = windDirection * windPower;

            // foreach cannot be used for DataList in UdonSharp
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _rigidbodies.Count; ++i)
            {
                var token = _rigidbodies[i];
                var rb = (Rigidbody)token.Reference;
                if (!rb)
                {
                    Debug.Log($"{name}: Rigidbody at index {i} is null");
                    _rigidbodies.RemoveAt(i);
                    --i;
                    continue;
                }

                if (!rb.gameObject.activeSelf)
                {
                    Debug.Log($"{name}: Rigidbody {rb.name} is inactive");
                    _rigidbodies.RemoveAt(i);
                    --i;
                    continue;
                }

                rb.AddForce(windForce, ForceMode.Force);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            _rigidbodies.Add(rb);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            _rigidbodies.Remove(rb);
        }

        /// <summary>
        /// Sets wind direction of the wind zone.
        /// </summary>
        /// <param name="direction">The new wind direction. Represented in world coordinate.</param>
        [PublicAPI]
        public void SetWindDirection(Vector3 direction)
        {
            windDirection = direction.normalized;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            RequestSerialization();
        }

        /// <summary>
        /// Sets wind power of the wind zone.
        /// </summary>
        /// <param name="power">The new wind power. Represented in world unit.</param>
        [PublicAPI]
        public void SetWindPower(float power)
        {
            windPower = power;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);

            RequestSerialization();
        }


#if UNITY_EDITOR && !COMPILER_UDONSHARP
        private void OnValidate()
        {
            var childColliders = GetComponentsInChildren<Collider>(true);
            foreach (var childCollider in childColliders)
                if (childCollider.isTrigger)
                    childCollider.includeLayers |= LayerMask.GetMask("GameProjectile");
        }

        private void OnDrawGizmos()
        {
            var gizmoColor = Color.cyan;
            gizmoColor.a = 0.5f;
            Gizmos.color = gizmoColor;
            var pos = transform.position;
            GizmosUtil.DrawArrow(pos, pos + (windDirection.normalized * windPower));
        }
#endif
    }
}
