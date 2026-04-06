using CenturionCC.System.Utils;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.PreciseTarget
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PreciseTarget : UdonSharpBehaviour
    {
        [SerializeField]
        private PreciseTargetCallback[] callbacks;

        [SerializeField]
        private PreciseTargetSettings settings;
        [SerializeField]
        private GameObject impactPointDisplay;

        private void OnCollisionEnter(Collision collision)
        {
            // if collision didn't exist, do not display
            if (collision == null || collision.contactCount == 0)
            {
                return;
            }

            // if settings exist and is disabled, do not display
            if (settings != null && !settings.isEnabled)
            {
                return;
            }

            var contact = collision.GetContact(0);

            // if DamageData didn't exist, do not display
            var damageData = contact.otherCollider.GetComponent<DamageData>();
            if (damageData == null)
            {
                return;
            }

            // if setting exists and their respective settings didn't match, do not display
            if (settings != null)
            {
                var localPlayerId = Networking.LocalPlayer.playerId;
                var attackerPlayerId = damageData.DamagerPlayerId;
                if (!settings.showLocalPlayerHits && localPlayerId == attackerPlayerId) return;
                if (!settings.showOtherPlayerHits && localPlayerId != attackerPlayerId) return;
            }

            var pos = contact.point;
            var rot = Quaternion.LookRotation(contact.normal);
            Instantiate(impactPointDisplay, pos, rot, transform);

            var localPos = transform.InverseTransformPoint(pos);
            var localRot = Quaternion.Inverse(transform.rotation) * rot;
            foreach (var callback in callbacks)
            {
                callback.OnHit(damageData, localPos, localRot);
            }
        }
    }
}
