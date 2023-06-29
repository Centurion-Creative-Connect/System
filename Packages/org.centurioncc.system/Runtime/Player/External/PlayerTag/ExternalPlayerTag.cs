using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

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

        protected override void Start()
        {
            base.Start();
            if (didSetup)
                updateManager.SubscribeUpdate(this);
        }

        private void OnEnable()
        {
            if (didStart)
                updateManager.SubscribeUpdate(this);
        }

        private void OnDisable()
        {
            updateManager.UnsubscribeUpdate(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            updateManager.UnsubscribeUpdate(this);
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

        private bool IsVisible()
        {
            return teamTag.gameObject.activeSelf || masterTag.gameObject.activeSelf ||
                   creatorTag.gameObject.activeSelf || staffTag.gameObject.activeSelf ||
                   devTag.gameObject.activeSelf || ownerTag.gameObject.activeSelf;
        }
    }
}