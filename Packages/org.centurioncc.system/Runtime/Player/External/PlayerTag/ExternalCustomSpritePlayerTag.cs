using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Player.External.PlayerTag
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ExternalCustomSpritePlayerTag : ExternalPlayerTagBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;

        [Header("UI Images")]
        [SerializeField]
        private Image teamTagImage;
        [SerializeField]
        private Image staffTagImage;
        [SerializeField]
        private Image ownerTagImage;

        [Header("Sprites")]
        [SerializeField]
        [Tooltip(
            "Replaces teamTagImage with corresponding sprites at index of team id.\nSetting it 'None' will disable teamTagImage.")]
        private Sprite[] teamSprites;
        [SerializeField]
        private bool applyTeamColor = false;

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
                    teamTagImage.gameObject.SetActive(isOn);
                    break;
                case TagType.Staff:
                    staffTagImage.gameObject.SetActive(isOn);
                    break;
                case TagType.Owner:
                    ownerTagImage.gameObject.SetActive(isOn);
                    break;
            }

            if (gameObject.activeSelf == IsVisible())
                return;

            gameObject.SetActive(IsVisible());
        }

        public override void SetTeamTag(int teamId, Color teamColor)
        {
            var spriteIndex = teamId;
            if (teamId < 0 || teamId >= teamSprites.Length)
                spriteIndex = 0;

            var sprite = teamSprites[spriteIndex];
            teamTagImage.sprite = sprite;
            teamTagImage.enabled = sprite != null;

            if (applyTeamColor)
                teamTagImage.color = teamColor;
        }

        private bool IsVisible()
        {
            return teamTagImage.gameObject.activeSelf || staffTagImage.gameObject.activeSelf ||
                   ownerTagImage.gameObject.activeSelf;
        }
    }
}