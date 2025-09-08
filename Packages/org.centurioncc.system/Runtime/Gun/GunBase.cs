using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    public abstract class GunBase : GunHandleCallbackBase
    {
        [PublicAPI] public abstract string WeaponName { get; }

        [PublicAPI] public abstract Transform Target { get; }
        [PublicAPI] public virtual Animator TargetAnimator { get; }
        [PublicAPI] public abstract GunHandle MainHandle { get; }
        [PublicAPI] public abstract GunHandle SubHandle { get; }
        [PublicAPI] public abstract GunHandle CustomHandle { get; }

        [PublicAPI] public abstract bool IsPickedUp { get; }

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

        /// <summary>
        /// Is the <see cref="CurrentHolder"/> in VR?
        /// </summary>
        [PublicAPI]
        public abstract bool IsVR { get; }

        [PublicAPI] public virtual TriggerState Trigger { get; set; }
        [PublicAPI] public virtual GunState State { get; set; }

        /// <summary>
        /// Has the gun bullet in a chamber?
        /// </summary>
        /// <remarks>
        /// This property should be set to <c>false</c> after bullet was shot by <see cref="Shoot"/>, <see cref="TryToShoot"/>.
        /// Load the bullet from a chamber using <see cref="LoadBullet"/>.
        /// Eject the bullet from a chamber using <see cref="EjectBullet"/>.
        /// </remarks>
        [PublicAPI]
        public virtual bool HasBulletInChamber { get; protected set; }

        /// <summary>
        /// Has the gun cocked?
        /// </summary>
        /// <remarks>
        /// This property is set to `false` after <see cref="Shoot"/>, <see cref="TryToShoot"/>, and <see cref="EmptyShoot"/>.
        /// Manipulate within <see cref="CenturionCC.System.Gun.Behaviour.GunBehaviourBase"/> 
        /// </remarks>
        [PublicAPI]
        public virtual bool HasCocked { get; set; }

        [PublicAPI] public abstract Vector3 MainHandlePositionOffset { get; }
        [PublicAPI] public abstract Quaternion MainHandleRotationOffset { get; }

        [PublicAPI] public abstract Vector3 SubHandlePositionOffset { get; }
        [PublicAPI] public abstract Quaternion SubHandleRotationOffset { get; }

        [PublicAPI] public abstract MovementOption MovementOption { get; }
        [PublicAPI] public abstract float WalkSpeed { get; }
        [PublicAPI] public abstract float SprintSpeed { get; }
        [PublicAPI] public abstract float SprintThresholdMultiplier { get; }
        [PublicAPI] public abstract CombatTagOption CombatTagOption { get; }
        [PublicAPI] public abstract float CombatTagSpeedMultiplier { get; }
        [PublicAPI] public abstract float CombatTagTime { get; }

        /// <summary>
        /// Shoots a bullet without any checks.
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
        /// Plays *click* sound for a failed shot.
        /// </summary>
        [PublicAPI]
        public abstract void EmptyShoot();

        /// <summary>
        /// Updates position and rotation according to MainHandle and SubHandle's position.
        /// </summary>
        [PublicAPI]
        public abstract void UpdatePosition();

        /// <summary>
        /// Moves it's Gun's position and rotation to a specified location.
        /// </summary>
        /// <param name="position">New world position for this Gun.</param>
        /// <param name="rotation">New world rotation for this Gun.</param>
        [PublicAPI]
        public abstract void MoveTo(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Does the gun have a next bullet to load into the chamber? 
        /// </summary>
        /// <returns>`true` if the magazine or gun itself can provide the next bullet to shoot, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool HasNextBullet()
        {
            return true;
        }

        /// <summary>
        /// Tries to load bullet from current magazine.
        /// </summary>
        /// <returns>`true` if successfully loaded bullet on a gun, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool LoadBullet()
        {
            if (HasBulletInChamber)
                EjectBullet();
            HasBulletInChamber = true;
            RequestSerialization();
            return HasBulletInChamber;
        }

        /// <summary>
        /// Tries to eject a bullet from the chamber.
        /// </summary>
        [PublicAPI]
        public virtual void EjectBullet()
        {
            HasBulletInChamber = false;
            RequestSerialization();
        }
    }
}