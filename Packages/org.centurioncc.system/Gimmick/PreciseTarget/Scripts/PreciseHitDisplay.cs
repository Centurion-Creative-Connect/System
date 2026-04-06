using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.PreciseTarget
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PreciseHitDisplay : UdonSharpBehaviour
    {
        [SerializeField]
        private float destroyInSeconds = 10f;

        private void Start()
        {
            SendCustomEventDelayedSeconds(nameof(DestroyThis), destroyInSeconds);
        }

        public void DestroyThis()
        {
            Destroy(gameObject);
        }
    }
}
