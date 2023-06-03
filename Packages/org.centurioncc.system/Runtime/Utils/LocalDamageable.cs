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
        private GameManager gameManager;

        private VRCPlayerApi _localPlayer;

        public override bool ShouldApplyDamage => !gameManager.IsInAntiZombieTime();
        public override int DamagerPlayerId => _localPlayer.playerId;
        public override Vector3 DamageOriginPosition => transform.position;
        public override Quaternion DamageOriginRotation => transform.rotation;
        public override string DamageType => damageType;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }
    }
}