using CenturionCC.System.Utils;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon.Common;
namespace CenturionCC.System.Gimmick.HardCase
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class HardCaseLock : UdonSharpBehaviour
    {
        private UdonSharpBehaviour[] _callbacks;
        private int _callbacksCount;

        [field: UdonSynced]
        public bool IsLocked { get; private set; } = true;

        private void OnDisable()
        {
            IsLocked = true;
            if (Networking.IsOwner(gameObject)) RequestSerialization();
            Invoke_OnLockUpdated();
        }

        public override void Interact()
        {
            IsLocked = !IsLocked;
            Sync();
            Invoke_OnLockUpdated();
        }

        public override void OnDeserialization(DeserializationResult result)
        {
            Invoke_OnLockUpdated();
        }

        private void Invoke_OnLockUpdated()
        {
            for (var i = 0; i < _callbacksCount; i++)
                if (_callbacks[i] != null)
                    _callbacks[i].SendCustomEvent("OnLockUpdated");
        }

        public bool Subscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.AddBehaviour(behaviour, ref _callbacksCount, ref _callbacks);
        }

        public bool Unsubscribe(UdonSharpBehaviour behaviour)
        {
            return CallbackUtil.RemoveBehaviour(behaviour, ref _callbacksCount, ref _callbacks);
        }

        private void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
    }
}
