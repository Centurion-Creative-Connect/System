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
    /// This class currently manipulates 2 features by checking player's walking surface:
    /// - adjusting player's movement speed by environmental effect and holding objects weight.
    /// - playing footstep sound when walking fast enough.
    /// </remarks>
    /// <seealso cref="ObjectMarker"/>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerController : UdonSharpBehaviour
    {
        private bool _canRun = true;
        private ObjectMarkerBase _currentSurfaceObject;

        private Vector3 _footstepLastCheckedPosition;
        private float _footstepLastInvokedTime;
        private GunManager _gunManager;
        private bool _lastSurfaceNoFootstep;

        private float _lastSurfaceUpdatedTime;

        private PlayerManager _playerManager;

        private void Start()
        {
            _playerManager = CenturionSystemReference.GetPlayerManager();
            _gunManager = CenturionSystemReference.GetGunManager();
        }

        private void FixedUpdate()
        {
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
            if (!Physics.Raycast(Networking.LocalPlayer.GetPosition() + new Vector3(0f, .1F, 0F), Vector3.down,
                    out var hit, 3,
                    surfaceCheckingLayer))
                return;

            var objMarker = hit.transform.GetComponent<ObjectMarkerBase>();
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
            if (!checkGunDirectionToAllowRunning || _gunManager.LocalHeldGuns.Length == 0)
            {
                _canRun = true;
                return;
            }

            _canRun = true;
            foreach (var gun in _gunManager.LocalHeldGuns)
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
            var currentPlayerPos = Networking.LocalPlayer.GetPosition();

            // Did player traveled one step distance?
            if (Vector3.Distance(_footstepLastCheckedPosition, currentPlayerPos) <
                footstepDistance * TotalMultiplier) return;

            var timeDiff = (Time.time - _footstepLastInvokedTime) * TotalMultiplier;
            _footstepLastInvokedTime = Time.time;
            _footstepLastCheckedPosition = currentPlayerPos;

            // Did player traveled fast enough to play footstep?
            if (footstepTime < timeDiff || _currentSurfaceObject == null) return;

            var playerBase = _playerManager.GetLocalPlayer();
            if (playerBase == null || _playerManager.IsStaffTeamId(playerBase.TeamId))
                return;

            var isSlow = footstepSlowThresholdTime < timeDiff;
            var methodName = GetFootstepMethodName(_currentSurfaceObject.ObjectType, isSlow);
            playerBase.SendCustomNetworkEvent(NetworkEventTarget.All, methodName);
        }

        private static string GetFootstepMethodName(ObjectType type, bool isSlow)
        {
            var format = isSlow ? "PlaySlow{0}FootstepAudio" : "Play{0}FootstepAudio";
            switch (type)
            {
                case ObjectType.Dirt:
                case ObjectType.Gravel:
                    return string.Format(format, "Ground");
                case ObjectType.Wood:
                    return string.Format(format, "Wood");
                case ObjectType.Metallic:
                    return string.Format(format, "Iron");
                case ObjectType.Concrete:
                case ObjectType.Prototype:
                default:
                    return string.Format(format, "Fallback");
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
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        /// <summary>
        /// Applies current movement speed to local player.
        /// </summary>
        [PublicAPI]
        public void UpdateLocalVrcPlayer()
        {
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null || !localPlayer.IsValid())
                return;

            Debug.Log($"[PlayerController] Applying actual properties: \n" +
                      $"WalkSpeed  : {ActualWalkSpeed}\n" +
                      $"RunSpeed   : {ActualRunSpeed}\n" +
                      $"StrafeSpeed: {ActualStrafeSpeed}\n" +
                      $"JumpImpulse: {ActualJumpImpulse}\n" +
                      $"GravityStrength: {ActualGravityStrength}");

            localPlayer.SetWalkSpeed(ActualWalkSpeed);
            localPlayer.SetRunSpeed(ActualRunSpeed);
            localPlayer.SetStrafeSpeed(ActualStrafeSpeed);
            localPlayer.SetJumpImpulse(ActualJumpImpulse);
            localPlayer.SetGravityStrength(ActualGravityStrength);
        }

        /*
         * Not concrete API, though for now it just works.
         */

        private void AddHoldingObject(float weightAddition)
        {
            PlayerWeight += weightAddition;
            UpdateLocalVrcPlayer();
        }

        /// <summary>
        /// Adds object's weight to player's current weight.
        /// </summary>
        /// <param name="anObject">an object which begin holding.</param>
        public void AddHoldingObject(ObjectMarkerBase anObject)
        {
            AddHoldingObject(anObject.ObjectWeight);
        }

        private void RemoveHoldingObject(float weightSubtraction)
        {
            PlayerWeight -= weightSubtraction;
            UpdateLocalVrcPlayer();
        }

        /// <summary>
        /// Subtracts object's weight from player's current weight.
        /// </summary>
        /// <param name="anObject">an object which stopped holding.</param>
        public void RemoveHoldingObject(ObjectMarkerBase anObject)
        {
            RemoveHoldingObject(anObject.ObjectWeight);
        }

        /// <summary>
        /// Resets player's weight.
        /// </summary>
        /// <remarks>
        /// This method can be used to ensure no objects are left held.
        /// </remarks>
        public void RemoveAllHoldingObject()
        {
            PlayerWeight = 0;
            UpdateLocalVrcPlayer();
        }

        #region Base

        [Tooltip("Delay in seconds until try to update current surface object marker.")]
        [SerializeField]
        private float surfaceUpdateFrequency = 0.5F;
        [Tooltip("Layers to check objects with ObjectMarker attached.")]
        [SerializeField]
        private LayerMask surfaceCheckingLayer = 1 << 11;

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
                playerWeight = value;
                if (playerWeight < 0)
                    playerWeight = 0;

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
        public float TotalMultiplier => (1 - (PlayerWeight / maximumCarryingWeightInKilogram)) *
                                        EnvironmentEffectMultiplier;

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