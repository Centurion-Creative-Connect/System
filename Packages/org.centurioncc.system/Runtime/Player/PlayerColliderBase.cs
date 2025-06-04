using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Player
{
    public abstract class PlayerColliderBase : UdonSharpBehaviour
    {
        public abstract bool IsDebugVisible { get; set; }
        public abstract BodyParts BodyParts { get; }
        public abstract Collider ActualCollider { get; }
    }
}