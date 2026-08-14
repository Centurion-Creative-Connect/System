using DerpyNewbie.Common;
using UnityEngine;
namespace CenturionCC.System.Gun.Behaviour
{
    public class InteractReloadBehaviour : GunBehaviourBase
    {
        [SerializeField] [Tooltip("Allow reloading when magazine is full?")]
        private bool doForceReload = false;
        [SerializeField] [Tooltip("How many bullets to load by single interaction? Setting this to 0 makes it load fully.")]
        private int reloadAmount;

        private GunBase[] _targetGuns = new GunBase[0];

        public override void Interact()
        {
            foreach (var targetGun in _targetGuns)
            {
                targetGun.ReloadHelper._DoSimplifiedReload(reloadAmount, doForceReload);
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
