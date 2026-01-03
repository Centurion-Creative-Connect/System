using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(VRC_Pickup))] [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ObjectiveIcon : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject(SearchScope.Self)]
        private VRC_Pickup pickup;

        [SerializeField]
        private ParticleSystem iconParticle;

        [SerializeField] [Range(0, 1)]
        private float pickupTransparency;

        [SerializeField] [Range(0, 1)]
        private float dropTransparency;

        [SerializeField] [NewbieInject]
        private PlayerManagerBase playerManager;

        private float _transparency = 1F;

        public override void OnPickup()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnPickupGlobal));
        }

        public override void OnDrop()
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnDropGlobal));
        }

        public void OnPickupGlobal()
        {
            ChangeIconTransparency(pickupTransparency);
            UpdateIcon();
        }

        public void OnDropGlobal()
        {
            ChangeIconTransparency(dropTransparency);
            UpdateIcon();
        }

        private void UpdateIcon()
        {
            // LocalPlayer must be same team or in special team to be able to see icon.
            // Except when current defuser team is special, no one should be able to see the icon.
            var shouldShow = !playerManager.IsSpecialTeamId(GetCurrentTeamId());
            if (iconParticle != null) iconParticle.gameObject.SetActive(shouldShow);
        }

        private void ChangeIconTransparency(float transparency)
        {
            if (iconParticle == null) return;
            var main = iconParticle.main;
            var color = main.startColor.color;
            color.a = transparency;
            main.startColor = color;
        }

        private int GetCurrentTeamId()
        {
            if (Utilities.IsValid(pickup.currentPlayer))
            {
                var carrier = playerManager.GetPlayerById(pickup.currentPlayer.playerId);
                if (carrier != null) return carrier.TeamId;
            }

            var owner = playerManager.GetPlayerById(Networking.GetOwner(gameObject).playerId);
            return owner != null ? owner.TeamId : 0;
        }
    }
}
