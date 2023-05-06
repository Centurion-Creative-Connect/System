using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CenturionCC.System.Player.PlayerExternal
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalPlayerTag : UdonSharpBehaviour
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        [SerializeField]
        private Image teamTag;
        [SerializeField]
        private Image ownerTag;
        [SerializeField]
        private Image devTag;
        [SerializeField]
        private Image staffTag;
        [SerializeField]
        private Image creatorTag;
        [SerializeField]
        private Image masterTag;
        private readonly Vector3 _nametagOffsetPosition = Vector3.up * 0.3F;
        private VRCPlayerApi _localPlayer;

        private ExternalPlayerTagManager _tagManager;
        private Transform _transform;

        public VRCPlayerApi followingPlayer;

        private void Start()
        {
            Debug.Log($"[{name}] Start");
            _localPlayer = Networking.LocalPlayer;
        }

        private void OnDestroy()
        {
            Debug.Log($"[{name}] OnDestroy");

            updateManager.UnsubscribeUpdate(this);
            _tagManager.RemovePlayerTag(this);
        }

        public void _Update()
        {
            if (!followingPlayer.IsValid())
            {
                DestroyThis();
                return;
            }

            _transform.position = followingPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position +
                                  _nametagOffsetPosition;
            _transform.LookAt(_localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (followingPlayer == player)
                DestroyThis();
        }

        public void Setup(ExternalPlayerTagManager manager, VRCPlayerApi api)
        {
            gameObject.SetActive(true);
            _transform = transform;
            _tagManager = manager;
            followingPlayer = api;
            _localPlayer = Networking.LocalPlayer;
            updateManager.SubscribeUpdate(this);
        }

        public void SetTagOn(TagType type, bool isOn)
        {
            switch (type)
            {
                case TagType.Team:
                    teamTag.gameObject.SetActive(isOn);
                    break;
                case TagType.Master:
                    masterTag.gameObject.SetActive(isOn);
                    break;
                default:
                case TagType.Debug:
                    // TODO: impl debug tag
                    break;
                case TagType.Creator:
                    creatorTag.gameObject.SetActive(isOn);
                    break;
                case TagType.Staff:
                    staffTag.gameObject.SetActive(isOn);
                    break;
                case TagType.Dev:
                    devTag.gameObject.SetActive(isOn);
                    break;
                case TagType.Owner:
                    ownerTag.gameObject.SetActive(isOn);
                    break;
            }

            if (gameObject.activeSelf == IsVisible())
                return;

            var isVisible = IsVisible();
            gameObject.SetActive(isVisible);
            if (isVisible)
            {
                updateManager.UnsubscribeUpdate(this);
                updateManager.SubscribeUpdate(this);
            }
            else
            {
                updateManager.UnsubscribeUpdate(this);
            }
        }

        public void SetTeamTagColor(Color color)
        {
            teamTag.color = color;
        }

        public bool IsVisible()
        {
            return teamTag.gameObject.activeSelf || masterTag.gameObject.activeSelf ||
                   creatorTag.gameObject.activeSelf || staffTag.gameObject.activeSelf ||
                   devTag.gameObject.activeSelf || ownerTag.gameObject.activeSelf;
        }

        public void DestroyThis()
        {
            Debug.Log($"[{name}] Self-Destroying");
            Destroy(gameObject);
        }
    }
}