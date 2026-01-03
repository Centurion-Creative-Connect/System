using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Defuser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DefuserPickup : ObjectMarkerBase
    {
        [SerializeField] [NewbieInject] [HideInInspector]
        private PlayerController controller;
        [SerializeField]
        private Defuser defuser;
        [SerializeField]
        private DefuserInteraction interaction;
        [SerializeField]
        private DefuserCancelInteraction cancelInteraction;


        [Header("ObjectMarker")]
        [SerializeField]
        private ObjectType objectType = ObjectType.Prototype;
        [SerializeField]
        private float objectWeight = 0F;
        [SerializeField]
        private float walkingSpeedMultiplier = 1F;
        [SerializeField]
        private string[] tags;

        private bool _hasStartedInteractAsDesktop;
        [NonSerialized]
        public VRC_Pickup pickup;

        public override ObjectType ObjectType => objectType;
        public override float ObjectWeight => objectWeight;
        public override float WalkingSpeedMultiplier => walkingSpeedMultiplier;
        public override string[] Tags => tags;

        public bool IsCancelling => cancelInteraction.IsCancelling;

        private void Start()
        {
            pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        }

        public override void OnPickup()
        {
            if (controller != null) controller.AddHoldingObject(this);
            defuser.Close();
            defuser.HideIconForTeam();
        }

        public override void OnPickupUseDown()
        {
            defuser.Open();
        }

        public override void OnPickupUseUp()
        {
            defuser.Close();
        }

        public override void OnDrop()
        {
            if (controller != null) controller.RemoveHoldingObject(this);
            switch (defuser.State)
            {
                default:
                case DefuserState.Idle:
                    break;
                case DefuserState.Opened:
                    defuser.Close();
                    break;
                case DefuserState.PlantingTimer:
                    defuser.AbortPlanting();
                    defuser.Close();
                    break;
                case DefuserState.PlantingUser:
                    defuser.BeginDefusing();
                    break;
                case DefuserState.Defusing:
                case DefuserState.Defused:
                    Debug.LogError($"[DefuserPickup-{name}] Unreachable code reached: OnDrop for {defuser.State}");
                    break;
            }

            defuser.ShowIconForTeam();
        }

        public void OnInteractionPickup()
        {
            controller.CustomEffectMultiplier = 0F;
            controller.UpdateLocalVrcPlayer();
        }

        public void OnInteractionUseDown()
        {
            if (defuser.State == DefuserState.Opened)
                defuser.BeginPlanting();
        }

        public void OnInteractionUseUp()
        {
            if (defuser.State == DefuserState.PlantingTimer)
                defuser.AbortPlanting();
        }

        public void OnInteractionDrop()
        {
            if (defuser.State == DefuserState.PlantingTimer)
                defuser.AbortPlanting();
            if (defuser.State == DefuserState.PlantingUser)
                pickup.Drop();

            controller.CustomEffectMultiplier = 1F;
            controller.UpdateLocalVrcPlayer();
        }

        public void OnStateChanged(DefuserState old, DefuserState next)
        {
            switch (next)
            {
                default:
                case DefuserState.Idle:
                    pickup.pickupable = true;
                    interaction.pickup.pickupable = false;
                    break;
                case DefuserState.Opened:
                    if (pickup.currentPlayer != null && pickup.currentPlayer.isLocal) InputCheckCoroutine();

                    pickup.pickupable = false;
                    interaction.pickup.pickupable = true;
                    break;
                case DefuserState.PlantingTimer:
                    pickup.pickupable = false;
                    interaction.pickup.pickupable = false;
                    break;
                case DefuserState.Defusing:
                case DefuserState.Defused:
                    pickup.pickupable = false;
                    interaction.pickup.pickupable = false;
                    break;
            }
        }

        public void InputCheckCoroutine()
        {
            if (defuser.State != DefuserState.Opened &&
                defuser.State != DefuserState.PlantingTimer &&
                defuser.State != DefuserState.PlantingUser)
            {
                if (_hasStartedInteractAsDesktop)
                {
                    OnInteractionUseUp();
                    OnInteractionDrop();
                }

                return;
            }

            SendCustomEventDelayedFrames(nameof(InputCheckCoroutine), 1);
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnInteractionPickup();
                OnInteractionUseDown();
                _hasStartedInteractAsDesktop = true;
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                OnInteractionUseUp();
                OnInteractionDrop();
            }
        }
    }
}
