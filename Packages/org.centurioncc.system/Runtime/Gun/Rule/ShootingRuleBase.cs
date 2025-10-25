using DerpyNewbie.Common;
using UdonSharp;

namespace CenturionCC.System.Gun.Rule
{
    public abstract class ShootingRuleBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Unique Rule Id to retrieve cancelled message.
        /// </summary>
        /// <remarks>
        /// Must be greater than 2000 to avoid conflict.
        /// int.MinValue to 500 is reserved for internal codes.
        /// 501 to 1999 is reserved for default rules.
        /// </remarks>
        public abstract int RuleId { get; }

        /// <summary>
        /// Cancellation message to notify player what's wrong.
        /// </summary>
        /// <remarks>
        /// Will be used by <see cref="GunManagerNotificationSender"/> to clearly state players what they did wrong for
        /// being unable to shoot.
        /// </remarks>
        public abstract TranslatableMessage CancelledMessage { get; }

        /// <summary>
        /// Determines <see cref="instance"/> is able to shoot or not.
        /// </summary>
        /// <param name="instance">GunBase instance that is trying to shoot.</param>
        /// <returns>true if it's able to shoot. false otherwise.</returns>
        public abstract bool CanLocalShoot(GunBase instance);
    }
}
