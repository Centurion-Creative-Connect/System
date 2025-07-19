using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunCameraTargetsDataStore : UdonSharpBehaviour
    {
        [Header("Custom Targets")]
        [SerializeField] private Transform[] customTargets;

        [SerializeField] private GunCameraDataStore[] customTargetsGunCameraDataStore;

        public int Length => customTargets.Length;

        public void Get(int i, out Transform targetTransform, out GunCameraDataStore customDataStore)
        {
            targetTransform = null;
            customDataStore = null;

            if (customTargets.Length != 0)
                targetTransform = customTargets[i % customTargets.Length];
            if (customTargetsGunCameraDataStore.Length != 0)
                customDataStore = customTargetsGunCameraDataStore[i % customTargetsGunCameraDataStore.Length];
        }
    }
}