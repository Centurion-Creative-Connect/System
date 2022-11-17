using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    public abstract class GunHandleCallbackBase : UdonSharpBehaviour
    {
        public abstract Vector3 GetHandleIdlePosition(GunHandle instance, HandleType handleType);

        public abstract Quaternion GetHandleIdleRotation(GunHandle instance, HandleType handleType);

        public abstract void OnHandlePickup(GunHandle instance, HandleType handleType);

        public abstract void OnHandleUseDown(GunHandle instance, HandleType handleType);

        public abstract void OnHandleUseUp(GunHandle instance, HandleType handleType);

        public abstract void OnHandleDrop(GunHandle instance, HandleType handleType);
    }
}