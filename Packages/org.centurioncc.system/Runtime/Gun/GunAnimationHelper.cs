using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GunAnimationHelper : UdonSharpBehaviour
    {
        private readonly int _cockingProgressAnimHash = Animator.StringToHash(GunUtility.CockingProgressParamName);
        private readonly int _cockingTwistAnimHash = Animator.StringToHash(GunUtility.CockingTwistParamName);
        private readonly int _hasBulletAnimHash = Animator.StringToHash(GunUtility.HasBulletParamName);
        private readonly int _hasCockedAnimHash = Animator.StringToHash(GunUtility.HasCockedParamName);
        private readonly int _isInWallAnimHash = Animator.StringToHash(GunUtility.IsInWallParamName);
        private readonly int _isLocalAnimHash = Animator.StringToHash(GunUtility.IsLocalParamName);
        private readonly int _isPickedUpGlobalAnimHash = Animator.StringToHash(GunUtility.IsPickedUpGlobalParamName);
        private readonly int _isPickedUpLocalAnimHash = Animator.StringToHash(GunUtility.IsPickedUpLocalParamName);
        private readonly int _isShootingAnimHash = Animator.StringToHash(GunUtility.IsShootingParamName);
        private readonly int _isShootingEmptyAnimHash = Animator.StringToHash(GunUtility.IsShootingEmptyParamName);
        private readonly int _isVRAnimHash = Animator.StringToHash(GunUtility.IsVRParamName);
        private readonly int _selectorTypeAnimHash = Animator.StringToHash(GunUtility.SelectorTypeParamName);
        private readonly int _stateAnimHash = Animator.StringToHash(GunUtility.StateParamName);
        private readonly int _triggerProgressAnimHash = Animator.StringToHash(GunUtility.TriggerProgressParamName);
        private readonly int _triggerStateAnimHash = Animator.StringToHash(GunUtility.TriggerStateParamName);

        public string[] SyncedParameterNames { get; set; }
        public Animator TargetAnimator { get; set; }

        public void _SetState(int state)
        {
            if (TargetAnimator) TargetAnimator.SetInteger(_stateAnimHash, state);
        }

        public void _SetSelectorType(int selectorType)
        {
            if (TargetAnimator) TargetAnimator.SetInteger(_selectorTypeAnimHash, selectorType);
        }

        public void _SetTriggerState(int triggerState)
        {
            if (TargetAnimator) TargetAnimator.SetInteger(_triggerStateAnimHash, triggerState);
        }

        public void _SetHasBullet(bool hasBullet)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_hasBulletAnimHash, hasBullet);
        }

        public void _SetHasCocked(bool hasCocked)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_hasCockedAnimHash, hasCocked);
        }

        public void _SetLocal(bool isLocal)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_isLocalAnimHash, isLocal);
        }

        [NetworkCallable]
        public void SetVR(bool isVR)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_isVRAnimHash, isVR);
        }

        public void _SetIsInWall(bool isInWall)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_isInWallAnimHash, isInWall);
        }

        public void _SetTriggerProgress(float progress)
        {
            if (TargetAnimator) TargetAnimator.SetFloat(_triggerProgressAnimHash, progress);
        }

        public void _SetCockingProgress(float progress)
        {
            if (TargetAnimator) TargetAnimator.SetFloat(_cockingProgressAnimHash, progress);
        }

        public void _SetTwistingProgress(float progress)
        {
            if (TargetAnimator) TargetAnimator.SetFloat(_cockingTwistAnimHash, progress);
        }

        public void _SetShooting()
        {
            if (TargetAnimator) TargetAnimator.SetTrigger(_isShootingAnimHash);
        }

        public void _SetEmptyShooting()
        {
            if (TargetAnimator) TargetAnimator.SetTrigger(_isShootingEmptyAnimHash);
        }

        public void _SetPickedUpLocally(bool isPickedUpLocally)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_isPickedUpLocalAnimHash, isPickedUpLocally);
        }

        [NetworkCallable]
        public void SetPickedUpGlobally(bool isPickedUpGlobally)
        {
            if (TargetAnimator) TargetAnimator.SetBool(_isPickedUpGlobalAnimHash, isPickedUpGlobally);
        }

        [NetworkCallable]
        public void SetCustomFloat(int idx, float value)
        {
            if (!TargetAnimator || SyncedParameterNames.Length <= idx) return;
            TargetAnimator.SetFloat(SyncedParameterNames[idx], value);
        }

        [NetworkCallable]
        public void SetCustomInt(int idx, int value)
        {
            if (!TargetAnimator || SyncedParameterNames.Length <= idx) return;
            TargetAnimator.SetInteger(SyncedParameterNames[idx], value);
        }

        [NetworkCallable]
        public void SetCustomBool(int idx, bool value)
        {
            if (!TargetAnimator || SyncedParameterNames.Length <= idx) return;
            TargetAnimator.SetBool(SyncedParameterNames[idx], value);
        }

        [NetworkCallable]
        public void SetCustomTrigger(int idx)
        {
            if (!TargetAnimator || SyncedParameterNames.Length <= idx) return;
            TargetAnimator.SetTrigger(SyncedParameterNames[idx]);
        }

        [PublicAPI]
        public void _SetCustomFloatSynced(string parameter, float value)
        {
            var idx = SyncedParameterNames.FindItem(parameter);
            if (idx == -1) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SetCustomFloat), idx, value);
        }

        [PublicAPI]
        public void _SetCustomIntSynced(string parameter, int value)
        {
            var idx = SyncedParameterNames.FindItem(parameter);
            if (idx == -1) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SetCustomInt), idx, value);
        }

        [PublicAPI]
        public void _SetCustomBoolSynced(string parameter, bool value)
        {
            var idx = SyncedParameterNames.FindItem(parameter);
            if (idx == -1) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SetCustomBool), idx, value);
        }

        [PublicAPI]
        public void _SetCustomTriggerSynced(string parameter)
        {
            var idx = SyncedParameterNames.FindItem(parameter);
            if (idx == -1) return;
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SetCustomTrigger), idx);
        }

        [PublicAPI]
        public void _SyncAllParameters()
        {
            if (!TargetAnimator) return;

            for (var i = 0; i < SyncedParameterNames.Length; i++)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SetCustomFloat), i, TargetAnimator.GetFloat(SyncedParameterNames[i]));
            }
        }
    }
}
