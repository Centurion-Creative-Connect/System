using CenturionCC.System.Audio;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace CenturionCC.System.Utils
{
    [RequireComponent(typeof(MeshRenderer))] [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Target : UdonSharpBehaviour
    {
        public Material hitMaterial;
        public float seconds = 3;
        public AudioMarker marker;
        private Material _defaultMaterial;

        private MeshRenderer _mesh;

        private void Start()
        {
            _mesh = GetComponent<MeshRenderer>();
            _defaultMaterial = _mesh.materials[0];
        }

        public void OnCollisionEnter(Collision other)
        {
            var damageData = other.gameObject.GetComponent<DamageData>();
            if (damageData == null) return;

            var localPlayerId = Networking.LocalPlayer.playerId;
            if (damageData.DamagerPlayerId == localPlayerId)
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnHit));
        }

        public void OnHit()
        {
            _ChangeMat(hitMaterial);
            SendCustomEventDelayedSeconds(nameof(ResetMaterial), seconds);
            if (marker)
                marker.PlayAt(transform.position, 10);
        }

        public void ResetMaterial()
        {
            _ChangeMat(_defaultMaterial);
        }

        private void _ChangeMat(Material mat)
        {
            var mats = _mesh.materials;
            mats[0] = mat;
            _mesh.materials = mats;
        }
    }
}