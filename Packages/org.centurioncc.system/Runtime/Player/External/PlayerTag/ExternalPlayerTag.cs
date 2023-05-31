using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace CenturionCC.System.Player.External.PlayerTag
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalPlayerTag : ExternalPlayerTagBase
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
        private bool _didSetup;
        private bool _didStart;
        private VRCPlayerApi _localPlayer;

        private ExternalPlayerTagManager _tagManager;
        private Transform _transform;

        private void Start()
        {
            Debug.Log($"[{name}] Start");
            _localPlayer = Networking.LocalPlayer;
            _didStart = true;
            if (_didSetup)
                updateManager.SubscribeUpdate(this);
        }

        private void OnEnable()
        {
            Debug.Log($"[{name}] OnEnable: {updateManager == null}");
            if (_didStart)
                updateManager.SubscribeUpdate(this);
        }

        private void OnDisable()
        {
            Debug.Log($"[{name}] OnDisable: {updateManager == null}");
            updateManager.UnsubscribeUpdate(this);
        }

        private void OnDestroy()
        {
            Debug.Log($"[{name}] OnDestroy");

            updateManager.UnsubscribeUpdate(this);
            _tagManager.RemovePlayerTag(this);
        }

        public void _Update()
        {
            if (!Utilities.IsValid(followingPlayer))
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

        public override void Setup(ExternalPlayerTagManager manager, VRCPlayerApi api)
        {
            gameObject.SetActive(true);
            _transform = transform;
            _tagManager = manager;
            followingPlayer = api;
            _localPlayer = Networking.LocalPlayer;
            _didSetup = true;
        }

        public override void SetTagOn(TagType type, bool isOn)
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

            gameObject.SetActive(IsVisible());
        }

        public override void SetTeamTag(int teamId, Color teamColor)
        {
            teamTag.color = teamColor;
        }

        public bool IsVisible()
        {
            return teamTag.gameObject.activeSelf || masterTag.gameObject.activeSelf ||
                   creatorTag.gameObject.activeSelf || staffTag.gameObject.activeSelf ||
                   devTag.gameObject.activeSelf || ownerTag.gameObject.activeSelf;
        }
    }
}