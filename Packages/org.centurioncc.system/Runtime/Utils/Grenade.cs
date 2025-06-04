using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using DerpyNewbie.Common.ObjectPool;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [RequireComponent(typeof(VRCPickup))]
    public class Grenade : UdonSharpBehaviour
    {
        private const string TriggerLeft = "Oculus_CrossPlatform_PrimaryIndexTrigger";
        private const string TriggerRight = "Oculus_CrossPlatform_SecondaryIndexTrigger";

        private const string AnimParamHasSafetyLever = "HasSafetyLever";
        private const string AnimParamHasSafetyPin = "HasSafetyPin";
        private const string AnimParamHasSafetyPinHeld = "HasSafetyPinHeld";
        private const string AnimParamSafetyPinPull = "SafetyPinPull";

        [SerializeField] [HideInInspector] [NewbieInject]
        private GrenadeManager grenadeManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private ProjectilePool projectilePool;

        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private AudioManager audioManager;

        [SerializeField] private ObjectPoolProxy objectPoolProxy;
        [SerializeField] private Animator animator;
        [SerializeField] private ProjectileDataProvider projectileData;
        [SerializeField] private HolsterableObject holsterable;
        [SerializeField] private Transform safetyPinReference;
        [SerializeField] private Transform[] shootingOffsets;

        [SerializeField]
        private float safetyPinPullDistance = .1F;

        [SerializeField]
        private float safetyPinInteractionProximity = .1F;

        [SerializeField]
        private bool safetyPinPullDirectionCheck;

        [SerializeField] [InspectorName("Weapon Name (Damage Type)")]
        private string damageType = "Grenade";

        [Header("Trigger Settings: Timed")]
        [SerializeField]
        private bool useTimedTrigger = true;

        [SerializeField] [Tooltip("Delay after safety lever has released. in seconds")]
        private float timedTriggerDelay = 3;

        [Header("Trigger Settings: Impact")]
        [SerializeField]
        private bool useImpactTrigger;

        [SerializeField] [Tooltip("Threshold of impact triggering in relative velocity magnitude")]
        private float impactTriggerThreshold = 0.2F;

        [SerializeField] [Tooltip("Delay after impact occurred. in seconds")]
        private float impactTriggerDelay;

        [SerializeField] [Tooltip("Ignoring impacts caused with specified colliders")]
        private Collider[] impactTriggerExclusions;

        [Header("Explosion Settings")]
        [SerializeField] private Vector3 explosionRelativeTorque = new Vector3(1.5F, .3F, 0F);

        [SerializeField] private Vector3 explosionTorque = new Vector3(0, 1F, 0);
        [SerializeField] private Vector3 explosionRelativeForce = Vector3.down * 0.1F;
        [SerializeField] private Vector3 explosionForce = new Vector3(0, 0.5F, 0);

        [SerializeField] [Tooltip("Duration of explosion force in seconds")]
        private float explosionDuration = 0.5F;

        [SerializeField] [Tooltip("Must be non-zero or else it'll throw division by zero")]
        private float explosionShootingInterval = 0.01F;

        [Header("Audio Settings")]
        [SerializeField] private AudioDataStore pinPulledAudio;

        [FormerlySerializedAs("leverReleasedAudio")] [SerializeField]
        private AudioDataStore safetyLeverReleasedAudio;

        [SerializeField] private AudioDataStore explosionAudio;

        [Header("Mouth Interaction")]
        [SerializeField]
        private bool useMouthSafetyPinInteraction = true;

        [SerializeField]
        private Vector3 mouthPositionOffset = Vector3.forward * 0.05F;

        [Header("Debug Settings")]
        [SerializeField]
        private bool useDebugTrails;

        private readonly int _hashedHasSafetyLever = Animator.StringToHash(AnimParamHasSafetyLever);
        private readonly int _hashedHasSafetyPin = Animator.StringToHash(AnimParamHasSafetyPin);
        private readonly int _hashedHasSafetyPinHeld = Animator.StringToHash(AnimParamHasSafetyPinHeld);
        private readonly int _hashedSafetyPinPull = Animator.StringToHash(AnimParamSafetyPinPull);

        private ContactPoint[] _contactPoints = new ContactPoint[2];

        private float _currentExplosionInterval;

        private Guid _eventId;

        private float _explosionTimer;
        private bool _hasExploded;
        private bool _hasSafetyLever;

        private bool _hasSafetyPin;
        private bool _isExploding;
        private bool _isHeld;
        private bool _isSafetyPinHeld;
        private Vector3 _lastShotPos;
        private Quaternion _lastShotRot;
        private float _lastShotTime;
        private VRC_Pickup _pickup;
        private Rigidbody _rb;
        private float _safetyPinPullProgress;

        private void Start()
        {
            _pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            _rb = GetComponent<Rigidbody>();
            _rb.maxAngularVelocity = 100F;

            _hasSafetyPin = true;
            _hasSafetyLever = true;
        }

        private void Update()
        {
            _UpdateAnimation();

            if (_isHeld) _CheckInteraction();

            if (!_isExploding) return;

            var max = Mathf.CeilToInt((_lastShotTime - _explosionTimer) / _currentExplosionInterval);
            var count = 0;
            for (; _explosionTimer < _lastShotTime; _lastShotTime -= _currentExplosionInterval)
            {
                ++count;
                var t = transform;
                var d = (float)count / max;
                var iPos = Vector3.Lerp(_lastShotPos, t.position, d);
                var iRot = Quaternion.Lerp(_lastShotRot, t.rotation, d);

                projectileData.Get(
                    0,
                    out var posOffset,
                    out var velocity,
                    out var rotOffset,
                    out var torque,
                    out var drag,
                    out var trailTime,
                    out var trailCol,
                    out var lifeTimeInSeconds
                );

                var grad = trailCol;
                if (useDebugTrails)
                {
                    var c = Color.Lerp(Color.red, Color.blue, d);
                    grad = new Gradient();
                    grad.SetKeys(new[] { new GradientColorKey(c, 0), new GradientColorKey(c, 1) },
                        new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                }

                foreach (var offset in shootingOffsets)
                {
                    var wtlMatrix = offset.worldToLocalMatrix;
                    var offIPos = wtlMatrix.MultiplyPoint3x4(iPos);
                    var offIRot = iRot * Quaternion.Inverse(wtlMatrix.rotation);
                    var proj = projectilePool.Shoot(
                        _eventId,
                        offset.position + offIPos + posOffset,
                        offset.rotation * offIRot * rotOffset,
                        velocity,
                        torque,
                        drag,
                        damageType,
                        Networking.GetNetworkDateTime(),
                        Networking.GetOwner(gameObject).playerId,
                        trailTime,
                        grad,
                        lifeTimeInSeconds
                    );

                    proj.SetDamageSetting(DetectionType.VictimSide, false, true);
                }
            }

            _explosionTimer -= Time.deltaTime;

            if (_explosionTimer < 0)
            {
                _isExploding = false;
                _hasExploded = true;
                _pickup.pickupable = true;

                SendCustomEventDelayedSeconds(nameof(_RespawnGrenade), 3);
            }
        }

        private void FixedUpdate()
        {
            if (!_isExploding) return;

            _rb.AddRelativeTorque(explosionRelativeTorque);
            _rb.AddTorque(explosionTorque);
            _rb.AddRelativeForce(explosionRelativeForce);
            _rb.AddForce(explosionForce);
        }

        private void OnEnable()
        {
            ResetGrenade();
        }

        private void OnDisable()
        {
            ResetGrenade();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!useImpactTrigger || !Networking.IsOwner(gameObject) || _hasSafetyLever || _isExploding || _hasExploded)
                return;

            if (other.relativeVelocity.magnitude < impactTriggerThreshold) return;

            var contacts = other.GetContacts(_contactPoints);
            for (var i = 0; i < contacts; i++)
            {
                var cp = _contactPoints[i];
                if (!impactTriggerExclusions.ContainsItem(cp.thisCollider)) continue;

                Debug.Log($"[Grenade] Hit exclusion collider: {cp.thisCollider.name}", this);
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(IgniteByImpact));
        }

        public override void OnPickup()
        {
            _isHeld = true;
            grenadeManager.AddLocalGrenade(this);
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override void OnDrop()
        {
            _isHeld = false;
            if (grenadeManager.CanExplode)
            {
                if (!_hasSafetyPin && _hasSafetyLever)
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RemoveSafetyLever));
            }
            else
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetGrenade));
            }

            grenadeManager.RemoveLocalGrenade(this);
        }

        public override void OnPickupUseUp()
        {
            if (!grenadeManager.CanExplode)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetGrenade));
                return;
            }

            if (_hasSafetyPin && !Networking.LocalPlayer.IsUserInVR())
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RemoveSafetyPin));
                return;
            }

            if (!_hasSafetyPin && _hasSafetyLever)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RemoveSafetyLever));
                return;
            }

            if (_hasExploded) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ResetGrenade));
        }

        public void ResetGrenade()
        {
            _hasSafetyPin = true;
            _hasSafetyLever = true;

            _isSafetyPinHeld = false;
            _isExploding = false;
            _hasExploded = false;
            _safetyPinPullProgress = 0;
        }

        public void RemoveSafetyPin()
        {
            _isSafetyPinHeld = false;
            _hasSafetyPin = false;
            _PlayAudio(pinPulledAudio);
        }

        public void RemoveSafetyLever()
        {
            _hasSafetyLever = false;
            if (useTimedTrigger)
                SendCustomEventDelayedSeconds(nameof(_Explode), timedTriggerDelay);
            _PlayAudio(safetyLeverReleasedAudio);
        }

        public void IgniteByImpact()
        {
            if (useImpactTrigger)
                SendCustomEventDelayedSeconds(nameof(_Explode), impactTriggerDelay);
        }

        public void _RespawnGrenade()
        {
            if (Networking.IsMaster)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            if (holsterable != null && holsterable.IsHolsteredLocally)
            {
                holsterable.UnHolster();
            }

            objectPoolProxy.OwnerOnly_Return(gameObject);
            ResetGrenade();
        }

        public void _Explode()
        {
            if (_hasSafetyLever || _isExploding || _hasExploded)
            {
                Debug.LogError(_hasSafetyLever
                    ? "[Grenade] Will not be exploding as safety lever has been reset"
                    : "[Grenade] Will not be exploding as it is already exploding or exploded"
                );
                return;
            }

            _explosionTimer = explosionDuration;
            _lastShotTime = _explosionTimer;
            _eventId = Guid.NewGuid();
            var t = transform;
            _lastShotPos = t.position;
            _lastShotRot = t.rotation;
            _isExploding = true;
            _pickup.pickupable = false;

            var playerPos = Networking.LocalPlayer.GetPosition();
            var grenadePos = transform.position;
            var distance = Vector3.Distance(playerPos, grenadePos);
            var farNearDiff = grenadeManager.BulletReductionFar - grenadeManager.BulletReductionNear;
            var reductionRate =
                Mathf.Clamp(distance - grenadeManager.BulletReductionFar, 0, farNearDiff) / farNearDiff;
            _currentExplosionInterval = explosionShootingInterval + (explosionDuration * reductionRate);

            // Debug.Log($"Reduction Rate: {reductionRate}");

            if (_isHeld) _pickup.Drop();

            _PlayAudio(explosionAudio);
        }

        private void _CheckInteraction()
        {
            if (_hasSafetyPin) _HandleSafetyPinInteraction();
        }

        private void _HandleSafetyPinInteraction()
        {
            if (!_hasSafetyPin) return;

            _safetyPinPullProgress = 0;

            // Check for hand safety pin interaction
            var nonDominantHandTrigger =
                Input.GetAxisRaw(_pickup.currentHand == VRC_Pickup.PickupHand.Left ? TriggerRight : TriggerLeft);
            var nonDominantHandFinger = Networking.LocalPlayer.GetBonePosition(
                _pickup.currentHand == VRC_Pickup.PickupHand.Left
                    ? HumanBodyBones.RightIndexDistal
                    : HumanBodyBones.LeftIndexDistal);

            var fingerHeld =
                _CheckSafetyPinInteraction(nonDominantHandTrigger, nonDominantHandFinger, safetyPinInteractionProximity,
                    _isSafetyPinHeld);

            if (!useMouthSafetyPinInteraction)
            {
                _isSafetyPinHeld = fingerHeld;
                return;
            }

            // Check for head safety pin interaction
            var dominantHandTrigger =
                Input.GetAxisRaw(_pickup.currentHand == VRC_Pickup.PickupHand.Left ? TriggerLeft : TriggerRight);
            var headTrackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var head = headTrackingData.position + (headTrackingData.rotation * mouthPositionOffset);

            var headHeld =
                _CheckSafetyPinInteraction(dominantHandTrigger, head, safetyPinInteractionProximity, _isSafetyPinHeld);

            _isSafetyPinHeld = fingerHeld || headHeld;
        }

        private bool _CheckSafetyPinInteraction(float trigger, Vector3 refPos, float proximity, bool lastPinHeld)
        {
            var distance = Vector3.Distance(safetyPinReference.position, refPos);
            var dot = safetyPinPullDirectionCheck
                ? Vector3.Dot(safetyPinReference.forward, refPos - safetyPinReference.position)
                : 1;

            if (!lastPinHeld)
            {
                if (trigger > 0.2F && dot > 0 && distance < proximity)
                {
                    lastPinHeld = true;
                }

                return lastPinHeld;
            }

            if (trigger < 0.1F)
            {
                return false;
            }

            if (dot > 0)
            {
                _safetyPinPullProgress = distance / safetyPinPullDistance;
            }
            else
            {
                _safetyPinPullProgress = Mathf.Max(_safetyPinPullProgress, 0);
            }

            if (distance < safetyPinPullDistance || dot < 0) return true;

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RemoveSafetyPin));
            return true;
        }

        private void _UpdateAnimation()
        {
            animator.SetBool(_hashedHasSafetyLever, _hasSafetyLever);
            animator.SetBool(_hashedHasSafetyPin, _hasSafetyPin);
            animator.SetBool(_hashedHasSafetyPinHeld, _isSafetyPinHeld);
            animator.SetFloat(_hashedSafetyPinPull, _safetyPinPullProgress);
        }

        private void _PlayAudio([CanBeNull] AudioDataStore dataStore)
        {
            if (dataStore == null) return;

            audioManager.PlayAudioAtTransform(dataStore, transform);
        }
    }
}