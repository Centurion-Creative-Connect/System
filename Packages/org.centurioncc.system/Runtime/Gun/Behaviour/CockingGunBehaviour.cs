using CenturionCC.System.Gun.DataStore;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CockingGunBehaviour : GunBehaviourBase
    {
        [SerializeField] private GunHandle customHandle;

        [SerializeField] private bool returnToCockingPositionOnDrop;

        [SerializeField] private bool snapToClosestPositionOnDrop;

        [Header("Cocking")] [SerializeField] private bool canCockAfterCock;

        [SerializeField] private bool isBlowBack;

        [SerializeField] private bool isDoubleAction;

        [SerializeField] private bool doHoldOpenOnLastBullet = true;

        [SerializeField] private bool requireManualPushingAfterFire;

        [SerializeField] private bool dropCustomHandleOnFire = true;

        [SerializeField] private Transform cockingPosition;

        [SerializeField] private float cockingLength;

        [SerializeField] [Range(0, 1)] private float cockingMargin;

        [SerializeField] [Range(0, 1)] private float cockingAutoLoadMargin;

        [Header("Twisting")] [SerializeField] private bool requireTwist;

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

            if (!canCockAfterCock && instance.State == GunState.Idle && instance.HasBulletInChamber)
            {
                progress = Mathf.Clamp(progress, 0, cockingMargin);
                progress = Mathf.Clamp(twist, 1 - twistMargin, 1);
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
                    var t = requireTwist && idleTwistPosition != null ? idleTwistPosition : cockingPosition;
                    targetPos = t.localPosition;
                    targetRot = t.localRotation;
                    break;
                }
                case GunState.InCockingTwisting:
                {
                    var t = requireTwist && activeTwistPosition != null ? activeTwistPosition : cockingPosition;
                    targetPos = t.localPosition;
                    targetRot = t.localRotation;
                    break;
                }
                case GunState.InCockingPull:
                case GunState.InCockingPush:
                {
                    var t = requireTwist && activeTwistPosition != null ? activeTwistPosition : cockingPosition;
                    targetPos = cockingPosition.localPosition + cockingPosition.localRotation * new Vector3(0, 0, -cockingLength);
                    targetRot = t.localRotation;
                    break;
                }
            }

            var handleTransform = customHandle.transform;
            handleTransform.localPosition = targetPos;
            handleTransform.localRotation = targetRot;
        }

        private void DoCocking(GunBase instance)
        {
            float progressNormalized, twistNormalized;
            // Calculate cocking/twist progress
            if (instance.IsVR)
            {
                GetNormalizedProgressAndTwist(instance, out progressNormalized, out twistNormalized);
            }
            else
            {
                // Initiate desktop cocking on key press
                var cockingInput = Input.GetKeyDown(desktopCockingKey) || doDesktopCockingAutomatically;
                var shouldCock = instance.State == GunState.Idle &&
                                 ((!instance.HasCocked && !isBlowBack) || !instance.HasBulletInChamber) &&
                                 instance._HasNextBullet();
                if (!_isOnDesktopCocking && shouldCock && cockingInput)
                {
                    Debug.Log("[CockingGunBehaviour] Begin Desktop Reloading");
                    _isOnDesktopCocking = true;
                    _isDesktopCockingCurrentlyPulling = true;
                    _desktopCockingTimer = 0F;
                }

                // Do desktop cocking work
                if (_isOnDesktopCocking)
                {
                    var timeScale = requireTwist ? desktopCockingTime / 4 : desktopCockingTime / 2;
                    var progressTime = requireTwist ? _desktopCockingTimer - timeScale : _desktopCockingTimer;
                    var twistTime = _desktopCockingTimer;

                    if (_isDesktopCockingCurrentlyPulling)
                    {
                        _desktopCockingTimer += Time.deltaTime;

                        twistNormalized = Mathf.Clamp01(twistTime / timeScale);
                        progressNormalized = Mathf.Clamp01(progressTime / timeScale);

                        // If completed pulling back process
                        if (progressNormalized >= 1) _isDesktopCockingCurrentlyPulling = false;
                    }
                    else
                    {
                        _desktopCockingTimer -= Time.deltaTime;

                        progressNormalized = Mathf.Clamp01(progressTime / timeScale);
                        twistNormalized = Mathf.Clamp01(twistTime / timeScale);

                        // If completed reloading process
                        if (_desktopCockingTimer <= 0)
                        {
                            _isOnDesktopCocking = false;
                            _desktopCockingTimer = 0F;
                        }
                    }
                }
                else
                {
                    progressNormalized = 0;
                    twistNormalized = 0;
                }
            }

            // Change states using GunUtility
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
            Debug.Log("[CockingGunBehaviour] OnGunPickup");
            UpdateCustomHandlePosition(instance);
            customHandle.SetPickupable(true);
        }

        public override void OnGunDrop(GunBase instance)
        {
            Debug.Log("[CockingGunBehaviour] OnGunDrop");
            UpdateCustomHandlePosition(instance);
            customHandle.SetPickupable(false);
        }

        public override void OnGunUpdate(GunBase instance)
        {
            // Shoot a gun whenever it's able to shoot. load a new bullet if it's a blow back variant
            if (instance.Trigger == TriggerState.Firing)
            {
                var shotResult = instance._TryToShoot();
                var hasSucceeded = shotResult == ShotResult.Succeeded || shotResult == ShotResult.SucceededContinuously;
                if (hasSucceeded)
                {
                    if (doHoldOpenOnLastBullet && instance.BulletsInMagazine == 0)
                    {
                        instance.State = GunState.InCockingPull;
                        customHandle.ForceDrop();
                        UpdateCustomHandlePosition(instance);
                        instance.AnimationHelper._SetCockingProgress(1);
                    }

                    if (isBlowBack || requireManualPushingAfterFire)
                    {
                        instance.HasCocked = true;
                        instance._LoadBullet();
                    }

                    if (requireManualPushingAfterFire)
                    {
                        instance.State = GunState.InCockingPush;
                    }

                    if (dropCustomHandleOnFire)
                    {
                        customHandle.ForceDrop();
                        UpdateCustomHandlePosition(instance);
                    }
                }
            }

            if (instance.IsVR && !customHandle.IsPickedUpLocally)
            {
                Debug.Log("wont process cocking");
                return;
            }

            DoCocking(instance);
        }

        public override void Setup(GunBase instance)
        {
            Debug.Log($"[CockingGunBehaviour] setup called for {instance.name}");
            var state = instance.State;
            if (state == GunState.Unknown)
            {
                Debug.LogWarning(
                    $"[CockingGunBehaviour] resetting state because it was invalid: {state.GetStateString()}");
                instance.State = GunState.Idle;
            }

            customHandle.callback = instance;
            customHandle.transform.SetPositionAndRotation(cockingPosition.position, Quaternion.identity);
        }

        public override void OnHandleDrop(GunBase instance, GunHandle handle)
        {
            if (handle.handleType != HandleType.CustomHandle)
            {
                Debug.Log(
                    $"[CockingGunBehaviour] OnHandleDrop: {instance.name} handle.handleId ({handle.handleType}) != {HandleType.CustomHandle}");
                return;
            }

            Vector3 expectedPos;
            var handlePos = customHandle.transform.localPosition;
            var cockingPos = cockingPosition.localPosition;

            if (returnToCockingPositionOnDrop)
            {
                expectedPos = cockingPosition.localPosition;
                if (cockingHapticData && cockingHapticData.Done && instance)
                    cockingHapticData.Done.PlayInHand(instance.MainHandle.CurrentHand);
            }
            else if (requireTwist)
            {
                GetNormalizedProgressAndTwist(instance, out var progressNormalized, out var twistNormalized);

                if (requireTwist && twistAutoLoadMargin > twistNormalized && cockingAutoLoadMargin > progressNormalized)
                {
                    expectedPos = idleTwistPosition.localPosition;
                }
                else
                {
                    var atp = activeTwistPosition.localPosition;
                    var expectedZ = Mathf.Clamp(handlePos.z, cockingPos.z - cockingLength, cockingPos.z);
                    expectedPos = new Vector3(atp.x, atp.y, expectedZ);
                }
            }
            else
            {
                if (cockingAutoLoadMargin > GetProgressNormalized())
                {
                    expectedPos = cockingPosition.localPosition;
                }
                else
                {
                    var expectedZ = Mathf.Clamp(handlePos.z, cockingPos.z - cockingLength, cockingPos.z);
                    expectedPos = new Vector3(cockingPos.x, cockingPos.y, expectedZ);
                }
            }

            if (snapToClosestPositionOnDrop)
            {
                // TODO: check for closest handles
            }

            handle.MoveToLocalPosition(expectedPos, Quaternion.identity);
            Debug.Log($"[CockingGunBehaviour] OnHandleDrop: {instance.name} moved handle to {expectedPos.ToString("F2")}");

            // I have no idea, but calling DoCocking twice will fix the desync issue
            DoCocking(instance);
            DoCocking(instance);
        }
        #endregion
    }
}
