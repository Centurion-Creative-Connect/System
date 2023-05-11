using UdonSharp;

namespace CenturionCC.System.UI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class NotificationProvider : UdonSharpBehaviour
    {
        public virtual void ShowHelp(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Help, message);
        }

        public virtual void ShowInfo(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Info, message);
        }

        public virtual void ShowWarn(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Warn, message);
        }

        public virtual void ShowError(string message, float duration = 5F, int id = 0)
        {
            Show(NotificationLevel.Error, message);
        }

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