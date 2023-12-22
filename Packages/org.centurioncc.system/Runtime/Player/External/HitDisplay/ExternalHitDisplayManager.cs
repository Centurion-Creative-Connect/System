using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

namespace CenturionCC.System.Player.External.HitDisplay
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalHitDisplayManager : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManager playerManager;
        [FormerlySerializedAs("sourceRemoteHitDisplay")]
        [SerializeField]
        private GameObject sourceExternalHitDisplay;
        [SerializeField]
        private Transform parent;

        private void Start()
        {
            playerManager.SubscribeCallback(this);
            sourceExternalHitDisplay.SetActive(false);
            if (parent == null)
                parent = transform;
        }

        public override void OnKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (victim.IsLocal)
                return;

            Play(victim);
        }

        public void Play(PlayerBase player)
        {
            if (player == null)
            {
                Debug.LogError("[ExternalHitDisplayManager] Provided player is null");
                return;
            }

            var obj = Instantiate(sourceExternalHitDisplay, parent);
            var hitDisplay = obj.GetComponent<ExternalHitDisplayBase>();
            hitDisplay.Play(player);
        }

        public void Clear()
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var obj = parent.GetChild(i);
                if (obj == null || obj.gameObject == sourceExternalHitDisplay)
                    continue;
                Destroy(obj.gameObject);
            }
        }
    }

    public enum HitDisplaySetting
    {
        Always,
        LocalOnly,
        RemoteOnly
    }
}