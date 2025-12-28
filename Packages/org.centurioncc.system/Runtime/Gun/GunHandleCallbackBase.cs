using CenturionCC.System.Utils;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    public abstract class GunHandleCallbackBase : ObjectMarkerBase
    {
        public abstract void OnHandlePickup(GunHandle instance, HandleType handleType);

        public abstract void OnHandleUseDown(GunHandle instance, HandleType handleType);

        public abstract void OnHandleUseUp(GunHandle instance, HandleType handleType);

        public abstract void OnHandleDrop(GunHandle instance, HandleType handleType);
    }
}