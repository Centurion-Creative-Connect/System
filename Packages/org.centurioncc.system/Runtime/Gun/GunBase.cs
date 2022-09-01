using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    public abstract class GunBase : GunHandleCallbackBase
    {
        [PublicAPI]
        public abstract string WeaponName { get; }
        
        [PublicAPI]
        public abstract Transform Target { get; }
        [PublicAPI]
        public virtual Animator TargetAnimator { get; }
        [PublicAPI]
        public abstract GunHandle MainHandle { get; }
        [PublicAPI]
        public abstract GunHandle SubHandle { get; }
        [PublicAPI]
        public abstract GunHandle CustomHandle { get; }

        [PublicAPI]
        public abstract bool IsPickedUp { get; }
        /// <summary>
        /// Current holder of this Gun.
        /// </summary>
        [PublicAPI]
        public abstract VRCPlayerApi CurrentHolder { get; }

        /// <summary>
        /// Is the <see cref="CurrentHolder"/> local?
        /// </summary>
        [PublicAPI]
        public abstract bool IsLocal { get; }

        [PublicAPI]
        public virtual TriggerState Trigger { get; set; }
        [PublicAPI]
        public virtual GunState State { get; set; }


        [PublicAPI]
        public abstract Vector3 MainHandlePositionOffset { get; }
        [PublicAPI]
        public abstract Quaternion MainHandleRotationOffset { get; }

        [PublicAPI]
        public abstract Vector3 SubHandlePositionOffset { get; }
        [PublicAPI]
        public abstract Quaternion SubHandleRotationOffset { get; }


        /// <summary>
        /// Shoots bullet without any checks.
        /// </summary>
        [PublicAPI]
        public abstract void Shoot();

        /// <summary>
        /// If the Gun was able to shoot, will shoot the bullet.
        /// </summary>
        /// <returns><see cref="ShotResult.Succeeded"/> if succeed to shoot, <see cref="ShotResult.Paused"/> if paused, <see cref="ShotResult.Failed"/> if checks were failed.</returns>
        [PublicAPI]
        public abstract ShotResult TryToShoot();

        /// <summary>
        /// Plays *click* sound for failed shot.
        /// </summary>
        [PublicAPI]
        public abstract void EmptyShoot();

        /// <summary>
        /// Updates position and rotation according to MainHandle and SubHandle's position.
        /// </summary>
        [PublicAPI]
        public abstract void UpdatePosition();

        /// <summary>
        /// Moves it's Gun's position and rotation to specified location
        /// </summary>
        /// <param name="position">New world position for this Gun.</param>
        /// <param name="rotation">New world rotation for this Gun.</param>
        [PublicAPI]
        public abstract void MoveTo(Vector3 position, Quaternion rotation);
    }
}