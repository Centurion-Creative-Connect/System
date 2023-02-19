using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    /// <summary>
    /// Object marker marks objects with property which can be used in other classes
    /// </summary>
    /// <seealso cref="PlayerController"/>
    /// <seealso cref="System.Audio.FootstepGenerator"/>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ObjectMarker : ObjectMarkerBase
    {
        [Tooltip("Object type which can be used to determine which effect is suitable for this object.\n" +
                 "Mostly used in FootstepGenerator and Gun.")]
        [SerializeField]
        private ObjectType objectType = ObjectType.Prototype;

        [Tooltip("Describes how heavy this object is, in kilogram.")]
        [SerializeField]
        private float objectWeight = 1F;

        [Tooltip("Multiplier which affects player's movement speed when standing directly above this object.\n" +
                 "Mostly used in PlayerController.")]
        [SerializeField]
        private float walkingSpeedMultiplier = 1F;

        [Tooltip("Tags can be used to describe this object's behaviour.\n" +
                 "E.x. add \"NoFootstep\" tag to notify object's not for footstep.")]
        [SerializeField]
        private string[] tags;

        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerController controller;

        public override ObjectType ObjectType => objectType;
        public override float ObjectWeight => objectWeight;
        public override float WalkingSpeedMultiplier => walkingSpeedMultiplier;
        public override string[] Tags => tags;

        public override void OnPickup()
        {
            if (controller != null)
                controller.AddHoldingObject(this);
        }

        public override void OnDrop()
        {
            if (controller != null)
                controller.RemoveHoldingObject(this);
        }

#if UNITY_EDITOR && !COMPILER_UDONSHARP
        public void EditorOnly_SetObjectType(ObjectType type)
        {
            objectType = type;
        }

        public void EditorOnly_SetReference(PlayerController c)
        {
            controller = c;
        }
#endif
    }
}