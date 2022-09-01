using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ShooterSword : DamageData
    {
        public override bool ShouldApplyDamage => _shouldApplyDamage;
        public override int DamagerPlayerId => Networking.LocalPlayer.playerId;
        public override Vector3 DamageOriginPosition => transform.position;
        public override Quaternion DamageOriginRotation => transform.rotation;
        public override string DamageType => "Sword";

        [SerializeField]
        private bool requireTriggerToDamage = true;
        [SerializeField]
        private bool hideMeshRendererOnPickup;
        [SerializeField]
        private MeshRenderer meshRenderer;

        private bool _shouldApplyDamage;

        private void Start()
        {
            DisableDamage();
        }

        public void EnableDamage()
        {
            Debug.Log($"[ShooterSword-{name}] EnableDamage");
            _shouldApplyDamage = true;
        }

        public void DisableDamage()
        {
            Debug.Log($"[ShooterSword-{name}] DisableDamage");
            _shouldApplyDamage = false;
        }

        public override void OnPickup()
        {
            if (!requireTriggerToDamage)
                EnableDamage();
            if (hideMeshRendererOnPickup && meshRenderer != null)
                meshRenderer.enabled = false;
        }

        public override void OnPickupUseDown()
        {
            if (requireTriggerToDamage)
                EnableDamage();
        }

        public override void OnPickupUseUp()
        {
            if (requireTriggerToDamage)
                DisableDamage();
        }

        public override void OnDrop()
        {
            DisableDamage();
            if (hideMeshRendererOnPickup && meshRenderer != null)
                meshRenderer.enabled = true;
        }
    }
}