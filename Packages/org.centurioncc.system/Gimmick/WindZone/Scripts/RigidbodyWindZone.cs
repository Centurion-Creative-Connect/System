using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.WindZone
{
    /// <summary>
    /// Pushes rigidbodies inside the trigger as if the air around them were moving.
    /// </summary>
    /// <remarks>
    /// Unity's linear drag decelerates a rigidbody towards zero velocity, which is drag against still air
    /// (a = -drag * v). Moving air at wind velocity w turns that into a = -drag * (v - w), so the wind zone only
    /// has to add drag * w. Objects therefore drift towards the wind velocity at the rate their own drag allows:
    /// high-drag BBs get pushed noticeably while low-drag objects barely notice.
    /// </remarks>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RigidbodyWindZone : UdonSharpBehaviour
    {
        [SerializeField] [UdonSynced]
        private Vector3 windDirection;
        [SerializeField] [UdonSynced]
        [Tooltip("Mean wind speed in m/s.")]
        private float windPower;

        [Header("Coupling")]
        [SerializeField]
        [Tooltip("Rigidbodies with less drag than this are treated as having this much drag towards the wind. " +
                 "Use to let wind affect objects that have zero drag. 0 to only use rigidbody's own drag.")]
        private float minimumDrag;

        [Header("Gusts")]
        [SerializeField] [Range(0, 1)]
        [Tooltip("How much wind speed fluctuates around the mean. 0.3 means +-30%.")]
        private float gustStrength = 0.3F;
        [SerializeField]
        [Tooltip("How fast gusts come and go, roughly in cycles per second.")]
        private float gustFrequency = 0.25F;
        [SerializeField] [Range(0, 90)]
        [Tooltip("How far, in degrees, wind direction wanders from its mean direction.")]
        private float turbulenceAngle = 10F;
        [SerializeField]
        [Tooltip("How fast wind direction wanders, roughly in cycles per second.")]
        private float turbulenceFrequency = 0.1F;
        [SerializeField]
        [Tooltip("Offsets the gust pattern so multiple wind zones don't gust in unison.")]
        private float noiseSeed;
        private readonly DataList _colliderCounts = new DataList();

        private readonly DataList _rigidbodies = new DataList();

        private Vector3 _currentWindVelocity;

        [PublicAPI]
        public Vector3 WindDirection => windDirection;
        [PublicAPI]
        public float WindPower => windPower;
        /// <summary>
        /// Wind velocity including gusts and turbulence, as of the last physics step.
        /// </summary>
        [PublicAPI]
        public Vector3 CurrentWindVelocity => _currentWindVelocity;

        private void Start()
        {
            windDirection = windDirection.normalized;
        }

        private void FixedUpdate()
        {
            if (_rigidbodies.Count == 0) return;

            var wind = ComputeWindVelocity(GetNoiseTime());
            _currentWindVelocity = wind;

            // Keep the extra drag term from overshooting the wind velocity within a single step
            var maxExtraDrag = 1F / Time.fixedDeltaTime;

            // foreach cannot be used for DataList in UdonSharp
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _rigidbodies.Count; ++i)
            {
                var token = _rigidbodies[i];
                var rb = (Rigidbody)token.Reference;
                if (!rb)
                {
                    Debug.Log($"{name}: Rigidbody at index {i} is null");
                    RemoveAt(i);
                    --i;
                    continue;
                }

                if (!rb.gameObject.activeInHierarchy)
                {
                    Debug.Log($"{name}: Rigidbody {rb.name} is inactive");
                    RemoveAt(i);
                    --i;
                    continue;
                }

                if (rb.isKinematic) continue;

                var drag = rb.drag;
                var acceleration = wind * drag;

                var extraDrag = Mathf.Min(minimumDrag - drag, maxExtraDrag);
                if (extraDrag > 0)
                    acceleration += (wind - rb.velocity) * extraDrag;

                rb.AddForce(acceleration, ForceMode.Acceleration);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            // A rigidbody with multiple colliders enters once per collider; only push it once
            var index = _rigidbodies.IndexOf(rb);
            if (index != -1)
            {
                _colliderCounts[index] = _colliderCounts[index].Int + 1;
                return;
            }

            _rigidbodies.Add(rb);
            _colliderCounts.Add(1);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            var rb = other.attachedRigidbody;
            if (rb == null) return;

            var index = _rigidbodies.IndexOf(rb);
            if (index == -1) return;

            var count = _colliderCounts[index].Int - 1;
            if (count > 0)
            {
                _colliderCounts[index] = count;
                return;
            }

            RemoveAt(index);
        }

        private void RemoveAt(int index)
        {
            _rigidbodies.RemoveAt(index);
            _colliderCounts.RemoveAt(index);
        }

        private float GetNoiseTime()
        {
            // Server time keeps gusts in sync between players. Wrapped to keep float precision for Perlin noise.
            return (float)(Networking.GetServerTimeInSeconds() % 10000D);
        }

        private Vector3 ComputeWindVelocity(float time)
        {
            var speed = windPower;
            if (gustStrength > 0)
            {
                var gust = Mathf.PerlinNoise(time * gustFrequency, noiseSeed) * 2F - 1F;
                speed *= Mathf.Max(1F + gust * gustStrength, 0F);
            }

            var direction = windDirection;
            if (turbulenceAngle > 0)
            {
                var t = time * turbulenceFrequency;
                var yaw = (Mathf.PerlinNoise(noiseSeed + 31.4F, t) * 2F - 1F) * turbulenceAngle;
                var pitch = (Mathf.PerlinNoise(t, noiseSeed + 67.8F) * 2F - 1F) * turbulenceAngle * 0.5F;

                var right = Vector3.Cross(Vector3.up, direction);
                if (right.sqrMagnitude < 0.0001F) right = Vector3.right;
                right.Normalize();

                direction = Quaternion.AngleAxis(yaw, Vector3.up) * (Quaternion.AngleAxis(pitch, right) * direction);
            }

            return direction * speed;
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
        /// <param name="power">The new mean wind speed. Represented in m/s.</param>
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

            if (!Application.isPlaying) return;
            gizmoColor = Color.yellow;
            gizmoColor.a = 0.5f;
            Gizmos.color = gizmoColor;
            GizmosUtil.DrawArrow(pos, pos + _currentWindVelocity);
        }
#endif
    }
}
