using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GunManagerEventHelper : UdonSharpBehaviour
    {

        private const string Prefix = "[<color=olive>GunManagerEvent</color>] ";
        [SerializeField] [NewbieInject]
        private PrintableBase logger;

        private int _callbackCount;
        private UdonSharpBehaviour[] _eventCallbacks = new UdonSharpBehaviour[0];

        [PublicAPI]
        public bool Subscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.AddBehaviour(behaviour, ref _callbackCount, ref _eventCallbacks);
        }

        [PublicAPI]
        public bool Unsubscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.RemoveBehaviour(behaviour, ref _callbackCount, ref _eventCallbacks);
        }

        [NetworkCallable]
        public void Invoke_OnGunsReset(GunManagerResetType type)
        {
            logger.Log($"{Prefix}OnGunsResetAll");

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                if (callback) callback.OnGunsReset(type);
            }
        }

        public void Invoke_OnVariantChanged(GunBase instance)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnVariantChanged: Instance is null"))
                return;

            logger.Log($"{Prefix}OnVariantChanged: {instance.name}");

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnVariantChanged(instance);
            }
        }

        public void Invoke_OnPickedUpLocally(GunBase instance)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnPickedUpLocally: Instance is null"))
                return;

            logger.Log($"{Prefix}OnPickedUpLocally: {instance.name}");

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnPickedUpLocally(instance);
            }
        }

        public void Invoke_OnDropLocally(GunBase instance)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnDropLocally: Instance is null"))
                return;

            logger.Log($"{Prefix}OnDropLocally: {instance.name}");

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnDropLocally(instance);
            }
        }

        public void Invoke_OnShoot(GunBase instance, ProjectileBase projectile)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnShoot: Instance is null") |
                ErrorDiagnostic.Assert(projectile, "GunManagerEventHelper:OnShoot: Projectile is null"))
                return;

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnShoot: {instance.name}");
#endif

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnShoot(instance, projectile);
            }
        }

        public void Invoke_OnEmptyShoot(GunBase instance)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnEmptyShoot: Instance is null"))
                return;

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnEmptyShoot: {instance.name}");
#endif

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnEmptyShoot(instance);
            }
        }

        public void Invoke_OnShootFailed(GunBase instance, int reasonId)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnShootFailed: Instance is null"))
                return;

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnShootFailed: {instance.name}, {reasonId}");
#endif

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnShootFailed(instance, reasonId);
            }
        }

        public void Invoke_OnShootCancelled(GunBase instance, int reasonId)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnShootCancelled: Instance is null"))
                return;

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnShootCancelled: {instance.name}, {reasonId}");
#endif

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnShootCancelled(instance, reasonId);
            }
        }

        public void Invoke_OnFireModeChanged(GunBase instance)
        {
            if (ErrorDiagnostic.Assert(instance, "GunManagerEventHelper:OnFireModeChanged: Instance is null"))
                return;

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            logger.Log($"{Prefix}OnFireModeChanged: {instance.name}");
#endif

            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                callback.OnFireModeChanged(instance);
            }
        }

        public bool Invoke_CanShoot()
        {
            for (var i = 0; i < _callbackCount; i++)
            {
                var callback = (GunManagerCallbackBase)_eventCallbacks[i];
                if (!callback.CanShoot()) return false;
            }

            return true;
        }
    }
}
