using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class FootstepMarker : UdonSharpBehaviour
    {
        [SerializeField]
        private string footstepType;

        public string FootstepType => footstepType;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void Internal_SetFootstepType(string type) => footstepType = type;
#endif
    }
}