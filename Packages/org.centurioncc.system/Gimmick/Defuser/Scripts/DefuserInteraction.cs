using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Defuser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DefuserInteraction : UdonSharpBehaviour
    {
        [SerializeField]
        private DefuserPickup defuserPickup;

        private Vector3 _initPos;
        private Quaternion _initRot;
        [NonSerialized]
        public VRC_Pickup pickup;

        private void Start()
        {
            pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));

            var t = transform;
            _initPos = t.localPosition;
            _initRot = t.localRotation;
        }

        public override void OnPickup()
        {
            defuserPickup.OnInteractionPickup();
        }

        public override void OnPickupUseDown()
        {
            defuserPickup.OnInteractionUseDown();
        }

        public override void OnPickupUseUp()
        {
            defuserPickup.OnInteractionUseUp();
        }

        public override void OnDrop()
        {
            defuserPickup.OnInteractionDrop();
            transform.SetLocalPositionAndRotation(_initPos, _initRot);
        }
    }
}
