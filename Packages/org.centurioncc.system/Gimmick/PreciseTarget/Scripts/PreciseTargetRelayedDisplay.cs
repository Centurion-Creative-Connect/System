using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.PreciseTarget
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PreciseTargetRelayedDisplay : PreciseTargetCallback
    {
        [SerializeField]
        private GameObject impactPointDisplay;

        public override void OnHit(DamageData data, Vector3 localPosition, Quaternion localRotation)
        {
            Instantiate(impactPointDisplay, transform.TransformPoint(localPosition), transform.rotation * localRotation, transform);
        }
    }
}
