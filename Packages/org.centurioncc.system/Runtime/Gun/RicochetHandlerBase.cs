using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class RicochetHandlerBase : UdonSharpBehaviour
    {
        public abstract void OnRicochet(ProjectileBase projectileBase, Collision collision);
    }
}
