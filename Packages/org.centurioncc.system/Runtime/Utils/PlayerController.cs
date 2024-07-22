using System;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
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
    public class PlayerController : GunManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;

        private readonly DataList _activeTags = new DataList();
        private readonly DataList _heldObjects = new DataList();

        private bool _canRun = true;
        private bool _combatTagged = false;

        private float _environmentEffectMultiplier = 1F;

        private Vector3 _footstepLastCheckedPosition;
        private float _footstepLastInvokedTime;
        private RaycastHit _hit;
        private bool _isApplyingGroundSnap;
        private float _lastGroundSnapUpdatedTime;
        private bool _lastSurfaceNoFootstep;
        private float _lastSurfaceUpdatedTime;

        private VRCPlayerApi _localPlayer;

        private float _playerWeight;
        private Ray _ray;
        private bool _updatedSurfaceInFrame = false;
        private Vector3 _vel;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;

            if (gunManager != null) gunManager.SubscribeCallback(this);
        }

        private void Update()
        {
            _updatedSurfaceInFrame = false;

            if (snapPlayerToGroundOnSlopes)
                GroundSnapUpdatePass();

            if (UpdateTimer())
                CurrentSurfaceUpdatePass();

            if (playFootstepSound && !_lastSurfaceNoFootstep)
                FootstepUpdatePass();

            if (baseUseGunSprint)
                GunIntegrationUpdatePass();
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

        private void CurrentSurfaceUpdatePass()
        {
            _updatedSurfaceInFrame = true;
            _ray = new Ray(_localPlayer.GetPosition() + new Vector3(0f, .1F, 0F), Vector3.down);
            if (!Physics.Raycast(_ray, out _hit, 3, surfaceCheckingLayer)) return;

            // Check existing terrain
            var objMarker = TryGetObjectMarker(_hit.transform, out var hasMarkerDataUpdate);
            var hasChanged = objMarker != CurrentSurface || hasMarkerDataUpdate;
            if (!hasChanged) return;
            Debug.Log(
                $"[PlayerController] ObjMarker changed: {(CurrentSurface != null ? CurrentSurface.name : null)} to {(objMarker != null ? objMarker.name : null)}");
            if (hasMarkerDataUpdate)
                Debug.Log($"[PlayerController] ObjMarkerData was updated: {objMarker.ObjectType}");

            // If marker data was updated, its tags are already removed at TryGetObjectMarker
            if (CurrentSurface != null && !hasMarkerDataUpdate)
                foreach (var surfTag in CurrentSurface.Tags)
                    _activeTags.Remove(surfTag);

            var lastSurf = CurrentSurface;
            CurrentSurface = objMarker;

            if (CurrentSurface != null)
            {
                foreach (var surfTag in CurrentSurface.Tags)
                    _activeTags.Add(surfTag);

                EnvironmentEffectMultiplier = CurrentSurface.WalkingSpeedMultiplier;
                _lastSurfaceNoFootstep = CurrentSurface.Tags.ContainsString("NoFootstep");
            }
            else
            {
                EnvironmentEffectMultiplier = 1;
                _lastSurfaceNoFootstep = true;
            }

            Invoke_OnActiveTagsUpdated();
            Invoke_OnSurfaceUpdated(lastSurf, CurrentSurface);
        }

        private ObjectMarkerBase TryGetObjectMarker(Component o, out bool hasMarkerDataUpdate)
        {
            var terrainMarker = o.GetComponent<TerrainMarker>();
            if (terrainMarker != null)
            {
                hasMarkerDataUpdate = terrainMarker.UpdateObjectMarkerInfo(_hit.point);
                if (CurrentSurface == terrainMarker && hasMarkerDataUpdate)
                {
                    foreach (var prevTag in terrainMarker.PreviousTags)
                        ActiveTags.Remove(prevTag);
                }

                return terrainMarker;
            }

            hasMarkerDataUpdate = false;
            return o.GetComponent<ObjectMarkerBase>();
        }

        private void FootstepUpdatePass()
        {
            var localPlayerPos = _localPlayer.GetPosition();

            // Is total multiplier not zero?
            if (TotalMultiplier == 0) return;

            // Did player traveled one step distance?
            if (Vector3.Distance(_footstepLastCheckedPosition, localPlayerPos) <
                footstepDistance * TotalMultiplier) return;

            // Is player airborne?
            if (!_localPlayer.IsPlayerGrounded()) return;

            var timeDiff = (Time.time - _footstepLastInvokedTime) * TotalMultiplier;
            _footstepLastInvokedTime = Time.time;
            _footstepLastCheckedPosition = localPlayerPos;

            // Did player traveled fast enough to play footstep?
            if (footstepTime < timeDiff) return;

            if (!_updatedSurfaceInFrame) CurrentSurfaceUpdatePass();

            if (CurrentSurface == null) return;

            var playerBase = playerManager.GetLocalPlayer();
            if (playerBase == null || playerManager.IsStaffTeamId(playerBase.TeamId)) return;

            var isSlow = footstepSlowThresholdTime < timeDiff;
            var methodName = GetFootstepMethodName(CurrentSurface.ObjectType, isSlow);
            playerBase.SendCustomNetworkEvent(NetworkEventTarget.All, methodName);
        }

        private void GroundSnapUpdatePass()
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
            if (0.01F < _vel.y) return;

            // Player is already airborne, don't check ground.
            if (!_localPlayer.IsPlayerGrounded() && !_isApplyingGroundSnap) return;

            // Make it 2D normalized vector to easier use
            _vel = (new Vector3(_vel.x, 0F, _vel.z).normalized) * groundSnapForwardDistance;

            _ray = new Ray(_localPlayer.GetPosition() + Vector3.up + _vel, Vector3.down);
            if (!Physics.Raycast(_ray, out _hit, groundSnapMaxDistance + 1, playerGroundLayer))
            {
                if (_isApplyingGroundSnap)
                {
                    UpdateLocalVrcPlayer();
                    _isApplyingGroundSnap = false;
                }

                return;
            }

            // If hit was occuring at roughly same place, don't begin snapping.
            if (_hit.distance <= 1.0075F) return;

            _localPlayer.SetGravityStrength(100F);
            _lastGroundSnapUpdatedTime = Time.timeSinceLevelLoad + .5F;
            _isApplyingGroundSnap = true;
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

        #region BasePublicAPIs

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
            if (_localPlayer == null || !_localPlayer.IsValid()) return;

            Invoke_OnPreApplyMovementProperties();

            _localPlayer.SetWalkSpeed(ActualWalkSpeed);
            _localPlayer.SetRunSpeed(ActualRunSpeed);
            _localPlayer.SetStrafeSpeed(ActualStrafeSpeed);
            _localPlayer.SetJumpImpulse(ActualJumpImpulse);
            _localPlayer.SetGravityStrength(ActualGravityStrength);

            Invoke_OnPostApplyMovementProperties();
        }

        #endregion

        #region ObjectBaseHandlingAPIs

        /// <summary>
        /// Adds object's weight to player's current weight.
        /// </summary>
        /// <param name="anObject">an object which begin holding.</param>
        public void AddHoldingObject(ObjectMarkerBase anObject)
        {
            if (anObject == null)
            {
                Debug.LogWarning("[PlayerController] Tried to add null to held object list!");
                return;
            }

            if (_heldObjects.Contains(anObject) && !allowDuplicateHeldObjects)
            {
                Debug.LogWarning(
                    $"[PlayerController] Tried to add {anObject.name}, but it already exists in held objects list!");
                return;
            }

            _heldObjects.Add(anObject);
            foreach (var objectTag in anObject.Tags) _activeTags.Add(objectTag);
            UpdateHoldingObjects();
            Invoke_OnActiveTagsUpdated();
            Invoke_OnHeldObjectsUpdated();
        }

        /// <summary>
        /// Subtracts object's weight from player's current weight.
        /// </summary>
        /// <param name="anObject">an object which stopped holding.</param>
        public void RemoveHoldingObject(ObjectMarkerBase anObject)
        {
            if (anObject == null)
            {
                Debug.LogWarning("[PlayerController] Tried to remove null from held objects list!");
                return;
            }

            _heldObjects.Remove(anObject);
            foreach (var objectTag in anObject.Tags) _activeTags.Remove(objectTag);
            UpdateHoldingObjects();
            Invoke_OnActiveTagsUpdated();
            Invoke_OnHeldObjectsUpdated();
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
            var arr = _heldObjects.ToArray();
            foreach (var o in arr)
                if (((ObjectMarkerBase)o.Reference) != null)
                    totalWeight += ((ObjectMarkerBase)o.Reference).ObjectWeight;
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
            _heldObjects.Clear();
            _activeTags.Clear();
            UpdateHoldingObjects();

            if (CurrentSurface == null) return;

            foreach (var surfTags in CurrentSurface.Tags) _activeTags.Add(surfTags);
            Invoke_OnActiveTagsUpdated();
            Invoke_OnHeldObjectsUpdated();
        }

        #endregion

        #region BaseSerializedFields

        [Tooltip("Delay in seconds until try to update current surface object marker.")] [SerializeField]
        private float surfaceUpdateFrequency = 0.25F;

        [Tooltip("Layers to check objects with ObjectMarker attached.")] [SerializeField]
        private LayerMask surfaceCheckingLayer = 1 << 11;

        #endregion

        #region PublicAPIProperties

        /// <summary>
        /// Current surface <see cref="ObjectMarkerBase"/> used to determine footsteps and environment multipliers
        /// </summary>
        [PublicAPI]
        public ObjectMarkerBase CurrentSurface { get; private set; }

        /// <summary>
        /// Current held objects list of <see cref="ObjectMarkerBase"/>s
        /// </summary>
        /// <remarks>
        /// To use contents, get <see cref="DataToken.Reference"/> and cast it to <see cref="ObjectMarkerBase"/>.
        /// </remarks>
        /// <example>
        /// <code>((ObjectMarkerBase)HeldObjects[0].Reference).ObjectType</code>
        /// </example>
        [PublicAPI]
        public DataList HeldObjects => _heldObjects.DeepClone();

        /// <summary>
        /// Current effective <see cref="string"/> tags
        /// </summary>
        /// <example>
        /// <code>ActiveTags[0].String</code>
        /// </example>
        /// <seealso cref="ObjectMarkerBase.Tags"/>
        [PublicAPI]
        public DataList ActiveTags => _activeTags.DeepClone();

        #endregion

        #region GroundSnapping

        [Header("Ground Snapping")] [SerializeField]
        public bool snapPlayerToGroundOnSlopes = true;

        [SerializeField] private LayerMask playerGroundLayer = (1 | 1 << 9 | 1 << 11);
        [SerializeField] public float groundSnapMaxDistance = 0.4F;
        [SerializeField] public float groundSnapForwardDistance = 0.2F;

        #endregion

        #region PlayerMovementSerializeFields

        [Header("Base Player Movement")] [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseWalkSpeed))]
        private float baseWalkSpeed = 2F;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseRunSpeed))]
        private float baseRunSpeed = 4F;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseStrafeSpeed))]
        private float baseStrafeSpeed = 2F;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseJumpImpulse))]
        private float baseJumpImpulse;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(BaseGravityStrength))]
        private float baseGravityStrength = 1F;

        [SerializeField] [UdonSynced] [FieldChangeCallback(nameof(CanRun))]
        private bool baseCanRun = true;

        [Header("ObjectMarker Pickups")] [SerializeField]
        public float maximumCarryingWeightInKilogram = 75F;

        [SerializeField]
        [Tooltip("Compatibility option.\n" +
                 "Allows duplicate occuring for `PlayerController#AddHeldObject` method.\n" +
                 "Leave it unchecked if you are not experiencing issues.")]
        private bool allowDuplicateHeldObjects;

        #endregion

        #region FootstepSerializeFields

        [Header("Footstep")] [SerializeField] [UdonSynced]
        private bool playFootstepSound = true;

        [SerializeField]
        [Range(0, 2)]
        [UdonSynced]
        [Tooltip("Plays footstep sound if player went this amount of units away.")]
        public float footstepDistance = 1F;

        [SerializeField]
        [Range(0, 2)]
        [UdonSynced]
        [Tooltip("Plays footstep sound if player went footstepDistance far for this time, in seconds.")]
        public float footstepTime = 0.9F;

        [SerializeField]
        [Range(0, 2)]
        [UdonSynced]
        [Tooltip("Plays slow footstep sound if player invoked footstep sound after this time, in seconds.")]
        public float footstepSlowThresholdTime = 0.45F;

        #endregion

        #region GunIntegrationSerializeFields

        [Header("Options")] [SerializeField] private bool baseApplyGunPropertyToPlayerController = true;

        [Header("Gun Sprint")] [SerializeField] [UdonSynced] [FormerlySerializedAs("checkGunDirectionToAllowRunning")]
        private bool baseUseGunSprint = true;

        [SerializeField] [UdonSynced] private float baseGunWalkSpeed = 1F;
        [SerializeField] [UdonSynced] private float baseGunSprintSpeed = 2F;

        [SerializeField] [UdonSynced] [Range(0, 1F)] [FormerlySerializedAs("gunDirectionDotThreshold")]
        private float baseGunDirectionThreshold = 0.88F;

        [Header("Combat Tag")]
        [SerializeField]
        [UdonSynced]
        [Tooltip("\"CombatTag\" refers to a slow down when player has started shooting")]
        private bool baseUseCombatTag = true;

        [SerializeField] [UdonSynced] private float baseCombatTagSpeedMultiplier = 0.5F;
        [SerializeField] [UdonSynced] private float baseCombatTagTime = 0;

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
            get => _playerWeight;
            protected set
            {
                _playerWeight = Mathf.Clamp(value, 0F, maximumCarryingWeightInKilogram);
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI]
        public float EnvironmentEffectMultiplier
        {
            get => _environmentEffectMultiplier;
            protected set
            {
                _environmentEffectMultiplier = value;
                UpdateLocalVrcPlayer();
            }
        }

        [PublicAPI] public float CustomEffectMultiplier { get; set; } = 1F;

        [PublicAPI]
        public float TotalMultiplier => (1 - (PlayerWeight / maximumCarryingWeightInKilogram)) *
                                        EnvironmentEffectMultiplier * CustomEffectMultiplier * CombatTagMultiplier;

        [PublicAPI] public float CombatTagMultiplier => _combatTagged ? _cachedCombatTagSpeedMultiplier : 1F;
        [PublicAPI] public bool CanRun => _canRun && !_combatTagged;

        [PublicAPI]
        public float ActualWalkSpeed =>
            (_useGunSprint ? _cachedGunWalkSpeed : BaseWalkSpeed) * TotalMultiplier;

        [PublicAPI]
        public float ActualRunSpeed => CanRun
            ? (_useGunSprint ? _cachedGunSprintSpeed : BaseRunSpeed) * TotalMultiplier
            : ActualWalkSpeed;

        [PublicAPI]
        public float ActualStrafeSpeed => CanRun
            ? (_useGunSprint ? BaseWalkSpeed : BaseStrafeSpeed) * TotalMultiplier
            : ActualWalkSpeed;

        [PublicAPI] public float ActualJumpImpulse => BaseJumpImpulse * TotalMultiplier;
        [PublicAPI] public float ActualGravityStrength => BaseGravityStrength * TotalMultiplier;

        #endregion

        #region EventInvokers

        private readonly DataList _eventCallbacks = new DataList();
        private bool _isInvokingEvents;

        [PublicAPI]
        public void SubscribeCallback(PlayerControllerCallback callback)
        {
            if (callback == null) return;
            _eventCallbacks.Add(callback);
        }

        [PublicAPI]
        public bool UnsubscribeCallback(PlayerControllerCallback callback)
        {
            return _eventCallbacks.Remove(callback);
        }

        private void Invoke_OnSurfaceUpdated(ObjectMarkerBase previous, ObjectMarkerBase current)
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();
            foreach (var token in arr) ((PlayerControllerCallback)token.Reference).OnSurfaceUpdated(previous, current);
            _isInvokingEvents = false;
        }

        private void Invoke_OnHeldObjectsUpdated()
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();
            foreach (var token in arr) ((PlayerControllerCallback)token.Reference).OnHeldObjectsUpdated();
            _isInvokingEvents = false;
        }

        private void Invoke_OnActiveTagsUpdated()
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();
            foreach (var token in arr) ((PlayerControllerCallback)token.Reference).OnActiveTagsUpdated();
            _isInvokingEvents = false;
        }

        private void Invoke_OnPreApplyMovementProperties()
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();
            foreach (var token in arr) ((PlayerControllerCallback)token.Reference).OnPreApplyMovementProperties();
            _isInvokingEvents = false;
        }

        private void Invoke_OnPostApplyMovementProperties()
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();
            foreach (var token in arr) ((PlayerControllerCallback)token.Reference).OnPostApplyMovementProperties();
            _isInvokingEvents = false;
        }

        #endregion

        #region GunIntegration

        private bool _useGunSprint = false;
        private float _cachedGunWalkSpeed = float.NaN;
        private float _cachedGunSprintSpeed = float.NaN;
        private float _cachedGunSprintThresholdMultiplier = float.NaN;
        private bool _useCombatTag = false;
        private float _cachedCombatTagSpeedMultiplier = float.NaN;
        private float _cachedCombatTagTime = float.NaN;

        private void UpdateCombatTagAndCanRunState()
        {
            if (!baseUseGunSprint || gunManager.LocalHeldGuns.Length == 0 || !_useGunSprint)
            {
                _canRun = true;
                _combatTagged = false;
                return;
            }

            var head = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var headForward = head.rotation * Vector3.forward;
            var utcNow = DateTime.UtcNow;

            _canRun = true;
            _combatTagged = false;
            foreach (var gun in gunManager.LocalHeldGuns)
            {
                if (!gun.MainHandle.IsPickedUp) continue;

                if (utcNow.Subtract(gun.LastShotTime).Seconds < _cachedCombatTagTime && _useCombatTag)
                {
                    _combatTagged = true;
                    break;
                }

                var gunForward = gun.ShooterRotation * Vector3.forward;
                var dot = Vector3.Dot(headForward, gunForward);

                if (dot > baseGunDirectionThreshold * _cachedGunSprintThresholdMultiplier)
                {
                    _canRun = false;
                    break;
                }
            }
        }

        private void UpdateLowestGunProperty()
        {
            _useGunSprint = false;
            _cachedGunWalkSpeed = float.NaN;
            _cachedGunSprintSpeed = float.NaN;
            _cachedGunSprintThresholdMultiplier = float.NaN;
            _useCombatTag = false;
            _cachedCombatTagSpeedMultiplier = float.NaN;
            _cachedCombatTagTime = float.NaN;

            if (gunManager.LocalHeldGuns.Length == 0) return;

            foreach (var gun in gunManager.LocalHeldGuns)
            {
                float estWalkSpeed, estSprintSpeed, estSprintThresholdMultiplier;
                var movementOption = gun.MovementOption;
                switch (movementOption)
                {
                    default:
                    case MovementOption.Inherit:
                    {
                        estWalkSpeed = baseGunWalkSpeed;
                        estSprintSpeed = baseGunSprintSpeed;
                        estSprintThresholdMultiplier = 1F;
                        break;
                    }
                    case MovementOption.Direct:
                    {
                        estWalkSpeed = gun.WalkSpeed;
                        estSprintSpeed = gun.SprintSpeed;
                        estSprintThresholdMultiplier = gun.SprintThresholdMultiplier;
                        break;
                    }
                    case MovementOption.Multiply:
                    {
                        estWalkSpeed = BaseWalkSpeed * gun.WalkSpeed;
                        estSprintSpeed = BaseRunSpeed * gun.SprintSpeed;
                        estSprintThresholdMultiplier = gun.SprintThresholdMultiplier;
                        break;
                    }
                    case MovementOption.Disable:
                    {
                        estWalkSpeed = BaseWalkSpeed;
                        estSprintSpeed = BaseRunSpeed;
                        estSprintThresholdMultiplier = 1F;
                        break;
                    }
                }

                if (movementOption != MovementOption.Disable)
                {
                    if (float.IsNaN(_cachedGunSprintSpeed) ||
                        estSprintSpeed < _cachedGunSprintSpeed)
                    {
                        _cachedGunSprintSpeed = estSprintSpeed;
                        _cachedGunSprintThresholdMultiplier = estSprintThresholdMultiplier;
                    }

                    if (float.IsNaN(_cachedGunWalkSpeed) ||
                        estWalkSpeed < _cachedGunWalkSpeed)
                    {
                        _cachedGunWalkSpeed = estWalkSpeed;
                    }

                    _useGunSprint = true;
                }

                var combatTagOption = gun.CombatTagOption;
                float estCombatTagSpeedMultiplier, estCombatTagTime;
                switch (combatTagOption)
                {
                    default:
                    case CombatTagOption.Inherit:
                    {
                        estCombatTagSpeedMultiplier = baseCombatTagSpeedMultiplier;
                        estCombatTagTime = baseCombatTagTime;
                        break;
                    }
                    case CombatTagOption.Direct:
                    {
                        estCombatTagSpeedMultiplier = gun.CombatTagSpeedMultiplier;
                        estCombatTagTime = gun.CombatTagTime;
                        break;
                    }
                    case CombatTagOption.Multiply:
                    {
                        estCombatTagSpeedMultiplier = baseCombatTagSpeedMultiplier * gun.CombatTagSpeedMultiplier;
                        estCombatTagTime = baseCombatTagTime * gun.CombatTagTime;
                        break;
                    }
                    case CombatTagOption.Disable:
                    {
                        estCombatTagSpeedMultiplier = 1;
                        estCombatTagTime = 0;
                        break;
                    }
                }

                if (combatTagOption != CombatTagOption.Disable)
                {
                    if (float.IsNaN(_cachedCombatTagSpeedMultiplier) ||
                        _cachedCombatTagSpeedMultiplier > estCombatTagSpeedMultiplier)
                    {
                        _cachedCombatTagSpeedMultiplier = estCombatTagSpeedMultiplier;
                    }

                    if (float.IsNaN(_cachedCombatTagTime) || _cachedCombatTagTime < estCombatTagTime)
                    {
                        _cachedCombatTagTime = estCombatTagTime;
                    }

                    _useCombatTag = true;
                }
            }
        }

        public override void OnPickedUpLocally(ManagedGun instance)
        {
            UpdateLowestGunProperty();
        }

        public override void OnDropLocally(ManagedGun instance)
        {
            UpdateLowestGunProperty();
        }

        private void GunIntegrationUpdatePass()
        {
            bool lastCanRun = _canRun, lastCombatTagged = _combatTagged;
            UpdateCombatTagAndCanRunState();

            if (lastCanRun != _canRun || lastCombatTagged != _combatTagged)
                UpdateLocalVrcPlayer();
        }

        #endregion

        #region Compatibility

        [Obsolete]
        public bool checkGunDirectionToAllowRunning
        {
            get => baseUseGunSprint;
            set => baseUseGunSprint = value;
        }

        [Obsolete]
        public float gunDirectionUpperBound
        {
            get => baseGunDirectionThreshold;
            set => baseGunDirectionThreshold = value;
        }

        [Obsolete]
        public float combatTagTime
        {
            get => baseCombatTagTime;
            set => baseCombatTagTime = value;
        }

        #endregion
    }

    public abstract class PlayerControllerCallback : UdonSharpBehaviour
    {
        /// <summary>
        /// Called when <see cref="PlayerController.CurrentSurface"/> has been changed.
        /// </summary>
        /// <param name="previous">previous <see cref="PlayerController.CurrentSurface"/></param>
        /// <param name="current">current <see cref="PlayerController.CurrentSurface"/></param>
        public virtual void OnSurfaceUpdated(ObjectMarkerBase previous, ObjectMarkerBase current)
        {
        }

        /// <summary>
        /// Called when <see cref="ObjectMarkerBase"/> objects were picked up or dropped.
        /// </summary>
        /// <seealso cref="PlayerController.HeldObjects"/>
        public virtual void OnHeldObjectsUpdated()
        {
        }

        /// <summary>
        /// Called when affected held or surface <see cref="ObjectMarkerBase"/> tags were updated.
        /// </summary>
        /// <remarks>
        /// Does not check equality of previous and updated list. unchanged calls are possible.
        /// This event is called before <see cref="OnSurfaceUpdated"/> and <see cref="OnHeldObjectsUpdated"/>
        /// </remarks>
        /// <seealso cref="PlayerController.ActiveTags"/>
        public virtual void OnActiveTagsUpdated()
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerController.UpdateLocalVrcPlayer"/> was called. before applying properties.
        /// </summary>
        /// <remarks>
        /// Updating player mods such as <see cref="VRCPlayerApi.SetWalkSpeed"/> will have no effect since it'll be overwritten.
        /// </remarks>
        public virtual void OnPreApplyMovementProperties()
        {
        }

        /// <summary>
        /// Called when <see cref="PlayerController.UpdateLocalVrcPlayer"/> was called. after applied properties.
        /// </summary>
        public virtual void OnPostApplyMovementProperties()
        {
        }
    }
}