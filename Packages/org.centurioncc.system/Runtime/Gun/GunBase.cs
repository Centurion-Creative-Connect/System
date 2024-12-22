using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    public abstract class GunBase : GunHandleCallbackBase
    {
        [PublicAPI] public abstract string WeaponName { get; }

        [PublicAPI] public abstract Transform Target { get; }
        [PublicAPI] [CanBeNull] public virtual Animator TargetAnimator { get; }
        [PublicAPI] [CanBeNull] public virtual MagazineReceiver MagazineReceiver { get; }
        [PublicAPI] public abstract GunHandle MainHandle { get; }
        [PublicAPI] public abstract GunHandle SubHandle { get; }
        [PublicAPI] public abstract GunHandle CustomHandle { get; }

        [PublicAPI] public abstract bool IsPickedUp { get; }

        /// <summary>
        /// Current holder of this Gun.
        /// </summary>
        [PublicAPI] [CanBeNull]
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
        /// Bullets remaining in the magazine (not in reserve)
        /// </summary>
        [PublicAPI]
        public virtual int MagazineRoundsRemaining =>
            MagazineReceiver != null ? MagazineReceiver.MagazineRoundsRemaining : 1;

        /// <summary>
        /// Size of the magazine.
        /// </summary>
        [PublicAPI]
        public virtual int MagazineRoundsCapacity =>
            MagazineReceiver != null ? MagazineReceiver.MagazineRoundsCapacity : 1;

        /// <summary>
        /// Which magazine does the gun have?
        /// </summary>
        [PublicAPI]
        public abstract int CurrentMagazineType { get; protected set; }

        /// <summary>
        /// Has the gun bullet in chamber?
        /// </summary>
        /// <remarks>
        /// This property should be set to <c>false</c> after bullet was shot by <see cref="Shoot"/>, <see cref="TryToShoot"/>.
        /// Load the bullet from chamber using <see cref="LoadBullet"/>.
        /// Eject the bullet from chamber using <see cref="EjectBullet"/>.
        /// </remarks>
        [PublicAPI]
        public virtual bool HasBulletInChamber { get; protected set; }

        /// <summary>
        /// Has the gun cocked?
        /// </summary>
        /// <remarks>
        /// This property is set to `false` after <see cref="Shoot"/>, <see cref="TryToShoot"/>, and <see cref="EmptyShoot"/>.
        /// Manipulate within <see cref="Gun.Behaviour.GunBehaviourBase"/> 
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
        /// Moves it's Gun's position and rotation to specified location.
        /// </summary>
        /// <param name="position">New world position for this Gun.</param>
        /// <param name="rotation">New world rotation for this Gun.</param>
        [PublicAPI]
        public abstract void MoveTo(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Has next bullet to load into chamber? 
        /// </summary>
        /// <returns>`true` if magazine or gun itself can provide next bullet to shoot, `false` otherwise.</returns>
        [PublicAPI]
        public virtual bool HasNextBullet()
        {
            return MagazineReceiver == null || MagazineReceiver.HasNextBullet();
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

            if (!HasNextBullet())
                return HasBulletInChamber;

            HasBulletInChamber = MagazineReceiver == null || MagazineReceiver.ConsumeBullet();
            RequestSerialization();

            return HasBulletInChamber;
        }

        /// <summary>
        /// Tries to eject bullet from chamber.
        /// </summary>
        [PublicAPI]
        public virtual void EjectBullet()
        {
            HasBulletInChamber = false;
            RequestSerialization();
        }

        /// <summary>
        /// Invokes when `MagazineReceiver` had its state changed such as inserting, or releasing a magazine.
        /// </summary>
        [PublicAPI]
        public virtual void OnMagazineChanged()
        {
            if (MagazineReceiver == null) return;
            CurrentMagazineType = MagazineReceiver.MagazineType;
        }

        public virtual void OnMagazineCollision()
        {
        }
    }
}