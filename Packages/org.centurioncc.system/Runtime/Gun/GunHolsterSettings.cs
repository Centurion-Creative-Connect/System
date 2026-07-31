using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunHolsterSettings : UdonSharpBehaviour
    {
        [SerializeField] [UdonSynced]
        private bool enableHolster;
        [SerializeField] [UdonSynced]
        private bool enableHighlighting;
        [SerializeField] [UdonSynced]
        private bool enableVisual;
        [SerializeField] [UdonSynced]
        private bool enableEditing;

        private readonly DataList _callbacks = new DataList();

        public bool EnableHolster
        {
            get => enableHolster;
            set
            {
                enableHolster = value;
                InvokeCallback();
            }
        }

        public bool EnableHighlighting
        {
            get => enableHighlighting;
            set
            {
                enableHighlighting = value;
                InvokeCallback();
            }
        }

        public bool EnableVisual
        {
            get => enableVisual;
            set
            {
                enableVisual = value;
                InvokeCallback();
            }
        }

        public bool EnableEditing
        {
            get => enableEditing;
            set
            {
                enableEditing = value;
                InvokeCallback();
            }
        }

        public void Subscribe(UdonSharpBehaviour ub)
        {
            _callbacks.Add(ub);
        }

        public bool Unsubscribe(UdonSharpBehaviour ub)
        {
            return _callbacks.RemoveAll(ub);
        }

        private void InvokeCallback()
        {
            var arr = _callbacks.ToArray();
            foreach (var token in arr)
            {
                if (token.TokenType != TokenType.Reference) continue;
                var ub = (UdonSharpBehaviour)token.Reference;
                if (ub == null) continue;

                ub.SendCustomEvent("OnHolsterSettingsChanged");
            }
        }
    }
}
