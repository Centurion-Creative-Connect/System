using UdonSharp;

namespace CenturionCC.System.Gun.Behaviour
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunBehaviourBase : UdonSharpBehaviour
    {
        /// <summary>
        /// Gets called when <see cref="Gun.mainHandle" />'s pickup used down.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Gun.Trigger" /> flag to store whether we shoot next or not.
        /// Get <see cref="Gun.LastShotTime" /> within <see cref="OnGunUpdate" /> for delaying until specified RPM.
        /// </remarks>
        /// <seealso cref="Gun.IsTriggerDown" />
        /// <seealso cref="Gun.Trigger"/>
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnTriggerDown(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when <see cref="Gun.mainHandle" />'s pickup used up.
        /// </summary>
        /// <seealso cref="Gun.IsTriggerDown" />
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnTriggerUp(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when player wanting to change fire mode.
        /// </summary>
        /// <seealso cref="Gun.FireMode" />
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnFireModeChange(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when assigned <see cref="Gun" />'s <see cref="Gun.SubHandle" /> was triggered down.
        /// </summary>
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnAction(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when assigned <see cref="Gun" /> is picked up.
        /// </summary>
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnGunPickup(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when assigned <see cref="Gun" /> is dropped.
        /// </summary>
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnGunDrop(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called every frame while behaviour is assigned to a <see cref="Gun" />.
        /// </summary>
        /// <param name="instance">instance of LocalGun which was changed</param>
        public virtual void OnGunUpdate(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when assigned to a <see cref="Gun" />.
        /// </summary>
        /// <param name="instance">instance of LocalGun which was assigned</param>
        public virtual void Setup(GunBase instance)
        {
        }

        /// <summary>
        /// Gets called when unassigned from a <see cref="Gun" />.
        /// </summary>
        public virtual void Dispose(GunBase instance)
        {
        }

        public virtual void OnHandlePickup(GunBase instance, GunHandle handle)
        {
        }

        public virtual void OnHandleUseDown(GunBase instance, GunHandle handle)
        {
        }

        public virtual void OnHandleUseUp(GunBase instance, GunHandle handle)
        {
        }

        public virtual void OnHandleDrop(GunBase instance, GunHandle handle)
        {
        }
    }
}
