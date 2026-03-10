using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gun.Behaviour
{
    public class InteractReloadBehaviour : GunBehaviourBase
    {
        [SerializeField] [Tooltip("Allow reloading when magazine is full?")]
        private bool doForceReload = false;

        private GunBase[] _targetGuns = new GunBase[0];

        public override void Interact()
        {
            foreach (var targetGun in _targetGuns)
            {
                targetGun.ReloadHelper._DoSimplifiedReload(doForceReload);
            }
        }

        public override void Setup(GunBase instance)
        {
            _targetGuns = _targetGuns.AddAsSet(instance);
        }

        public override void Dispose(GunBase instance)
        {
            _targetGuns = _targetGuns.RemoveItem(instance);
        }
    }
}
