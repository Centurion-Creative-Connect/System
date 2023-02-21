using System;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Audio
{
    [Obsolete("Use ObjectMarker instead.")] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FootstepMarker : UdonSharpBehaviour
    {
        [SerializeField]
        private FootstepType type = FootstepType.Fallback;

        public FootstepType Type => type;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void Internal_SetFootstepType(FootstepType fType) => type = fType;
#endif
    }

    public enum FootstepType
    {
        Fallback,
        NoAudio,
        Gravel,
        Wood,
        Iron
    }
}