using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gun.Behaviour
{
    public class SendCustomEventBehaviour : GunBehaviourBase
    {
        [SerializeField]
        private UdonSharpBehaviour[] callbacks;

        [Header("Gun Events")]
        [SerializeField]
        private string onTriggerDownEvent;
        [SerializeField]
        private string onTriggerUpEvent;
        [SerializeField]
        private string onFireModeChangeEvent;
        [SerializeField]
        private string onActionEvent;
        [SerializeField]
        private string onGunPickupEvent;
        [SerializeField]
        private string onGunDropEvent;
        [SerializeField] [Tooltip("Sent every frame while assigned to a gun.")]
        private string onGunUpdateEvent;

        [Header("Handle Events")]
        [SerializeField] [Tooltip("Only handles of this type send handle events. None sends events for all handles.")]
        private HandleType handleTypeFilter = HandleType.None;
        [SerializeField]
        private string onHandlePickupEvent;
        [SerializeField]
        private string onHandleUseDownEvent;
        [SerializeField]
        private string onHandleUseUpEvent;
        [SerializeField]
        private string onHandleDropEvent;

        [Header("Lifecycle Events")]
        [SerializeField]
        private string onSetupEvent;
        [SerializeField]
        private string onDisposeEvent;

        private void _NotifyCallback(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return;

            foreach (var callback in callbacks)
            {
                if (callback) callback.SendCustomEvent(eventName);
            }
        }

        private void _NotifyHandleCallback(GunHandle handle, string eventName)
        {
            if (handleTypeFilter != HandleType.None && (!handle || handle.handleType != handleTypeFilter)) return;
            _NotifyCallback(eventName);
        }

        public override void OnTriggerDown(GunBase instance)
        {
            _NotifyCallback(onTriggerDownEvent);
        }

        public override void OnTriggerUp(GunBase instance)
        {
            _NotifyCallback(onTriggerUpEvent);
        }

        public override void OnFireModeChange(GunBase instance)
        {
            _NotifyCallback(onFireModeChangeEvent);
        }

        public override void OnAction(GunBase instance)
        {
            _NotifyCallback(onActionEvent);
        }

        public override void OnGunPickup(GunBase instance)
        {
            _NotifyCallback(onGunPickupEvent);
        }

        public override void OnGunDrop(GunBase instance)
        {
            _NotifyCallback(onGunDropEvent);
        }

        public override void OnGunUpdate(GunBase instance)
        {
            _NotifyCallback(onGunUpdateEvent);
        }

        public override void Setup(GunBase instance)
        {
            _NotifyCallback(onSetupEvent);
        }

        public override void Dispose(GunBase instance)
        {
            _NotifyCallback(onDisposeEvent);
        }

        public override void OnHandlePickup(GunBase instance, GunHandle handle)
        {
            _NotifyHandleCallback(handle, onHandlePickupEvent);
        }

        public override void OnHandleUseDown(GunBase instance, GunHandle handle)
        {
            _NotifyHandleCallback(handle, onHandleUseDownEvent);
        }

        public override void OnHandleUseUp(GunBase instance, GunHandle handle)
        {
            _NotifyHandleCallback(handle, onHandleUseUpEvent);
        }

        public override void OnHandleDrop(GunBase instance, GunHandle handle)
        {
            _NotifyHandleCallback(handle, onHandleDropEvent);
        }
    }
}
