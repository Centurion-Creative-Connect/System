using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(Collider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalDamageable : DamageData
    {
        [SerializeField] private string damageType = "LocalDamageable";
        [SerializeField] private bool isDamaging = true;

        [SerializeField] [NewbieInject] [HideInInspector]
        private PlayerManager playerManager;

        private VRCPlayerApi _localPlayer;

        public override bool ShouldApplyDamage
        {
            get
            {
                var local = playerManager.GetLocalPlayer();
                return isDamaging && local != null && !local.IsDead;
            }
        }

        public override int DamagerPlayerId => _localPlayer.playerId;
        public override Vector3 DamageOriginPosition => transform.position;
        public override Quaternion DamageOriginRotation => transform.rotation;
        public override DateTime DamageOriginTime => Networking.GetNetworkDateTime();
        public override string DamageType => damageType;


        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }

        [PublicAPI]
        public void ActivateDamageable()
        {
            SetDamageable(true);
        }

        [PublicAPI]
        public void DeactivateDamageable()
        {
            SetDamageable(false);
        }

        [PublicAPI]
        public void ToggleDamageable()
        {
            SetDamageable(!isDamaging);
        }

        [PublicAPI]
        public void SetDamageable(bool b)
        {
            isDamaging = b;
        }

        [PublicAPI]
        public bool IsDamaging()
        {
            return isDamaging;
        }
    }
}