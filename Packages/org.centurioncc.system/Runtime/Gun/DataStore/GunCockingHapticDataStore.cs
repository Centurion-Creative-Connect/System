using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.DataStore
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunCockingHapticDataStore : UdonSharpBehaviour
    {
        [SerializeField]
        private GunHaptic cockingDoneHaptic;
        [SerializeField]
        private GunHaptic cockingTwistHaptic;
        [SerializeField]
        private GunHaptic cockingPullHaptic;
        [SerializeField]
        private GunContinuousHaptic cockingInBetweenHaptic;

        public GunHaptic Done => cockingDoneHaptic;
        public GunHaptic Twist => cockingTwistHaptic;
        public GunHaptic Pull => cockingPullHaptic;
        public GunContinuousHaptic InBetween => cockingInBetweenHaptic;
    }
}