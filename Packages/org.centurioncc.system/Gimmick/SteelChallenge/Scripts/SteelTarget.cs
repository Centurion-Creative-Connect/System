using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gimmick.SteelChallenge
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SteelTarget : UdonSharpBehaviour
    {
        public SteelChallengeGame game;
        public bool hasHit;

        private void OnCollisionEnter(Collision collision)
        {
            var other = collision.gameObject;
            if (other == null)
                return;
            var damageData = other.GetComponent<DamageData>();
            if (damageData == null || damageData.DamagerPlayerId != Networking.LocalPlayer.playerId)
                return;

            game.OnTargetHit(this, damageData);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayAudio));
        }

        public void PlayAudio()
        {
            game.PlayAudioAtTarget(this);
        }
    }
}
