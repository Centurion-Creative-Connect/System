using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunHapticDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private GunHaptic interactionHaptic;
        [SerializeField]
        private GunHaptic shootingHaptic;

        public GunHaptic Interaction => interactionHaptic;
        public GunHaptic Shooting => shootingHaptic;
    }
}