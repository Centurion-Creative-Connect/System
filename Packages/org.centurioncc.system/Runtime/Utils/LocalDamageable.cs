using System;
using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(Collider))] [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalDamageable : DamageData
    {
        [SerializeField]
        private string damageType = "LocalDamageable";
        [SerializeField] [NewbieInject] [HideInInspector]
        private PlayerManager playerManager;

        private VRCPlayerApi _localPlayer;

        public override bool ShouldApplyDamage
        {
            get
            {
                var local = playerManager.GetLocalPlayer();
                return local != null && !local.IsDead;
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
    }
}