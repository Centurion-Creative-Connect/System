using UnityEngine;
namespace CenturionCC.System.Gun.Behaviour
{
    public class SmoothingOnActionBehaviour : GunBehaviourBase
    {
        [SerializeField]
        private SmoothingType smoothingType = SmoothingType.Rotation;
        [SerializeField]
        private float smoothingPositionAmount = 2;
        [SerializeField]
        private float smoothingRotationAmount = 2;

        public override void Setup(GunBase instance)
        {
            var posHelper = instance.PositioningHelper;
            posHelper.SetSmoothPositionAmount(smoothingPositionAmount);
            posHelper.SetSmoothRotationAmount(smoothingRotationAmount);
            posHelper.SetSmoothingType(SmoothingType.None);
        }

        public override void Dispose(GunBase instance)
        {
            var posHelper = instance.PositioningHelper;
            posHelper.SetSmoothPositionAmount(0);
            posHelper.SetSmoothRotationAmount(0);
            posHelper.SetSmoothingType(SmoothingType.None);
        }

        public override void OnHandleUseDown(GunBase instance, GunHandle handle)
        {
            if (handle.handleType != HandleType.SubHandle) return;
            instance.PositioningHelper.SetSmoothingType(smoothingType);
        }

        public override void OnHandleUseUp(GunBase instance, GunHandle handle)
        {
            if (handle.handleType != HandleType.SubHandle) return;
            instance.PositioningHelper.SetSmoothingType(SmoothingType.None);
        }
    }
}
