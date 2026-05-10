using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Utils.PlayerLocomotion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerControllerJumpTagExtension : PlayerControllerCallback
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerController playerController;
        [SerializeField]
        private string jumpActivateTag = "enableJumping";
        [SerializeField]
        private float defaultJumpImpulse = 0;
        [SerializeField]
        private float activatedJumpImpulse = 5;

        private void Start()
        {
            playerController.Subscribe(this);
        }

        public override void OnActiveTagsUpdated()
        {
            var tags = playerController.ActiveTags.ToArray();
            foreach (var t in tags)
            {
                if (t.String == jumpActivateTag)
                {
                    playerController.BaseJumpImpulse = activatedJumpImpulse;
                    return;
                }
            }

            playerController.BaseJumpImpulse = defaultJumpImpulse;
        }
    }
}
