using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gimmick.PreciseTarget
{
    public abstract class PreciseTargetCallback : UdonSharpBehaviour
    {
        public abstract void OnHit(DamageData data, Vector3 localPosition, Quaternion localRotation);
    }
}
