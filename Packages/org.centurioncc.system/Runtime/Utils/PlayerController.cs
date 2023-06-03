using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Utils
{
    /// <summary>
    /// Expands VRChats player feature.
    /// </summary>
    /// <remarks>
    /// This class currently manipulates 3 features by checking player's walking surface:
    /// - adjusting player's movement speed by environmental effect and holding objects weight.
    /// - playing footstep sound when walking fast enough.
    /// - snaps player onto ground (by adjusting gravity accordingly) if walking on slopes
    /// </remarks>
    /// <seealso cref="ObjectMarker"/>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerController : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private bool _canRun = true;

        private ObjectMarkerBase[] _currentPickupObjects = new ObjectMarkerBase[0];
        private ObjectMarkerBase _currentSurfaceObject;

        private Vector3 _footstepLastCheckedPosition;
        private float _footstepLastInvokedTime;
        private RaycastHit _hit;
        private bool _isApplyingGroundSnap;
        private float _lastGroundSnapUpdatedTime;
        private bool _lastSurfaceNoFootstep;

        private float _lastSurfaceUpdatedTime;

        private VRCPlayerApi _localPlayer;
        private Ray _ray;
        private Vector3 _vel;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            if (snapPlayerToGroundOnSlopes)
                UpdateGroundSnap();

            if (UpdateTimer())
            {
                UpdateCurrentSurface();

                var lastCanRun = _canRun;

                UpdateCanRunState();

                if (lastCanRun != _canRun)
                {
                    Debug.Log($"[PlayerController] CanRun was updated: {_canRun}");
                    UpdateLocalVrcPlayer();
                }
            }

            if (playFootstepSound && !_lastSurfaceNoFootstep)
                UpdateFootstep();
        }

        private bool UpdateTimer()
        {
            if (surfaceUpdateFrequency < Time.timeSinceLevelLoad - _lastSurfaceUpdatedTime)
            {
                _lastSurfaceUpdatedTime = Time.timeSinceLevelLoad;
                return true;
            }

            return false;
        }

        private void UpdateCurrentSurface()
        {
            _ray = new Ray(_localPlayer.GetPosition() + new Vector3(0f, .1F, 0F), Vector3.down);
            if (!Physics.Raycast(_ray, out _hit, 3, surfaceCheckingLayer))
                return;

            var objMarker = _hit.transform.GetComponent<ObjectMarkerBase>();
            var hasChanged = objMarker != _currentSurfaceObject;
            if (hasChanged)
                Debug.Log(
                    $"[PlayerController] ObjMarker changed: {(_currentSurfaceObject != null ? _currentSurfaceObject.name : null)} to {(objMarker != null ? objMarker.name : null)}");

            _currentSurfaceObject = objMarker;

            if (hasChanged && _currentSurfaceObject != null)
            {
                EnvironmentEffectMultiplier = _currentSurfaceObject.WalkingSpeedMultiplier;
                _lastSurfaceNoFootstep = _currentSurfaceObject.Tags.ContainsString("NoFootstep");
            }
        }

        private void UpdateCanRunState()
        {
            if (!checkGunDirectionToAllowRunning || gunManager.LocalHeldGuns.Length == 0)
            {
                _canRun = true;
                return;
            }

            _canRun = true;
            foreach (var gun in gunManager.LocalHeldGuns)
            {
                var dot = Vector3.Dot(Vector3.up, gun.transform.forward);
                if (dot < gunDirectionUpperBound && dot > gunDirectionLowerBound)
                {
                    _canRun = false;
                    break;
                }
            }
        }

        private void UpdateFootstep()
        {
            var localPlayerPos = _localPlayer.GetPosition();

            // Is total multiplier not zero?
            if (TotalMultiplier == 0)
                return;

            // Did player traveled one step distance?
            if (Vector3.Distance(_footstepLastCheckedPosition, localPlayerPos) <
                footstepDistance * TotalMultiplier) return;

            // Is player airborne?
            if (!_localPlayer.IsPlayerGrounded())
                return;

            var timeDiff = (Time.time - _footstepLastInvokedTime) * TotalMultiplier;
            _footstepLastInvokedTime = Time.time;
            _footstepLastCheckedPosition = localPlayerPos;

            // Did player traveled fast enough to play footstep?
            if (footstepTime < timeDiff || _currentSurfaceObject == null) return;

            var playerBase = playerManager.GetLocalPlayer();
            if (playerBase == null || playerManager.IsStaffTeamId(playerBase.TeamId))
                return;

            var isSlow = footstepSlowThresholdTime < timeDiff;
            var methodName = GetFootstepMethodName(_currentSurfaceObject.ObjectType, isSlow);
            playerBase.SendCustomNetworkEvent(NetworkEventTarget.All, methodName);
        }

        private void UpdateGroundSnap()
        {
            // If we've been applying ground snap for period of time, remove that effect by setting gravity to default.
            if (_isApplyingGroundSnap && _lastGroundSnapUpdatedTime < Time.timeSinceLevelLoad)
            {
                // Debug.Log($"[PlayerController] GroundSnap: Reset");
                UpdateLocalVrcPlayer();
                _isApplyingGroundSnap = false;
            }

            _vel = _localPlayer.GetVelocity();

            // We're going upwards, don't check ground.
            if (0.01F < _vel.y)
            {
                // Debug.Log($"[PlayerController] GroundSnap: {_isApplyingGroundSnap} Going Upwards");
                return;
            }

            // Player is already airborne, don't check ground.
            if (!_localPlayer.IsPlayerGrounded() && !_isApplyingGroundSnap)
            {
                // Debug.Log("[PlayerController] GroundSnap: Not Grounded");
                return;
            }

            // Make it 2D normalized vector to easier use
            _vel = (new Vector3(_vel.x, 0F, _vel.z).normalized) * groundSnapForwardDistance;

            _ray = new Ray(_localPlayer.GetPosition() + Vector3.up + _vel, Vector3.down);
            if (!Physics.Raycast(_ray, out _hit, groundSnapMaxDistance + 1, playerGroundLayer))
            {
                // Debug.DrawRay(_ray.origin, _ray.direction * (groundSnapMaxDistance + 1), Color.red, 5F);
                // Debug.Log(
                //     $"[PlayerController] GroundSnap: {_isApplyingGroundSnap} Ray Not Hit: {_vel.ToString("F2")}");
                if (_isApplyingGroundSnap)
                {
                    // Debug.Log("[PlayerController] GroundSnap: Abort");
                    UpdateLocalVrcPlayer();
                    _isApplyingGroundSnap = false;
                }

                return;
            }


            // If hit was occuring at roughly same place, don't begin snapping.
            if (_hit.distance <= 1.0075F)
            {
                // Debug.DrawRay(_ray.origin, _ray.direction * _hit.distance, Color.cyan, 5F);
                return;
            }

            // Debug.DrawRay(_ray.origin, _ray.direction * _hit.distance, Color.green, 5F);

            _localPlayer.SetGravityStrength(100F);
            _lastGroundSnapUpdatedTime = Time.timeSinceLevelLoad + .5F;
            _isApplyingGroundSnap = true;
            // Debug.Log($"[PlayerController] GroundSnap: Begin: {_hit.distance}");
        }

        private static string GetFootstepMethodName(ObjectType type, bool isSlow)
        {
            var format = isSlow ? "PlaySlow{0}FootstepAudio" : "Play{0}FootstepAudio";
            switch (type)
            {
                case ObjectType.Prototype:
                default:
                    return string.Format(format, "Prototype");
                case ObjectType.Gravel:
                    return string.Format(format, "Gravel");
                case ObjectType.Wood:
                    return string.Format(format, "Wood");
                case ObjectType.Metallic:
                    return string.Format(format, "Metallic");
                case ObjectType.Dirt:
                    return string.Format(format, "Dirt");
                case ObjectType.Concrete:
                    return string.Format(format, "Concrete");
            }
        }

        /// <summary>
        /// Syncs and applies locals base properties globally. 
        /// </summary>
        [PublicAPI]
        public void Sync()
        {
            Debug.Log($"[PlayerController] Syncing base properties: \n" +
                      $"WalkSpeed  : {BaseWalkSpeed}\n" +
                      $"RunSpeed   : {BaseRunSpeed}\n" +
                      $"StrafeSpeed: {BaseStrafeSpeed}\n" +
                      $"JumpImpulse: {BaseJumpImpulse}\n" +
                      $"GravityStrength: {BaseGravityStrength}");
            Networking.SetOwner(_localPlayer, gameObject);
            RequestSerialization();
        }

        /// <summary>
        /// Applies current movement speed to local player.
        /// </summary>
        [PublicAPI]
        public void UpdateLocalVrcPlayer()
        {
            if (_localPlayer == null || !_localPlayer.IsValid())
                return;

            // Debug.Log($"[PlayerController] Applying actual properties: \n" +
            //           $"WalkSpeed  : {ActualWalkSpeed}\n" +
            //           $"RunSpeed   : {ActualRunSpeed}\n" +
            //           $"StrafeSpeed: {ActualStrafeSpeed}\n" +
            //           $"JumpImpulse: {ActualJumpImpulse}\n" +
            //           $"GravityStrength: {ActualGravityStrength}");

            _localPlayer.SetWalkSpeed(ActualWalkSpeed);
            _localPlayer.SetRunSpeed(ActualRunSpeed);
            _localPlayer.SetStrafeSpeed(ActualStrafeSpeed);
            _localPlayer.SetJumpImpulse(ActualJumpImpulse);
            _localPlayer.SetGravityStrength(ActualGravityStrength);
        }

        /// <summary>
        /// Adds object's weight to player's current weight.
        /// </summary>
        /// <param name="anObject">an object which begin holding.</param>
        public void AddHoldingObject(ObjectMarkerBase anObject)
        {
            _currentPickupObjects = _currentPickupObjects.AddAsSet(anObject);
            UpdateHoldingObjects();
        }

        /// <summary>
        /// Subtracts object's weight from player's current weight.
        /// </summary>
        /// <param name="anObject">an object which stopped holding.</param>
        public void RemoveHoldingObject(ObjectMarkerBase anObject)
        {
            _currentPickupObjects = _currentPickupObjects.RemoveItem(anObject);
            UpdateHoldingObjects();
        }

        /// <summary>
        /// Update <see cref="PlayerWeight"/> by currently known held objects.
        /// </summary>
        /// <remarks>
        /// This also updates current VrcPlayer. see <see cref="UpdateLocalVrcPlayer"/>.
        /// </remarks>
        /// <seealso cref="UpdateLocalVrcPlayer"/>
        public void UpdateHoldingObjects()
        {
            var totalWeight = 0F;
            foreach (var o in _currentPickupObjects)
                if (o != null)
                    totalWeight += o.ObjectWeight;
            PlayerWeight = totalWeight;
        }

        /// <summary>
        /// Resets player's weight.
        /// </summary>
        /// <remarks>
        /// This method can be used to ensure no objects are left held.
        /// </remarks>
        public void RemoveAllHoldingObject()
        {
            _currentPickupObjects = new ObjectMarkerBase[0];
            UpdateHoldingObjects();
        }

        #region Base

        [Tooltip("Delay in seconds until try to update current surface object marker.")]
        [SerializeField]
        private float surfaceUpdateFrequency = 0.5F;
        [Tooltip("Layers to check objects with ObjectMarker attached.")]
        [SerializeField]
        private LayerMask surfaceCheckingLayer = 1 << 11;

        #endregion

        #region GroundSnapping

        [SerializeField]
        public bool snapPlayerToGroundOnSlopes = true;
        [SerializeField]
        private LayerMask playerGroundLayer = (1 | 1 << 9 | 1 << 11);
        [SerializeField]
        public float groundSnapMaxDistance = 0.4F;
        [SerializeField]
        public float groundSnapForwardDistance = 0.2F;

        #endregion

        #region PlayerMovement

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseWalkSpeed))]
        private float baseWalkSpeed = 2F;
        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseRunSpeed))]
        private float baseRunSpeed = 4F;
        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseStrafeSpeed))]
        private float baseStrafeSpeed = 2F;
        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseJumpImpulse))]
        private float baseJumpImpulse = 0;
        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseGravityStrength))]
        private float baseGravityStrength = 1F;

        [SerializeField]
        private float playerWeight;
        [SerializeField]
        public float maximumCarryingWeightInKilogram = 75F;
        [SerializeField]
        private float environmentEffectMultiplier = 1F;
        [SerializeField]
        public bool checkGunDirectionToAllowRunning = false;
        [SerializeField] [Range(0, 1F)]
        public float gunDirectionUpperBound = 0.7F;
        [SerializeField] [Range(-1F, 0)]
        public float gunDirectionLowerBound = -0.7F;

        #endregion

        #region Footstep

        [SerializeField]
        private bool playFootstepSound = true;
        [SerializeField] [Range(0, 2)] [UdonSynced]
        [Tooltip("Plays footstep sound if player went this amount of units away.")]
        public float footstepDistance = 1F;
        [SerializeField] [Range(0, 2)] [UdonSynced]
        [Tooltip("Plays footstep sound if player went footstepDistance far for this time, in seconds.")]
        public float footstepTime = 0.9F;
        [SerializeField] [Range(0, 2)] [UdonSynced]
        [Tooltip("Plays slow footstep sound if player invoked footstep sound after this time, in seconds.")]
        public float footstepSlowThresholdTime = 0.45F;

        #endregion

        #region PlayerMovementProperties

        [PublicAPI]
        public float BaseWalkSpeed
        {
            get => baseWalkSpeed;
            set
            {
                baseWalkSpeed = value;
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float BaseRunSpeed
        {
            get => baseRunSpeed;
            set
            {
                baseRunSpeed = value;
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float BaseStrafeSpeed
        {
            get => baseStrafeSpeed;
            set
            {
                baseStrafeSpeed = value;
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float BaseJumpImpulse
        {
            get => baseJumpImpulse;
            set
            {
                baseJumpImpulse = value;
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float BaseGravityStrength
        {
            get => baseGravityStrength;
            set
            {
                baseGravityStrength = value;
                UpdateLocalVrcPlayer();
            }
        }

        /// <summary>
        /// In unit of kilogram
        /// </summary>
        [PublicAPI]
        public float PlayerWeight
        {
            get => playerWeight;
            protected set
            {
                playerWeight = Mathf.Clamp(value, 0F, maximumCarryingWeightInKilogram);
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float EnvironmentEffectMultiplier
        {
            get => environmentEffectMultiplier;
            protected set
            {
                environmentEffectMultiplier = value;
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float CustomEffectMultiplier { get; set; } = 1F;

        [PublicAPI]
        public float TotalMultiplier => (1 - (PlayerWeight / maximumCarryingWeightInKilogram)) *
                                        EnvironmentEffectMultiplier * CustomEffectMultiplier;

        [PublicAPI]
        public bool CanRun => _canRun;

        [PublicAPI]
        public float ActualWalkSpeed => BaseWalkSpeed * TotalMultiplier;

        [PublicAPI]
        public float ActualRunSpeed => CanRun ? BaseRunSpeed * TotalMultiplier : ActualWalkSpeed;

        [PublicAPI]
        public float ActualStrafeSpeed => CanRun ? BaseStrafeSpeed * TotalMultiplier : ActualWalkSpeed;

        [PublicAPI]
        public float ActualJumpImpulse => BaseJumpImpulse * TotalMultiplier;

        [PublicAPI]
        public float ActualGravityStrength => BaseGravityStrength * TotalMultiplier;

        #endregion
    }
}