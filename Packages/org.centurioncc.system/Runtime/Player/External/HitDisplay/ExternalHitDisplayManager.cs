using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Player.External.HitDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalHitDisplayManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [SerializeField]
        private GameObject sourceRemoteHitDisplay;
        [SerializeField]
        private Transform parent;

        private void Start()
        {
            playerManager.SubscribeCallback(this);
            sourceRemoteHitDisplay.SetActive(false);
            if (parent == null)
                parent = transform;
        }

        public override void OnKilled(PlayerBase firedPlayer, PlayerBase hitPlayer)
        {
            if (hitPlayer.IsLocal)
                return;

            PlayAt(hitPlayer.VrcPlayer);
        }

        public void PlayAt(VRCPlayerApi api)
        {
            if (api == null || !Utilities.IsValid(api))
            {
                Debug.LogError("[RemoteHitDisplayManager] VRCPlayerApi is null or invalid.");
                return;
            }

            var obj = Instantiate(sourceRemoteHitDisplay, parent);
            var hitDisplay = obj.GetComponent<ExternalHitDisplay>();
            hitDisplay.Play(api);
        }

        public void Clear()
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var obj = parent.GetChild(i);
                if (obj == null || obj.gameObject == sourceRemoteHitDisplay)
                    continue;
                Destroy(obj.gameObject);
            }
        }
    }
}