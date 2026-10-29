using CenturionCC.System.Gun.DataStore;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CockingGunBehaviour : GunBehaviourBase
    {
        [SerializeField] private GunHandle customHandle;

        [Tooltip("Make gun ready to shoot on setup? If checked, summoned gun will always be loaded.")]
        [SerializeField] private bool hotOnSetup;

        [Tooltip("If checked, CustomHandle snaps to the Cocking Position on drop.")]
        [SerializeField] private bool returnToCockingPositionOnDrop;

        [Tooltip("If checked, CustomHandle snaps to the closest of Cocking Position, Idle Twist Position, or Active Twist Position on drop.")]
        [SerializeField] private bool snapToClosestPositionOnDrop;

        [Header("Cocking")]
        [Tooltip("If checked, Gun will be able to cock any time even if the gun was cocked.")]
        [SerializeField] private bool canCockAfterCock;

        [Tooltip("If checked, Gun will load itself after a successful shot.")]
        [SerializeField] private bool isBlowBack;

        [Tooltip("If checked, Gun will cock itself when pulling the trigger.")]
        [SerializeField] private bool isDoubleAction;

        [Tooltip("If checked, Gun will hold open on the last shot of a magazine.")]
        [SerializeField] private bool doHoldOpenOnLastBullet = true;

        [Tooltip("If checked, Gun will be set to hold open state after a successful shot.")]
        [SerializeField] private bool requireManualPushingAfterFire;

        [Tooltip("If checked, CustomHandle will be dropped after a successful shot.")]
        [SerializeField] private bool dropCustomHandleOnFire = true;

        [SerializeField] private Transform cockingPosition;

        [SerializeField] private float cockingLength;

        [Tooltip("Mostly for twisting, Wiggle room when it's CustomHandle is in the idle position. 0 = No wiggle. 1 = Moves Freely")]
        [SerializeField] [Range(0, 1)] private float cockingMargin;

        [Tooltip("Tolerance of pull detection. 1 = Will be accepted as pull without moving, 0 = Has to pull full cocking length to be accepted as pulling.")]
        [SerializeField] [Range(0, 1)] private float cockingAutoLoadMargin;

        [Header("Twisting")]
        [Tooltip("If checked, Gun will behave like a bolt action.")]
        [SerializeField] private bool requireTwist;

        [Tooltip("If checked, CustomHandle's rotation will be used to calculate twist angle. Like a welrod.")]
        [SerializeField] private bool useHandleRotation;

        [SerializeField] private Transform idleTwistPosition;

        [SerializeField] private Transform activeTwistPosition;

        [SerializeField] private float twistAngleOffset;

        [SerializeField] private float twistMaxAngle;

        [SerializeField] private float twistMinAngle;

        [SerializeField] [Range(0, 1)] private float twistMargin;

        [SerializeField] [Range(0, 1)] private float twistAutoLoadMargin;

        [Header("Desktop")] [SerializeField] private bool doDesktopCockingAutomatically = true;

        [SerializeField] private KeyCode desktopCockingKey = KeyCode.F;

        [SerializeField] private float desktopCockingTime = 1F;

        [Header("Haptic")] [SerializeField] private GunCockingHapticDataStore cockingHapticData;

        private float _desktopCockingTimer;
        private bool _isDesktopCockingCurrentlyPulling;

        private bool _isOnDesktopCocking;

        private void Start()
        {
            // assign placeholders
            if (idleTwistPosition == null)
                idleTwistPosition = cockingPosition;
            if (activeTwistPosition == null)
                activeTwistPosition = cockingPosition;
        }

        private void GetNormalizedProgressAndTwist(GunBase instance, out float progress, out float twist)
        {
            var currentState = instance.State;
            progress = GetProgressNormalized();
            twist = GetTwistNormalized();

            if (!requireTwist) twist = 1;

            if (twist < 1 - twistMargin && (currentState == GunState.InCockingTwisting || currentState == GunState.Idle))
            {
                progress = Mathf.Clamp(progress, 0, cockingMargin);
            }
            else if (progress > cockingMargin)
            {
                twist = Mathf.Clamp(twist, 1 - twistMargin, 1);
            }

            // Lock the action closed so a chambered round can't be ejected by cocking again
            if (!canCockAfterCock && instance.State == GunState.Idle && instance.HasBulletInChamber)
            {
                progress = Mathf.Clamp(progress, 0, cockingMargin);
                twist = Mathf.Clamp(twist, 0, twistMargin);
            }
        }

        private float GetProgressNormalized()
        {
            return GetProgress() / cockingLength;
        }

        private float GetProgress()
        {
            return Mathf.Clamp(-cockingPosition.worldToLocalMatrix.MultiplyPoint3x4(customHandle.transform.position).z, 0, cockingLength);
        }

        private float GetTwistNormalized()
        {
            return GetTwist() / twistMaxAngle;
        }

        private float GetTwist()
        {
            var customHandlePos = customHandle.transform.localPosition;
            if (useHandleRotation) customHandlePos += (Vector3)(Vector2)(customHandle.transform.localRotation * Vector3.up);
            var oriented = Quaternion.AngleAxis(twistAngleOffset, Vector3.forward) * (customHandlePos - cockingPosition.localPosition);
            var signedAngle = Vector2.SignedAngle(Vector3.up, oriented.normalized);
            return Mathf.Clamp(signedAngle, twistMinAngle, twistMaxAngle);
        }

        private Transform GetIdleTransform()
        {
            return requireTwist && idleTwistPosition != null ? idleTwistPosition : cockingPosition;
        }

        private Transform GetActiveTransform()
        {
            return requireTwist && activeTwistPosition != null ? activeTwistPosition : cockingPosition;
        }

        /// <summary>
        /// Clamps local Z of the CustomHandle onto the cocking rail, keeping X/Y of <paramref name="railOrigin"/>.
        /// </summary>
        private Vector3 GetPositionOnRail(Vector3 railOrigin, float handleZ)
        {
            var cockingZ = cockingPosition.localPosition.z;
            return new Vector3(railOrigin.x, railOrigin.y, Mathf.Clamp(handleZ, cockingZ - cockingLength, cockingZ));
        }

        private void UpdateCustomHandlePosition(GunBase instance)
        {
            Vector3 targetPos;
            Quaternion targetRot;
            switch (instance.State)
            {
                default:
                case GunState.Idle:
                case GunState.Unknown:
                {
                    var t = GetIdleTransform();
                    targetPos = t.localPosition;
                    targetRot = t.localRotation;
                    break;
                }
                case GunState.InCockingTwisting:
                {
                    var t = GetActiveTransform();
                    targetPos = t.localPosition;
                    targetRot = t.localRotation;
                    break;
                }
                case GunState.InCockingPull:
                case GunState.InCockingPush:
                {
                    targetPos = cockingPosition.localPosition + cockingPosition.localRotation * new Vector3(0, 0, -cockingLength);
                    targetRot = GetActiveTransform().localRotation;
                    break;
                }
            }

            customHandle.transform.SetLocalPositionAndRotation(targetPos, targetRot);
        }

        /// <summary>
        /// Forces the CustomHandle out of the player's hand and snaps it to where the current state expects.
        /// </summary>
        private void ResetCustomHandle(GunBase instance)
        {
            customHandle.ForceDrop();
            UpdateCustomHandlePosition(instance);
        }

        private void DoCocking(GunBase instance)
        {
            float progressNormalized, twistNormalized;
            if (instance.IsVR)
                GetNormalizedProgressAndTwist(instance, out progressNormalized, out twistNormalized);
            else
                GetDesktopProgressAndTwist(instance, out progressNormalized, out twistNormalized);

            ApplyCockingProgress(instance, progressNormalized, twistNormalized);
        }

        /// <summary>
        /// Desktop has no hand to pull the CustomHandle with, so animate progress and twist over <see cref="desktopCockingTime"/>.
        /// </summary>
        private void GetDesktopProgressAndTwist(GunBase instance, out float progressNormalized, out float twistNormalized)
        {
            // Initiate desktop cocking on key press
            var cockingInput = Input.GetKeyDown(desktopCockingKey) || doDesktopCockingAutomatically;
            var shouldCock = ((!instance.HasCocked && !isBlowBack) || !instance.HasBulletInChamber) &&
                             instance._HasNextBullet();

            if (!_isOnDesktopCocking && shouldCock && cockingInput)
            {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
                Debug.Log("[CockingGunBehaviour] Begin Desktop Reloading");
#endif
                _isOnDesktopCocking = true;
                _isDesktopCockingCurrentlyPulling = instance.State == GunState.Idle;
                _desktopCockingTimer = _isDesktopCockingCurrentlyPulling ? 0F : desktopCockingTime / 2F;
            }

            if (!_isOnDesktopCocking)
            {
                progressNormalized = instance.State == GunState.Idle ? 0 : 1;
                twistNormalized = !requireTwist || instance.State == GunState.Idle ? 0 : 1;
                return;
            }

            // With twist, first quarter twists and second quarter pulls; without, first half pulls
            var timeScale = requireTwist ? desktopCockingTime / 4 : desktopCockingTime / 2;
            var progressTime = requireTwist ? _desktopCockingTimer - timeScale : _desktopCockingTimer;
            var twistTime = _desktopCockingTimer;

            progressNormalized = Mathf.Clamp01(progressTime / timeScale);
            twistNormalized = Mathf.Clamp01(twistTime / timeScale);

            if (_isDesktopCockingCurrentlyPulling)
            {
                _desktopCockingTimer += Time.deltaTime;

                // If completed pulling back process
                if (progressNormalized >= 1) _isDesktopCockingCurrentlyPulling = false;
            }
            else
            {
                _desktopCockingTimer -= Time.deltaTime;

                // If completed reloading process
                if (_desktopCockingTimer <= 0)
                {
                    _isOnDesktopCocking = false;
                    _desktopCockingTimer = 0F;
                }
            }
        }

        private void ApplyCockingProgress(GunBase instance, float progressNormalized, float twistNormalized)
        {
            if (requireTwist)
            {
                GunUtility.UpdateStateBoltAction(
                    instance,
                    cockingHapticData,
                    customHandle.CurrentHand,
                    progressNormalized,
                    cockingAutoLoadMargin,
                    1 - cockingAutoLoadMargin,
                    twistNormalized,
                    twistMargin
                );
            }
            else
            {
                GunUtility.UpdateStateStraightPull(
                    instance,
                    cockingHapticData,
                    customHandle.CurrentHand,
                    progressNormalized,
                    cockingAutoLoadMargin,
                    1 - cockingAutoLoadMargin
                );
            }
        }

        /// <summary>
        /// Where the CustomHandle should rest after being let go, depending on how far it was pulled and twisted.
        /// </summary>
        private Vector3 GetDropPosition(GunBase instance, Vector3 handlePos)
        {
            if (returnToCockingPositionOnDrop)
            {
                if (cockingHapticData && cockingHapticData.Done && instance)
                    cockingHapticData.Done.PlayInHand(instance.MainHandle.CurrentHand);
                return cockingPosition.localPosition;
            }

            if (requireTwist)
            {
                GetNormalizedProgressAndTwist(instance, out var progressNormalized, out var twistNormalized);

                return twistAutoLoadMargin > twistNormalized && cockingAutoLoadMargin > progressNormalized
                    ? idleTwistPosition.localPosition
                    : GetPositionOnRail(activeTwistPosition.localPosition, handlePos.z);
            }

            return cockingAutoLoadMargin > GetProgressNormalized()
                ? cockingPosition.localPosition
                : GetPositionOnRail(cockingPosition.localPosition, handlePos.z);
        }

        private Vector3 GetClosestReferencePosition(Vector3 handlePos, Vector3 fallback)
        {
            var transforms = new[]
            {
                cockingPosition,
                idleTwistPosition,
                activeTwistPosition,
            };

            var result = fallback;
            var closestDistance = Mathf.Infinity;
            foreach (var reference in transforms)
            {
                if (reference == null) continue;

                var refPos = reference.localPosition;
                var distance = Vector3.Distance(refPos, handlePos);
                if (distance >= closestDistance) continue;

                result = refPos;
                closestDistance = distance;
            }

            return result;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            DrawGizmos();
            if (requireTwist)
                DrawTwistGizmos();
        }

        private void DrawGizmos()
        {
            // NOTE: cocking pos might be null
            var cockingPos = cockingPosition.position;
            GizmosUtil.SetColor(Color.cyan, 0.8F);
            GizmosUtil.DrawArrow(cockingPos, cockingPos + -cockingLength * cockingPosition.forward, 0.01F);
        }

        private void DrawTwistGizmos()
        {
            GizmosUtil.SetColor(Color.green, 0.5F);
            GizmosUtil.DrawWireSphere(idleTwistPosition.position, 0.01F);
            GizmosUtil.DrawWireSphere(activeTwistPosition.position, 0.01F);

            var cockingPos = cockingPosition.position;
            var toOffset = cockingLength * cockingPosition.forward * -1;
            var twistOffset = activeTwistPosition.position - cockingPos;
            var twistOffsetCockingPos = cockingPos + twistOffset;
            GizmosUtil.SetColor(Color.blue, 0.8F);
            // TODO: make twist offset pos properly based on cockingPosition transform
            GizmosUtil.DrawArrow(twistOffsetCockingPos, twistOffsetCockingPos + toOffset, 0.01F);

            var segments = Mathf.RoundToInt(twistMaxAngle / 2);
            if (Mathf.RoundToInt(twistMaxAngle / segments) >= 0)
            {
                GizmosUtil.DrawWireArc(cockingPos, twistOffset.magnitude, twistMaxAngle, segments,
                    Quaternion.Euler(twistAngleOffset - twistMinAngle, -90, -90), cockingPos);
            }
        }
#endif

        #region GunBehaviourBase
        public override void OnGunPickup(GunBase instance)
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log("[CockingGunBehaviour] OnGunPickup");
#endif
            UpdateCustomHandlePosition(instance);
            customHandle.SetPickupable(true);
        }

        public override void OnGunDrop(GunBase instance)
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log("[CockingGunBehaviour] OnGunDrop");
#endif
            UpdateCustomHandlePosition(instance);
            customHandle.SetPickupable(false);
        }

        public override void OnGunUpdate(GunBase instance)
        {
            if (instance.Trigger == TriggerState.Firing)
                TryToShoot(instance);

            // In VR, cocking only progresses while the CustomHandle is held
            if (instance.IsVR && !customHandle.IsPickedUpLocally)
                return;

            DoCocking(instance);
        }

        /// <summary>
        /// Shoots whenever it's able to, then cycles the action according to the configured action type.
        /// </summary>
        private void TryToShoot(GunBase instance)
        {
            if (isDoubleAction && !instance.HasCocked)
            {
                instance.HasCocked = true;
            }

            var shotResult = instance._TryToShoot();
            if (shotResult != ShotResult.Succeeded && shotResult != ShotResult.SucceededContinuously)
                return;

            // ForceDrop on CustomHandle calls back into OnHandleDrop, so the order of steps below matters
            if (doHoldOpenOnLastBullet && instance.BulletsInMagazine == 0 && instance.ReloadHelper.HasMagazine)
            {
                instance.State = GunState.InCockingPull;
                ResetCustomHandle(instance);
                instance.AnimationHelper._SetCockingProgress(1);
            }

            if (isBlowBack || requireManualPushingAfterFire)
            {
                instance.HasCocked = true;
                instance._LoadBullet();
            }

            if (requireManualPushingAfterFire)
            {
                instance.State = GunState.InCockingPull;
                instance.AnimationHelper._SetCockingProgress(1);
            }

            if (dropCustomHandleOnFire)
            {
                ResetCustomHandle(instance);
            }
        }

        public override void Setup(GunBase instance)
        {
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log($"[CockingGunBehaviour] setup called for {instance.name}");
#endif
            var state = instance.State;
            if (state == GunState.Unknown)
            {
                Debug.LogWarning(
                    $"[CockingGunBehaviour] resetting state because it was invalid: {state.GetStateString()}");
                instance.State = GunState.Idle;
            }

            customHandle.handleType = HandleType.CustomHandle;
            customHandle.callback = instance;
            customHandle.transform.SetPositionAndRotation(cockingPosition.position, Quaternion.identity);
            customHandle.SetPickupable(false);

            if (hotOnSetup)
            {
                instance.HasCocked = true;
                if (!instance.HasBulletInChamber) instance._LoadBullet();
            }
        }

        public override void OnHandleDrop(GunBase instance, GunHandle handle)
        {
            if (handle.handleType != HandleType.CustomHandle)
                return;

            var handlePos = customHandle.transform.localPosition;
            var expectedPos = GetDropPosition(instance, handlePos);
            if (snapToClosestPositionOnDrop)
                expectedPos = GetClosestReferencePosition(handlePos, expectedPos);

            handle.MoveToLocalPosition(expectedPos, Quaternion.identity);
#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log($"[CockingGunBehaviour] OnHandleDrop: {instance.name} moved handle to {expectedPos.ToString("F2")}");
#endif

            // I have no idea, but calling DoCocking twice will fix the desync issue
            DoCocking(instance);
            DoCocking(instance);
        }
        #endregion
    }
}
