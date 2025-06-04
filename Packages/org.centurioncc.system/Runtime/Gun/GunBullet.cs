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

        private Guid _eventId;
        private float _hopUpStrength;
        private Vector3 _initialRotationUp;
        private Vector3 _nextTorque;
        private bool _nextUseTrail;

        private Vector3 _nextVelocity;
        private int _ricochetCount;
        private Rigidbody _rigidbody;
        private bool UseTrail => gunManager.useBulletTrail && _nextUseTrail;
        private bool UseDebugTrail => gunManager.useDebugBulletTrail;

        public override Guid EventId => _eventId;

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

        public override void Shoot(Guid eventId, Vector3 pos, Quaternion rot, Vector3 velocity, Vector3 torque,
            float drag,
            string damageType, DateTime time, int playerId,
            float trailTime, Gradient trailGradient,
            float lifeTimeInSeconds)
        {
            // Damage data
            _eventId = eventId;
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
            SendCustomEventDelayedSeconds(nameof(Deactivate), lifeTimeInSeconds);
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
        /// <returns>Pretty inaccurate approximation</returns>
        public static Vector3[] PredictTrajectory(Vector3 startingPos, Quaternion startingRot,
            ProjectileDataProvider provider, int offset, int pointsOfLine)
        {
            provider.Get
            (
                offset,
                out var posOffset, out var velocity,
                out var rotOffset, out var torque,
                out var drag,
                out var trailDuration,
                out var trailColor,
                out var lifeTimeInSeconds
            );

            var tempRot = startingRot * rotOffset;
            var tempPos = startingPos + (tempRot * posOffset);
            return PredictTrajectory(tempPos, tempRot, velocity, torque, drag, pointsOfLine);
        }

        /// <summary>
        /// This is very inaccurate approximation, do not expect accuracy!
        /// </summary>
        /// <param name="pos">Shooting position</param>
        /// <param name="rot">Shooting rotation</param>
        /// <param name="velocity">Starting velocity</param>
        /// <param name="torque">Starting torque</param>
        /// <param name="drag">Rigidbody drag</param>
        /// <param name="pointsOfLine">Max points of line</param>
        /// <returns>Pretty inaccurate approximation</returns>
        public static Vector3[] PredictTrajectory(Vector3 pos, Quaternion rot, Vector3 velocity, Vector3 torque,
            float drag, int pointsOfLine)
        {
            const float dt = 0.02F;
            velocity = rot * velocity;

            var hopUpStr = torque.x * dt * 9F; // Super magic number. dunno why but it works so...
            var initRotUp = rot * Vector3.up;
            var result = new Vector3[pointsOfLine];

            drag = Mathf.Clamp01(1F - (drag * dt));
            result[0] = pos;
            for (var i = 1; i < pointsOfLine; i++)
            {
                // Apply Hop Up
                velocity += initRotUp * (new Vector3(velocity.x, 0, velocity.z).magnitude * hopUpStr * dt);

                // Apply Gravity
                velocity += Physics.gravity * dt;

                // Apply Drag
                velocity *= drag;

                var nextPosition = pos + velocity * dt;

                result[i] = nextPosition;
                pos = nextPosition;
            }

            return result;
        }
    }
}