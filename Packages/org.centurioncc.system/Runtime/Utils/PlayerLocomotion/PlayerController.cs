using CenturionCC.System.Audio;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Utils.PlayerLocomotion
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
        [SerializeField] [NewbieInject]
        private AudioManager audioManager;

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        [SerializeField] [NewbieInject]
        private FootstepAudioStore footstepAudioStore;

        private readonly DataList _activeTags = new DataList();
        private readonly DataList _objectMarkers = new DataList();

        private float _environmentEffectMultiplier = 1F;

        private Vector3 _footstepLastCheckedPosition;
        private float _footstepLastInvokedTime;

        private bool _hasOverride;
        private RaycastHit _hit;
        private bool _isApplyingGroundSnap;
        private float _lastGroundSnapUpdatedTime;
        private bool _lastNoFootstep;
        private float _lastSurfaceUpdatedTime;

        private VRCPlayerApi _localPlayer;
        private float _overrideRunSpeed;
        private float _overrideStrafeSpeed;
        private float _overrideWalkSpeed;

        private float _playerWeight;
        private Ray _ray;

        private bool _shouldUpdateVrcPlayer;
        private bool _updatedSurfaceInFrame;
        private Vector3 _vel;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            SendCustomEventDelayedFrames(nameof(UpdateLocalVrcPlayer), 1);
        }

        private void Update()
        {
            _updatedSurfaceInFrame = false;

            if (snapPlayerToGroundOnSlopes)
                GroundSnapUpdatePass();

            if (UpdateTimer())
                CurrentSurfaceUpdatePass();

            if (playFootstepSound && !_lastNoFootstep)
                FootstepUpdatePass();

            Invoke_OnPlayerControllerUpdate();

            if (_shouldUpdateVrcPlayer)
                UpdateVrcPlayer();
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (!player.isLocal) return;
            UpdateLocalVrcPlayer();
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
            }
            else
            {
                EnvironmentEffectMultiplier = 1;
            }

            UpdateMarker();
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
            if (!playerBase || playerBase.IsInStaffTeam()) return;

            var isSlow = footstepSlowThresholdTime < timeDiff;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayFootstepSound), localPlayerPos, CurrentSurface.ObjectType, isSlow);
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

        private void UpdateVrcPlayer()
        {
            if (_localPlayer == null || !_localPlayer.IsValid()) return;

            Invoke_OnOverridePropertyCheck();
            Invoke_OnPreApplyMovementProperties();

            _localPlayer.SetWalkSpeed(ActualWalkSpeed);
            _localPlayer.SetRunSpeed(ActualRunSpeed);
            _localPlayer.SetStrafeSpeed(ActualStrafeSpeed);
            _localPlayer.SetJumpImpulse(ActualJumpImpulse);
            _localPlayer.SetGravityStrength(ActualGravityStrength);
            _shouldUpdateVrcPlayer = false;

            Invoke_OnPostApplyMovementProperties();
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
            _shouldUpdateVrcPlayer = true;
        }
        #endregion

        #region MarkerAPIs
        /// <summary>
        /// Adds a marker in manipulation of local player.
        /// </summary>
        /// <param name="anObject">The object marker to be added. Must not be null. If duplicates are not allowed, the object should not yet exist in the list.</param>
        [PublicAPI]
        public void AddMarker(ObjectMarkerBase anObject)
        {
            if (anObject == null)
            {
                Debug.LogWarning("[PlayerController] Tried to add null to held object list!");
                return;
            }

            if (_objectMarkers.Contains(anObject) && !allowDuplicateHeldObjects)
            {
                Debug.LogWarning(
                    $"[PlayerController] Tried to add {anObject.name}, but it already exists in held objects list!");
                return;
            }

            _objectMarkers.Add(anObject);
            foreach (var objectTag in anObject.Tags) _activeTags.Add(objectTag);
            UpdateMarker();
            Invoke_OnActiveTagsUpdated();
            Invoke_OnHeldObjectsUpdated();
        }

        /// <summary>
        /// Subtracts object's weight from the player's current weight.
        /// </summary>
        /// <param name="anObject">an object which stopped holding.</param>
        [PublicAPI]
        public void RemoveMarker(ObjectMarkerBase anObject)
        {
            if (anObject == null)
            {
                Debug.LogWarning("[PlayerController] Tried to remove null from held objects list!");
                return;
            }

            if (!_objectMarkers.Remove(anObject))
            {
                Debug.LogWarning("[PlayerController] Tried to remove object that isn't in the held objects list!");
                return;
            }

            foreach (var objectTag in anObject.Tags) _activeTags.Remove(objectTag);
            UpdateMarker();
            Invoke_OnActiveTagsUpdated();
            Invoke_OnHeldObjectsUpdated();
        }

        /// <summary>
        /// Checks whether the player is currently holding the specified object.
        /// </summary>
        /// <param name="anObject">The object to check against the player's held objects.</param>
        /// <returns>True if the player is holding the specified object; otherwise, false.</returns>
        [PublicAPI]
        public bool HasMarker(ObjectMarkerBase anObject)
        {
            return _objectMarkers.Contains(anObject);
        }

        /// <summary>
        /// Update <see cref="PlayerWeight"/> by currently known held objects.
        /// </summary>
        /// <remarks>
        /// This also updates the current VrcPlayer. See <see cref="UpdateLocalVrcPlayer"/>.
        /// </remarks>
        /// <seealso cref="UpdateLocalVrcPlayer"/>
        [PublicAPI]
        public void UpdateMarker()
        {
            var totalWeight = 0F;
            var arr = _objectMarkers.ToArray();
            foreach (var o in arr)
            {
                var marker = (ObjectMarkerBase)o.Reference;
                if (!marker) continue;

                totalWeight += marker.ObjectWeight;
            }

            PlayerWeight = totalWeight;
            _lastNoFootstep = _activeTags.Contains("NoFootstep");
        }

        /// <summary>
        /// Resets player's weight.
        /// </summary>
        /// <remarks>
        /// This method can be used to ensure no objects are left held.
        /// </remarks>
        [PublicAPI]
        public void RemoveAllMarkers()
        {
            _objectMarkers.Clear();
            _activeTags.Clear();

            if (CurrentSurface != null)
            {
                foreach (var surfTags in CurrentSurface.Tags)
                {
                    _activeTags.Add(surfTags);
                }
            }

            UpdateMarker();
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
        public DataList HeldObjects => _objectMarkers.DeepClone();

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

        [SerializeField]
        private bool useAvatarEyeHeightForMovementMultiplier;

        [SerializeField]
        [Tooltip("(AvatarEyeHeight / BaseAvatarEyeHeight) will be used as multiplier on all movement properties")]
        private float baseAvatarEyeHeight = 1.65f;
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

        [PublicAPI]
        public float BaseAvatarEyeHeight
        {
            get => baseAvatarEyeHeight;
            set
            {
                baseAvatarEyeHeight = value;
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

        [PublicAPI]
        public float CustomEffectMultiplier { get; set; } = 1F;

        [PublicAPI] 
        public float AvatarEyeHeightMultiplier => useAvatarEyeHeightForMovementMultiplier ? ActualAvatarEyeHeight / BaseAvatarEyeHeight : 1F;

        [PublicAPI]
        public float TotalMultiplier => (1 - (PlayerWeight / maximumCarryingWeightInKilogram)) * EnvironmentEffectMultiplier * CustomEffectMultiplier * AvatarEyeHeightMultiplier;

        [PublicAPI]
        public bool CanRun { get; private set; }

        [PublicAPI]
        public float ActualWalkSpeed => (_hasOverride ? _overrideWalkSpeed : BaseWalkSpeed) * TotalMultiplier;

        [PublicAPI]
        public float ActualRunSpeed => CanRun ? (_hasOverride ? _overrideRunSpeed : BaseRunSpeed) * TotalMultiplier : ActualWalkSpeed;

        [PublicAPI]
        public float ActualStrafeSpeed => !CanRun ? Mathf.Min(ActualWalkSpeed, BaseStrafeSpeed * TotalMultiplier) : BaseStrafeSpeed * TotalMultiplier;

        [PublicAPI]
        public float ActualJumpImpulse => BaseJumpImpulse * TotalMultiplier;

        [PublicAPI]
        public float ActualGravityStrength => BaseGravityStrength * TotalMultiplier;

        [PublicAPI]
        public float ActualAvatarEyeHeight => _localPlayer.GetAvatarEyeHeightAsMeters();
        #endregion

        #region EventInvokers
        private readonly DataList _eventCallbacks = new DataList();
        private bool _isInvokingEvents;

        [PublicAPI]
        public void Subscribe(PlayerControllerCallback callback)
        {
            if (callback == null) return;
            _eventCallbacks.Add(callback);
        }

        [PublicAPI]
        public bool Unsubscribe(PlayerControllerCallback callback)
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

        private void Invoke_OnPlayerControllerUpdate()
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();
            foreach (var token in arr) ((PlayerControllerCallback)token.Reference).OnPlayerControllerUpdate();
            _isInvokingEvents = false;
        }

        private void Invoke_OnOverridePropertyCheck()
        {
            if (_isInvokingEvents) return;

            _isInvokingEvents = true;
            var arr = _eventCallbacks.ToArray();

            _hasOverride = false;
            _overrideWalkSpeed = float.PositiveInfinity;
            _overrideRunSpeed = float.PositiveInfinity;
            _overrideStrafeSpeed = float.PositiveInfinity;
            CanRun = baseCanRun;

            foreach (var token in arr)
            {
                var callback = (PlayerControllerCallback)token.Reference;
                if (callback.HasSpeedOverrides())
                {
                    _hasOverride = true;
                    callback.GetSpeedOverrides(out var walkSpeed, out var runSpeed, out var strafeSpeed, out var canRun);
                    _overrideWalkSpeed = Mathf.Min(walkSpeed, _overrideWalkSpeed);
                    _overrideRunSpeed = Mathf.Min(runSpeed, _overrideRunSpeed);
                    _overrideStrafeSpeed = Mathf.Min(strafeSpeed, _overrideStrafeSpeed);
                    if (canRun != baseCanRun) CanRun = canRun;
                }
            }
            _isInvokingEvents = false;
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

        /// <summary>
        /// Called when PlayerController's update was called.
        /// Receiving updates in this callback will guarantee that PlayerController variables are up to date.
        /// </summary>
        public virtual void OnPlayerControllerUpdate()
        {
        }

        /// <summary>
        /// Called when PlayerController is checking for speed override.
        /// </summary>
        /// <returns>true when this callback overrides base player speed from PlayerController, false otherwise.</returns>
        /// <remarks>
        /// Returning true will override base speed. When multiple callbacks return true, the slowest among the callbacks will be used.
        /// </remarks>
        public virtual bool HasSpeedOverrides()
        {
            return false;
        }

        public virtual void GetSpeedOverrides(out float walkSpeed, out float runSpeed, out float strafeSpeed, out bool canRun)
        {
            walkSpeed = 0F;
            runSpeed = 0F;
            strafeSpeed = 0F;
            canRun = false;
        }
    }
}
