using CenturionCC.System.Player;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.Duel
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DuelGamePlayer : UdonSharpBehaviour
    {
        [SerializeField]
        private DuelGame instance;
        [SerializeField] [NewbieInject] [HideInInspector]
        private PlayerManagerBase playerManager;
        [FormerlySerializedAs("footBounding")]
        [SerializeField]
        private Collider footBounds;

        [UdonSynced]
        public bool isReady;
        [UdonSynced]
        public string weaponName;
        public PlayerBase player;
        [UdonSynced] [FieldChangeCallback(nameof(PlayerId))]
        private int _playerId;


        public VRCPlayerApi vrcPlayerApi;

        public int PlayerId
        {
            get => _playerId;
            private set
            {
                var lastId = _playerId;
                _playerId = value;

                if (lastId == value)
                    return;

                UpdatePlayer();

                if (lastId == 0)
                    instance.OnGamePlayerJoined(this);
                else if (value == 0)
                    instance.OnGamePlayerLeft(this);
            }
        }

        public string DisplayName => vrcPlayerApi != null && vrcPlayerApi.IsValid() ? vrcPlayerApi.displayName : "???";
        public Collider FootBounds => footBounds;

        public override void OnPlayerLeft(VRCPlayerApi api)
        {
            if (api.playerId == PlayerId)
                PlayerId = 0;
        }

        public void AssignLocal()
        {
            PlayerId = Networking.LocalPlayer.playerId;
            UpdatePlayer();
        }

        public void ResetPlayer()
        {
            PlayerId = 0;
            isReady = false;
            vrcPlayerApi = null;
            player = null;
        }

        public void Sync()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void UpdatePlayer()
        {
            vrcPlayerApi = VRCPlayerApi.GetPlayerById(_playerId);
            player = playerManager.GetPlayerById(_playerId);
        }
    }
}
