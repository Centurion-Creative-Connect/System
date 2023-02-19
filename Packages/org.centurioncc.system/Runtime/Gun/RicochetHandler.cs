using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class RicochetHandler : UdonSharpBehaviour
    {
        public abstract void OnRicochet(ProjectileBase projectileBase, Collision collision);
    }
}