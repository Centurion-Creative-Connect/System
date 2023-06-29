using CenturionCC.System.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace CenturionCC.System.Player
{
    public abstract class DamageDataResolverBase : PlayerManagerCallbackBase
    {
        public override void OnHitDetection(PlayerCollider playerCollider, DamageData damageData, Vector3 contactPoint)
        {
            Resolve(playerCollider, damageData, contactPoint);
        }

        public abstract void Resolve(
            [CanBeNull] PlayerCollider collider,
            [CanBeNull] DamageData damageData,
            Vector3 contactPoint
        );

        public abstract void RequestResolve(ResolverDataSyncer requester);

        public abstract void RequestResend(ResolverDataSyncer requester);
    }
}