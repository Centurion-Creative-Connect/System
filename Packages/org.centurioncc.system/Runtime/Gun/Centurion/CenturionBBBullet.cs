using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using System;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gun.Centurion
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionBBBullet : ProjectileBase
    {
        private const float HopUpCoefficient = .005F;
        private const float DampingCoefficient = 2.5F;

        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField] [NewbieInject]
        private RicochetHandlerBase[] ricochetHandlers;

        [SerializeField]
        private TrailRenderer trailRenderer;

        [SerializeField]
        private TrailRenderer debugTrailRenderer;

        [SerializeField] [NewbieInject(SearchScope.Self)]
        private Rigidbody rb;

        private float _damageAmount;
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
        private bool UseTrail => gunManager.UseBulletTrail && _nextUseTrail;
        private bool UseDebugTrail => gunManager.UseDebugBulletTrail;

        public override Guid EventId => _eventId;

        public override bool ShouldApplyDamage =>
            gunManager.AllowedRicochetCount + 1 >= _ricochetCount;

        public override int DamagerPlayerId => _damagerPlayerId;
        public override Vector3 DamageOriginPosition => _damageOriginPos;
        public override Quaternion DamageOriginRotation => _damageOriginRot;
        public override DateTime DamageOriginTime => _damageOriginTime;
        public override string DamageType => _damageType;
        public override float DamageAmount => _damageAmount;

        public void Start()
        {
            gameObject.SetActive(false);
            rb.Sleep();
        }

        private void FixedUpdate()
        {
            // Apply HopUp
            var vel = rb.velocity;
            rb.AddForce(
                _initialRotationUp * (new Vector3(vel.x, 0, vel.z).magnitude * _hopUpStrength),
                ForceMode.Force
            );
        }

        private void OnCollisionEnter(Collision collision)
        {
            ++_ricochetCount;
            rb.velocity /= DampingCoefficient;
            foreach (var ricochetHandler in ricochetHandlers)
            {
                ricochetHandler.OnRicochet(this, collision);
            }
        }

        public override void Shoot(
            Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, float damageAmount,
            DateTime time, int playerId,
            float trailTime, Gradient trailGradient, float lifeTimeInSeconds)
        {
            // Damage data
            _eventId = eventId;
            _damageOriginPos = pos;
            _damageOriginRot = rot;
            _damageOriginTime = time;
            _damageType = $"BBBullet: {damageType}";
            _damageAmount = damageAmount;
            _damagerPlayerId = playerId;

            // Speed data
            rb.drag = drag;

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

            // Move to shooting pos/rot
            transform.SetPositionAndRotation(pos, rot);

            // Setup rigidbody
            rb.velocity = _damageOriginRot * _nextVelocity;
            rb.angularVelocity = _damageOriginRot * _nextTorque;

            // Reset ricochet count
            _ricochetCount = 0;

            // Wake things up
            gameObject.SetActive(true);
            rb.WakeUp();
            SetTrailRendererEmission(true);

            // FIXME: when the reuse occurred, Deactivate will be called very early on because of previously scheduled call 
            // Schedule lifetime destroy
            SendCustomEventDelayedSeconds(nameof(Deactivate), lifeTimeInSeconds);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);

            _ricochetCount = int.MaxValue;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();

            SetTrailRendererEmission(false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void SetTrailRendererEmission(bool emit)
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
    }

    public static class CenturionBBBulletUtility
    {
        /// <summary>
        /// This is a very inaccurate approximation, do not expect accuracy!
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
                out _,
                out _,
                out _,
                out _
            );

            var tempRot = startingRot * rotOffset;
            var tempPos = startingPos + (tempRot * posOffset);
            return PredictTrajectory(tempPos, tempRot, velocity, torque, drag, pointsOfLine);
        }

        /// <summary>
        /// This is a very inaccurate approximation, do not expect accuracy!
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
