using System;
using CenturionCC.System.Audio;
using CenturionCC.System.Gun;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

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
        [SerializeField] [NewbieInject]
        private AudioManager audioManager;

        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private FootstepAudioStore footstepAudioStore;

        private readonly DataList _activeTags = new DataList();
        private readonly DataList _heldObjects = new DataList();

        private bool _canRun = true;
        private bool _combatTagged;

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
        private bool _updatedSurfaceInFrame;
        private Vector3 _vel;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;

            if (gunManager) gunManager.SubscribeCallback(this);
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

            if (useGunIntegration)
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
            if (CurrentSurface && !hasMarkerDataUpdate)
                foreach (var surfTag in CurrentSurface.Tags)
                    _activeTags.Remove(surfTag);

            var lastSurf = CurrentSurface;
            CurrentSurface = objMarker;

            if (CurrentSurface)
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
            if (terrainMarker)
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

            // Did player travel one-step distance?
            if (Vector3.Distance(_footstepLastCheckedPosition, localPlayerPos) <
                footstepDistance * TotalMultiplier) return;

            // Is the player airborne?
            if (!_localPlayer.IsPlayerGrounded()) return;

            var timeDiff = (Time.time - _footstepLastInvokedTime) * TotalMultiplier;
            _footstepLastInvokedTime = Time.time;
            _footstepLastCheckedPosition = localPlayerPos;

            // Did player travel fast enough to play footstep?
            if (footstepTime < timeDiff) return;

            if (!_updatedSurfaceInFrame) CurrentSurfaceUpdatePass();

            if (!CurrentSurface) return;

            var playerBase = playerManager.GetLocalPlayer();
            if (!playerBase || playerManager.IsInStaffTeam(playerBase)) return;

            var isSlow = footstepSlowThresholdTime < timeDiff;
            PlayFootstepSound(localPlayerPos, CurrentSurface.ObjectType, isSlow);
        }

        [NetworkCallable(100)]
        public void PlayFootstepSound(Vector3 pos, ObjectType type, bool isSlow)
        {
            var audioData = footstepAudioStore.Get(type, isSlow);
            audioManager.PlayAudioAtPosition(audioData, pos);
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

        [Header("Gun Integrations")] [SerializeField]
        [FormerlySerializedAs("baseApplyGunPropertyToPlayerController")]
        private bool useGunIntegration = true;

        [Header("Gun Integration: Gun Sprint")] [SerializeField] [UdonSynced]
        [FormerlySerializedAs("checkGunDirectionToAllowRunning")]
        private bool baseUseGunSprint = true;

        [SerializeField] [UdonSynced]
        [FormerlySerializedAs("baseGunWalkSpeed")]
        private float baseGunSprintWalkSpeed = 2F;

        [SerializeField] [UdonSynced]
        [FormerlySerializedAs("baseGunSprintSpeed")]
        private float baseGunSprintRunSpeed = 4F;

        [FormerlySerializedAs("baseGunDirectionThreshold")]
        [SerializeField] [UdonSynced] [Range(0, 1F)]
        [FormerlySerializedAs("gunDirectionDotThreshold")]
        private float baseGunDirectionDirectionThreshold = 0.88F;

        [Header("Gun Integration: Combat Tag")]
        [SerializeField] [UdonSynced]
        [Tooltip("\"CombatTag\" refers to a slow down when player has started shooting")]
        private bool baseUseCombatTag = true;

        [SerializeField] [UdonSynced] private float baseCombatTagSpeedMultiplier = 0.5F;
        [SerializeField] [UdonSynced] private float baseCombatTagTime = 0.25F;

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

        [PublicAPI] public float CombatTagMultiplier => _combatTagged ? ActualCombatTagSpeedMultiplier : 1F;
        [PublicAPI] public bool CanRun => _canRun && !_combatTagged;

        [PublicAPI]
        public float ActualWalkSpeed =>
            (ActualUseGunSprint ? ActualGunSprintWalkSpeed : BaseWalkSpeed) * TotalMultiplier;

        [PublicAPI]
        public float ActualRunSpeed => CanRun
            ? (ActualUseGunSprint ? ActualGunSprintRunSpeed : BaseRunSpeed) * TotalMultiplier
            : ActualWalkSpeed;

        [PublicAPI]
        public float ActualStrafeSpeed => ActualUseGunSprint
            ? Mathf.Min(ActualWalkSpeed, BaseStrafeSpeed * TotalMultiplier)
            : BaseStrafeSpeed * TotalMultiplier;

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

        private float _cachedGunSprintThresholdMultiplier = float.NaN;
        private float _lastShotTime;

        private void UpdateCombatTagAndCanRunState()
        {
            if (gunManager.LocalHeldGuns.Length == 0)
            {
                _canRun = true;
                _combatTagged = false;
                return;
            }

            var head = _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var headForward = head.rotation * Vector3.forward;

            _canRun = true;
            _combatTagged = false;
            foreach (var gun in gunManager.LocalHeldGuns)
            {
                if (!gun.MainHandle.IsPickedUp) continue;

                var gunForward = gun.ShooterRotation * Vector3.forward;
                var dot = Vector3.Dot(headForward, gunForward);

                if (dot > ActualGunSprintDirectionThreshold)
                {
                    _canRun = !ActualUseGunSprint;
                    break;
                }
            }

            if (Time.timeSinceLevelLoad - _lastShotTime < ActualCombatTagTime && ActualUseCombatTag)
            {
                _combatTagged = true;
            }
        }

        private void UpdateLowestGunProperty()
        {
            ActualUseGunSprint = false;
            ActualGunSprintWalkSpeed = float.NaN;
            ActualGunSprintRunSpeed = float.NaN;
            _cachedGunSprintThresholdMultiplier = float.NaN;
            ActualUseCombatTag = false;
            ActualCombatTagSpeedMultiplier = float.NaN;
            ActualCombatTagTime = float.NaN;

            if (gunManager.LocalHeldGuns.Length == 0 || !useGunIntegration) return;

            foreach (var gun in gunManager.LocalHeldGuns)
            {
                float estWalkSpeed, estSprintSpeed, estSprintThresholdMultiplier;
                var movementOption = gun.MovementOption;
                switch (movementOption)
                {
                    default:
                    case MovementOption.Inherit:
                    {
                        estWalkSpeed = baseGunSprintWalkSpeed;
                        estSprintSpeed = baseGunSprintRunSpeed;
                        estSprintThresholdMultiplier = 1;
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
                    if (float.IsNaN(ActualGunSprintRunSpeed) ||
                        estSprintSpeed < ActualGunSprintRunSpeed)
                    {
                        ActualGunSprintRunSpeed = estSprintSpeed;
                        _cachedGunSprintThresholdMultiplier = estSprintThresholdMultiplier;
                    }

                    if (float.IsNaN(ActualGunSprintWalkSpeed) ||
                        estWalkSpeed < ActualGunSprintWalkSpeed)
                    {
                        ActualGunSprintWalkSpeed = estWalkSpeed;
                    }

                    ActualUseGunSprint = movementOption != MovementOption.Inherit || baseUseGunSprint;
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
                    if (float.IsNaN(ActualCombatTagSpeedMultiplier) ||
                        ActualCombatTagSpeedMultiplier > estCombatTagSpeedMultiplier)
                    {
                        ActualCombatTagSpeedMultiplier = estCombatTagSpeedMultiplier;
                    }

                    if (float.IsNaN(ActualCombatTagTime) || ActualCombatTagTime < estCombatTagTime)
                    {
                        ActualCombatTagTime = estCombatTagTime;
                    }

                    ActualUseCombatTag = combatTagOption != CombatTagOption.Inherit || baseUseCombatTag;
                }
            }

            Debug.Log(
                $"[PlayerController] l:{gunManager.LocalHeldGuns.Length}, gs:{ActualUseGunSprint}, ct:{ActualUseCombatTag}");
        }

        public override void OnPickedUpLocally(ManagedGun instance)
        {
            if (!useGunIntegration) return;
            UpdateLowestGunProperty();
            UpdateLocalVrcPlayer();
        }

        public override void OnDropLocally(ManagedGun instance)
        {
            if (!useGunIntegration) return;
            UpdateLowestGunProperty();
            UpdateLocalVrcPlayer();
        }

        public override void OnShoot(ManagedGun instance, ProjectileBase projectile)
        {
            if (!useGunIntegration || !instance.IsLocal) return;
            _lastShotTime = Time.timeSinceLevelLoad;
        }

        private void GunIntegrationUpdatePass()
        {
            bool lastCanRun = _canRun, lastCombatTagged = _combatTagged;
            UpdateCombatTagAndCanRunState();

            if (lastCanRun != _canRun || lastCombatTagged != _combatTagged)
                UpdateLocalVrcPlayer();
        }

        #endregion

        #region GunIntegrationProperties

        [PublicAPI]
        public bool BaseUseCombatTag
        {
            get => baseUseCombatTag;
            set => baseUseCombatTag = value;
        }

        [PublicAPI]
        public float BaseCombatTagTime
        {
            get => baseCombatTagTime;
            set => baseCombatTagTime = value;
        }

        [PublicAPI]
        public float BaseCombatTagSpeedMultiplier
        {
            get => baseCombatTagSpeedMultiplier;
            set => baseCombatTagSpeedMultiplier = value;
        }

        [PublicAPI]
        public bool BaseUseGunSprint
        {
            get => baseUseGunSprint;
            set => baseUseGunSprint = value;
        }

        [PublicAPI]
        public float BaseGunSprintWalkSpeed
        {
            get => baseGunSprintWalkSpeed;
            set => baseGunSprintWalkSpeed = value;
        }

        [PublicAPI]
        public float BaseGunSprintRunSpeed
        {
            get => baseGunSprintRunSpeed;
            set => baseGunSprintRunSpeed = value;
        }

        [PublicAPI]
        public float BaseGunSprintDirectionThreshold
        {
            get => baseGunDirectionDirectionThreshold;
            set => baseGunDirectionDirectionThreshold = value;
        }

        [PublicAPI]
        public bool ActualUseCombatTag { get; private set; }

        [PublicAPI]
        public float ActualCombatTagTime { get; private set; } = float.NaN;

        [PublicAPI]
        public float ActualCombatTagSpeedMultiplier { get; private set; } = float.NaN;

        [PublicAPI]
        public bool ActualUseGunSprint { get; private set; }

        [PublicAPI]
        public float ActualGunSprintWalkSpeed { get; private set; } = float.NaN;

        [PublicAPI]
        public float ActualGunSprintRunSpeed { get; private set; } = float.NaN;

        [PublicAPI]
        public float ActualGunSprintDirectionThreshold =>
            baseGunDirectionDirectionThreshold * _cachedGunSprintThresholdMultiplier;

        #endregion

        #region Compatibility

        [Obsolete] // ReSharper disable once InconsistentNaming
        public bool checkGunDirectionToAllowRunning
        {
            get => baseUseGunSprint;
            set => baseUseGunSprint = value;
        }

        [Obsolete] // ReSharper disable once InconsistentNaming
        public float gunDirectionUpperBound
        {
            get => baseGunDirectionDirectionThreshold;
            set => baseGunDirectionDirectionThreshold = value;
        }

        [Obsolete] // ReSharper disable once InconsistentNaming
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