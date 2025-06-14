﻿using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ShooterSword : DamageData
    {
        [SerializeField]
        private float damageAmount = 100F;

        [SerializeField]
        private bool requireTriggerToDamage = true;

        [SerializeField]
        private bool hideMeshRendererOnPickup;

        [SerializeField]
        private MeshRenderer meshRenderer;

        private DateTime _originTime = default;

        private bool _shouldApplyDamage;

        public override Guid EventId => Guid.NewGuid();
        public override bool ShouldApplyDamage => _shouldApplyDamage;
        public override int DamagerPlayerId => Networking.LocalPlayer.playerId;
        public override Vector3 DamageOriginPosition => transform.position;
        public override Quaternion DamageOriginRotation => transform.rotation;
        public override DateTime DamageOriginTime => _originTime;
        public override string DamageType => "Sword";
        public override float DamageAmount => damageAmount;

        private void Start()
        {
            DisableDamage();
        }

        public void EnableDamage()
        {
            Debug.Log($"[ShooterSword-{name}] EnableDamage");
            _shouldApplyDamage = true;
            _originTime = Networking.GetNetworkDateTime();
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