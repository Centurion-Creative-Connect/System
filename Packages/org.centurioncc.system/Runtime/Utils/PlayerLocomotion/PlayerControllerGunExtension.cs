using CenturionCC.System.Gun;
using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Utils.PlayerLocomotion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [RequireComponent(typeof(PlayerControllerGunExtensionHelper))]
    public class PlayerControllerGunExtension : PlayerControllerCallback
    {
        [SerializeField] [NewbieInject]
        private PlayerController playerController;
        [SerializeField] [NewbieInject]
        private GunManagerBase gunManager;

        [SerializeField]
        public bool useGunIntegration = true;

        [Header("Gun Sprint")]
        [SerializeField]
        private bool baseUseGunSprint = true;

        [SerializeField]
        private float baseGunSprintWalkSpeed = 2F;

        [SerializeField]
        private float baseGunSprintRunSpeed = 4F;

        [SerializeField] [Range(0, 1F)]
        private float baseGunDirectionDirectionThreshold = 0.88F;

        [Header("Combat Tag")]
        [SerializeField]
        [Tooltip("\"CombatTag\" refers to a slow down when player has started shooting")]
        private bool baseUseCombatTag = true;

        [SerializeField] private float baseCombatTagSpeedMultiplier = 0.5F;
        [SerializeField] private float baseCombatTagTime = 0.25F;

        private void Start()
        {
            playerController.Subscribe(this);
        }

        public override void OnPlayerControllerUpdate()
        {
            if (!useGunIntegration) return;

            bool lastCanRun = _canRun, lastCombatTagged = _combatTagged;
            UpdateCombatTagAndCanRunState();

            if (lastCanRun != _canRun || lastCombatTagged != _combatTagged)
                playerController.UpdateLocalVrcPlayer();
        }

        public override bool HasSpeedOverrides()
        {
            return useGunIntegration && (!float.IsNaN(ActualGunSprintWalkSpeed) || !float.IsNaN(ActualCombatTagSpeedMultiplier));
        }

        public override void GetSpeedOverrides(out float walkSpeed, out float runSpeed, out float strafeSpeed, out bool canRun)
        {
            if (float.IsNaN(ActualGunSprintWalkSpeed))
            {
                walkSpeed = playerController.BaseWalkSpeed;
                runSpeed = playerController.BaseRunSpeed;
                strafeSpeed = playerController.BaseStrafeSpeed;
                canRun = _canRun;
                return;
            }

            walkSpeed = ActualGunSprintWalkSpeed * CombatTagMultiplier;
            runSpeed = ActualGunSprintRunSpeed * CombatTagMultiplier;
            strafeSpeed = ActualGunSprintWalkSpeed * CombatTagMultiplier;
            canRun = _canRun;
        }

        #region GunIntegration
        private bool _canRun;
        private bool _combatTagged;
        private float _cachedGunSprintThresholdMultiplier = float.NaN;
        private float _lastShotTime;

        private void UpdateCombatTagAndCanRunState()
        {
            var locallyHeldGuns = gunManager.GetLocallyHeldGunInstances();
            if (locallyHeldGuns.Length == 0)
            {
                _canRun = true;
                _combatTagged = false;
                return;
            }

            var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var headForward = head.rotation * Vector3.forward;

            _canRun = true;
            _combatTagged = false;
            foreach (var gun in locallyHeldGuns)
            {
                if (!gun.MainHandle.IsPickedUp) continue;

                gun._GetFiringPositionAndRotation(out var firingPos, out var firingRot);
                var gunForward = firingRot * Vector3.forward;
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

        private MovementOption CalculateMovementOption(GunVariantDataStore variantData, out float estWalkSpeed, out float estRunSpeed, out float estSprintThresholdMultiplier)
        {
            var movementOption = variantData.Movement;
            switch (movementOption)
            {
                default:
                case MovementOption.Inherit:
                {
                    estWalkSpeed = baseGunSprintWalkSpeed;
                    estRunSpeed = baseGunSprintRunSpeed;
                    estSprintThresholdMultiplier = 1;
                    break;
                }
                case MovementOption.Direct:
                {
                    estWalkSpeed = variantData.WalkSpeed;
                    estRunSpeed = variantData.SprintSpeed;
                    estSprintThresholdMultiplier = variantData.SprintThresholdMultiplier;
                    break;
                }
                case MovementOption.Multiply:
                {
                    estWalkSpeed = playerController.BaseWalkSpeed * variantData.WalkSpeed;
                    estRunSpeed = playerController.BaseRunSpeed * variantData.SprintSpeed;
                    estSprintThresholdMultiplier = variantData.SprintThresholdMultiplier;
                    break;
                }
                case MovementOption.Disable:
                {
                    estWalkSpeed = playerController.BaseWalkSpeed;
                    estRunSpeed = playerController.BaseRunSpeed;
                    estSprintThresholdMultiplier = 1F;
                    break;
                }
            }

            return movementOption;
        }

        private CombatTagOption CalculateCombatTagOption(GunVariantDataStore variantData, out float estCombatTagSpeedMultiplier, out float estCombatTagTime)
        {
            var combatTagOption = variantData.CombatTag;
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
                    estCombatTagSpeedMultiplier = variantData.CombatTagSpeedMultiplier;
                    estCombatTagTime = variantData.CombatTagTime;
                    break;
                }
                case CombatTagOption.Multiply:
                {
                    estCombatTagSpeedMultiplier = baseCombatTagSpeedMultiplier * variantData.CombatTagSpeedMultiplier;
                    estCombatTagTime = baseCombatTagTime * variantData.CombatTagTime;
                    break;
                }
                case CombatTagOption.Disable:
                {
                    estCombatTagSpeedMultiplier = 1;
                    estCombatTagTime = 0;
                    break;
                }
            }

            return combatTagOption;
        }

        public void UpdateLowestGunProperty()
        {
            ActualUseGunSprint = false;
            ActualGunSprintWalkSpeed = float.NaN;
            ActualGunSprintRunSpeed = float.NaN;
            _cachedGunSprintThresholdMultiplier = float.NaN;
            ActualUseCombatTag = false;
            ActualCombatTagSpeedMultiplier = float.NaN;
            ActualCombatTagTime = float.NaN;

            var locallyHeldGuns = gunManager.GetLocallyHeldGunInstances();
            if (locallyHeldGuns.Length == 0 || !useGunIntegration)
            {
                return;
            }

            foreach (var gun in locallyHeldGuns)
            {
                var variantData = gun.VariantData;
                if (!variantData)
                    continue;

                var movementOption = CalculateMovementOption(variantData, out var estWalkSpeed, out var estSprintSpeed, out var estSprintThresholdMult);
                if (movementOption != MovementOption.Disable)
                {
                    if (float.IsNaN(ActualGunSprintRunSpeed) ||
                        estSprintSpeed < ActualGunSprintRunSpeed)
                    {
                        ActualGunSprintRunSpeed = estSprintSpeed;
                        _cachedGunSprintThresholdMultiplier = estSprintThresholdMult;
                    }

                    if (float.IsNaN(ActualGunSprintWalkSpeed) ||
                        estWalkSpeed < ActualGunSprintWalkSpeed)
                    {
                        ActualGunSprintWalkSpeed = estWalkSpeed;
                    }

                    ActualUseGunSprint = movementOption != MovementOption.Inherit || baseUseGunSprint;
                }

                var combatTagOption = CalculateCombatTagOption(variantData, out var estCombatTagSpeedMult, out var estCombatTagTime);
                if (combatTagOption != CombatTagOption.Disable)
                {
                    if (float.IsNaN(ActualCombatTagSpeedMultiplier) ||
                        ActualCombatTagSpeedMultiplier > estCombatTagSpeedMult)
                    {
                        ActualCombatTagSpeedMultiplier = estCombatTagSpeedMult;
                    }

                    if (float.IsNaN(ActualCombatTagTime) || ActualCombatTagTime < estCombatTagTime)
                    {
                        ActualCombatTagTime = estCombatTagTime;
                    }

                    ActualUseCombatTag = combatTagOption != CombatTagOption.Inherit || baseUseCombatTag;
                }
            }

            Debug.Log(
                $"[PlayerController] l:{locallyHeldGuns.Length}, gs:{ActualUseGunSprint}, ct:{ActualUseCombatTag}");
        }

        public void UpdateLastShotTime()
        {
            _lastShotTime = Time.timeSinceLevelLoad;
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

        public float CombatTagMultiplier => _combatTagged ? ActualCombatTagSpeedMultiplier : 1F;
        #endregion
    }
}
