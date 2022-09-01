using CenturionCC.System.Audio;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace CenturionCC.System.Gun
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunBullet : ProjectileBase
    {
        private const float LifeTime = 3F;
        private const float HopUpCoefficient = .005F;
        private const float DampingCoefficient = 5F;
        private const float RicochetVolumeSpeedCoefficient = 3F;

        [SerializeField]
        private GunManager gunManager;
        [SerializeField]
        private TrailRenderer trailRenderer;
        [SerializeField]
        private TrailRenderer debugTrailRenderer;

        private float _currentMaxLifetime;
        private Collider _collider;
        private float _hopUpStrength;
        private Vector3 _initialRotationUp;

        private Vector3 _nextVelocity;
        private Vector3 _nextTorque;
        private bool _nextUseTrail;
        private Rigidbody _rigidbody;

        private bool _shootFlag;

        private UpdateManager _updateManager;
        private bool UseTrail => gunManager.UseBulletTrail && _nextUseTrail;
        private bool UseDebugTrail => gunManager.UseDebugBulletTrail;
        private int RicochetCount { get; set; }

        private int _damagerPlayerId;
        private Vector3 _damageOriginPos;
        private Quaternion _damageOriginRot;
        private string _damageType;

        public override bool ShouldApplyDamage =>
            gunManager.AllowedRicochetCount + 1 >= RicochetCount;
        public override int DamagerPlayerId => _damagerPlayerId;
        public override Vector3 DamageOriginPosition => _damageOriginPos;
        public override Quaternion DamageOriginRotation => _damageOriginRot;
        public override string DamageType => _damageType;

        public override void Shoot(Vector3 pos, Quaternion rot, Vector3 velocity, Vector3 torque, float drag,
            string damageType, int playerId,
            float trailTime,
            Gradient trailGradient)
        {
            // Damage data
            _damageOriginPos = pos;
            _damageOriginRot = rot;
            _damageType = $"BBBullet: {damageType}";
            _damagerPlayerId = playerId;

            // Speed data
            _rigidbody.drag = drag;

            _nextVelocity = velocity;
            _nextTorque = torque;

            // HopUp data
            _initialRotationUp = rot * Vector3.up;
            // Flip x rotation force for hop up cus it's negative rotation causing hop up
            _hopUpStrength = torque.x * HopUpCoefficient;

            // Trail data (nullable)
            if (float.IsNaN(trailTime) || trailGradient == null)
            {
                _nextUseTrail = false;
            }
            else
            {
                _nextUseTrail = true;
                trailRenderer.time = trailTime;
                trailRenderer.colorGradient = trailGradient;
            }

            // Prepare
            _collider.enabled = false;
            _shootFlag = true;
            RicochetCount = 0;

            _updateManager.UnsubscribeFixedUpdate(this);
            _updateManager.SubscribeFixedUpdate(this);
            gameObject.SetActive(true);
        }

        public void Start()
        {
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            _collider = gameObject.GetComponent<Collider>();
            _updateManager = GameManagerHelper.GetUpdateManager();
            SendCustomEventDelayedFrames(nameof(LateStart), 1);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ++RicochetCount;
            _rigidbody.velocity /= DampingCoefficient;
            if (Vector3.Distance(gameObject.transform.position, Networking.LocalPlayer.GetPosition()) < 35F)
                TryScheduleRicochetAudio(collision);
        }

        public void _FixedUpdate()
        {
            if (!IsCurrentlyActive)
            {
                if (!_shootFlag) return;

                DeactivateTrailRenderer();
                Activate();
                _shootFlag = false;
                SendCustomEventDelayedFrames(nameof(ActivateTrailRenderer), 1, EventTiming.LateUpdate);
                return;
            }

            if (_currentMaxLifetime > Time.timeSinceLevelLoad)
            {
                // Apply HopUp
                var vel = _rigidbody.velocity;
                _rigidbody.AddForce(
                    _initialRotationUp * (new Vector3(vel.x, 0, vel.z).magnitude * _hopUpStrength),
                    ForceMode.Force);
            }
            else
            {
                Deactivate();
                DeactivateTrailRenderer();
            }
        }

        public void LateStart()
        {
            gameObject.SetActive(false);
            _rigidbody.Sleep();
        }

        public void Deactivate()
        {
            _updateManager.UnsubscribeFixedUpdate(this);
            gameObject.SetActive(false);
            IsCurrentlyActive = false;

            RicochetCount = int.MaxValue;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.Sleep();
        }

        public void Activate()
        {
            _rigidbody.MovePosition(_damageOriginPos);
            _rigidbody.WakeUp();

            _rigidbody.AddForce(_damageOriginRot * _nextVelocity - _rigidbody.velocity, ForceMode.VelocityChange);
            _rigidbody.AddTorque(_damageOriginRot * _nextTorque, ForceMode.Force);

            // _rigidbody.AddTorque(_nextPosition.right * (-1 * _nextHopUpRotation), ForceMode.Force);

            _currentMaxLifetime = Time.timeSinceLevelLoad + LifeTime;
            IsCurrentlyActive = true;

            RicochetCount = 0;

            _collider.enabled = true;
            gameObject.SetActive(true);
        }

        public void ActivateTrailRenderer()
        {
            if (trailRenderer && UseTrail)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }

            if (debugTrailRenderer && UseDebugTrail)
            {
                debugTrailRenderer.Clear();
                debugTrailRenderer.emitting = true;
            }
        }

        public void DeactivateTrailRenderer()
        {
            if (trailRenderer)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = false;
            }

            if (debugTrailRenderer)
            {
                debugTrailRenderer.Clear();
                debugTrailRenderer.emitting = false;
            }
        }

        private void TryScheduleRicochetAudio(Collision col)
        {
            var marker = col.rigidbody
                ? col.rigidbody.GetComponent<AudioMarker>()
                : TryGetMarkerRecursively(col.gameObject, 5);
            var volume = col.impulse.magnitude / RicochetVolumeSpeedCoefficient;
            // Debug.Log($"mag: {col.impulse.magnitude}, vol: {volume}");
            if (marker)
                marker.PlayAt(transform.position, Mathf.Clamp01(volume));
        }

        [RecursiveMethod]
        private AudioMarker TryGetMarkerRecursively(GameObject o, int maxTryCount)
        {
            var marker = o.GetComponent<AudioMarker>();
            if (marker || maxTryCount <= 0) return marker;
            if (!o.transform.parent) return null;
            return TryGetMarkerRecursively(o.transform.parent.gameObject, --maxTryCount);
        }
    }
}