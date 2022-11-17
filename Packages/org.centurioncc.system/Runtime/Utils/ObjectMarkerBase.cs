using UdonSharp;

namespace CenturionCC.System.Utils
{
    /// <summary>
    /// Makes Udon to get detailed object's description for use in many other stuff.  
    /// </summary>
    /// <seealso cref="ObjectMarker"/>
    /// <seealso cref="PlayerController"/>
    public abstract class ObjectMarkerBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Describes which material type is suitable for this object.
        /// </summary>
        public abstract ObjectType ObjectType { get; }
        /// <summary>
        /// Describes how heavy this object is, in kilogram.
        /// </summary>
        public abstract float ObjectWeight { get; }
        /// <summary>
        /// Describes how fast the player can be moved when player is on top of this object.
        /// Default is 1.
        /// </summary>
        public abstract float WalkingSpeedMultiplier { get; }

        /// <summary>
        /// Unity's GameObject tags alternative for Udon. (GameObject.tag is not exposed to udon)
        /// Can be used to check if this object matches with some other script's flag.
        /// </summary>
        public abstract string[] Tags { get; }
    }

    public enum ObjectType
    {
        Prototype,
        Gravel,
        Wood,
        Iron,
        Dirt,
        Concrete,
    }
}