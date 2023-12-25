using System;
using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunBullet : ProjectileBase
    {
        private const float LifeTime = 5F;
        private const float HopUpCoefficient = .005F;
        private const float DampingCoefficient = 2.5F;

        [SerializeField]
        private GunManager gunManager;
        [SerializeField]
        private TrailRenderer trailRenderer;
        [SerializeField]
        private TrailRenderer debugTrailRenderer;

        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;
        private Collider _collider;

        private Vector3 _damageOriginPos;
        private Quaternion _damageOriginRot;
        private DateTime _damageOriginTime;

        private int _damagerPlayerId;
        private string _damageType;
        private float _hopUpStrength;
        private Vector3 _initialRotationUp;
        private Vector3 _nextTorque;
        private bool _nextUseTrail;

        private Vector3 _nextVelocity;
        private int _ricochetCount;
        private Rigidbody _rigidbody;
        private bool UseTrail => gunManager.useBulletTrail && _nextUseTrail;
        private bool UseDebugTrail => gunManager.useDebugBulletTrail;

        public override bool ShouldApplyDamage =>
            gunManager.allowedRicochetCount + 1 >= _ricochetCount;
        public override int DamagerPlayerId => _damagerPlayerId;
        public override Vector3 DamageOriginPosition => _damageOriginPos;
        public override Quaternion DamageOriginRotation => _damageOriginRot;
        public override DateTime DamageOriginTime => _damageOriginTime;
        public override string DamageType => _damageType;

        public void Start()
        {
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            _collider = gameObject.GetComponent<Collider>();
            SendCustomEventDelayedFrames(nameof(LateStart), 1);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ++_ricochetCount;
            _rigidbody.velocity /= DampingCoefficient;
            if (gunManager.RicochetHandler != null)
                gunManager.RicochetHandler.OnRicochet(this, collision);
        }

        public override void Shoot(Vector3 pos, Quaternion rot, Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime time, int playerId,
            float trailTime,
            Gradient trailGradient)
        {
            // Damage data
            _damageOriginPos = pos;
            _damageOriginRot = rot;
            _damageOriginTime = time;
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
            _ricochetCount = 0;

            Activate();
            SendCustomEventDelayedSeconds(nameof(Deactivate), LifeTime);
        }

        // _FixedUpdate will only be subscribed when it's in their lifetime.
        public void _FixedUpdate()
        {
            // Apply HopUp
            var vel = _rigidbody.velocity;
            _rigidbody.AddForce(
                _initialRotationUp * (new Vector3(vel.x, 0, vel.z).magnitude * _hopUpStrength),
                ForceMode.Force);
        }

        public void LateStart()
        {
            gameObject.SetActive(false);
            _rigidbody.Sleep();
        }

        public void Deactivate()
        {
            updateManager.UnsubscribeFixedUpdate(this);
            gameObject.SetActive(false);
            IsCurrentlyActive = false;

            _ricochetCount = int.MaxValue;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.Sleep();

            TrailRendererEmission(false);
        }

        public void Activate()
        {
            updateManager.UnsubscribeFixedUpdate(this);
            updateManager.SubscribeFixedUpdate(this);
            gameObject.SetActive(true);
            _rigidbody.WakeUp();
            _rigidbody.MovePosition(_damageOriginPos);

            _rigidbody.AddForce(_damageOriginRot * _nextVelocity - _rigidbody.velocity, ForceMode.VelocityChange);
            _rigidbody.AddTorque(_damageOriginRot * _nextTorque, ForceMode.Force);

            IsCurrentlyActive = true;
            _collider.enabled = true;
            _ricochetCount = 0;
            SendCustomEventDelayedFrames(nameof(LateActivate), 1);
        }

        public void LateActivate()
        {
            TrailRendererEmission(true);
        }

        private void TrailRendererEmission(bool emit)
        {
            if (trailRenderer)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = emit && UseTrail;
            }

            if (debugTrailRenderer)
            {
                debugTrailRenderer.Clear();
                debugTrailRenderer.emitting = emit && UseDebugTrail;
            }
        }

        /// <summary>
        /// This is very inaccurate approximation, do not expect accuracy!
        /// </summary>
        /// <param name="startingPos">Shooting position</param>
        /// <param name="startingRot">Shooting rotation</param>
        /// <param name="provider">Data provider</param>
        /// <param name="offset">Data offset</param>
        /// <param name="pointsOfLine">Max points of line</param>
        /// <param name="deltaTimeBetweenPoints">Time steps between points</param>
        /// <returns>Pretty inaccurate approximation</returns>
        public static Vector3[] PredictTrajectory(Vector3 startingPos, Quaternion startingRot,
            ProjectileDataProvider provider, int offset, int pointsOfLine, float deltaTimeBetweenPoints)
        {
            provider.Get(
                offset,
                out var positionOffset,
                out var velocity,
                out var rotationOffset,
                out var torque,
                out var drag,
                out var trailDuration,
                out var trailColor
            );

            Vector3 position = positionOffset + startingPos;
            velocity = startingRot * velocity;

            var hopUpStr = torque.x * 0.02F;
            var initRotUp = startingRot * rotationOffset * Vector3.up;
            var result = new Vector3[pointsOfLine];
            result[0] = position;
            for (var i = 1; i < pointsOfLine; i++)
            {
                // Apply Gravity
                velocity += Physics.gravity * deltaTimeBetweenPoints;

                // Apply Drag
                velocity *= Mathf.Clamp01(1F - drag * deltaTimeBetweenPoints);

                // Apply Hop Up
                velocity += initRotUp * (new Vector3(velocity.x, 0, velocity.z).magnitude * hopUpStr *
                                         deltaTimeBetweenPoints);

                var nextPosition = position + velocity * deltaTimeBetweenPoints;

                result[i] = nextPosition;
                position = nextPosition;
            }

            return result;
        }
    }
}