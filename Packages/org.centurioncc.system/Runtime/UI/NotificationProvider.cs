using JetBrains.Annotations;
using UdonSharp;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class NotificationProvider : UdonSharpBehaviour
    {
        /// <summary>
        /// Show notification with Help level.
        /// </summary>
        /// <param name="message">message that should be show up on notification</param>
        /// <param name="duration">how long notification should be shown for in seconds</param>
        /// <param name="id">id of this message for stacking, 0 for auto reference <paramref name="message"/></param>
        /// <seealso cref="Show"/>
        [PublicAPI]
        public virtual void ShowHelp(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Help, message);
        }

        /// <summary>
        /// Show notification with Info level.
        /// </summary>
        /// <param name="message">message that should be show up on notification</param>
        /// <param name="duration">how long notification should be shown for in seconds</param>
        /// <param name="id">id of this message for stacking, 0 for auto reference <paramref name="message"/></param>
        /// <seealso cref="Show"/>
        [PublicAPI]
        public virtual void ShowInfo(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Info, message);
        }

        /// <summary>
        /// Show notification with Warn level.
        /// </summary>
        /// <param name="message">message that should be show up on notification</param>
        /// <param name="duration">how long notification should be shown for in seconds</param>
        /// <param name="id">id of this message for stacking, 0 for auto reference <paramref name="message"/></param>
        /// <seealso cref="Show"/>
        [PublicAPI]
        public virtual void ShowWarn(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Warn, message);
        }

        /// <summary>
        /// Show notification with Error level.
        /// </summary>
        /// <param name="message">message that should be show up on notification</param>
        /// <param name="duration">how long notification should be shown for in seconds</param>
        /// <param name="id">id of this message for stacking, 0 for auto reference <paramref name="message"/></param>
        /// <seealso cref="Show"/>
        [PublicAPI]
        public virtual void ShowError(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Error, message);
        }

        /// <summary>
        /// Show notification with full options
        /// </summary>
        /// <param name="level">which level should message should be shown with</param>
        /// <param name="message">message that should be show up on notification</param>
        /// <param name="duration">how long notification should be shown for in seconds</param>
        /// <param name="id">id of this message for stacking, 0 for auto reference <paramref name="message"/></param>
        [PublicAPI]
        public abstract void Show(NotificationLevel level, string message, float duration = 5F, int id = 0);
    }

    public enum NotificationLevel
    {
        Help,
        Info,
        Warn,
        Error
    }
}