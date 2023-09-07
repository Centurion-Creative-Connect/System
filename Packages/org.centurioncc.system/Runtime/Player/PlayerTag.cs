using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerTag : UdonSharpBehaviour
    {
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
        [SerializeField]
        private Text debugText;

        private readonly Vector3 _nametagOffsetPosition = Vector3.up * 0.3F;

        private PlayerBase _player;
        private PlayerManager _playerManager;

        public bool HasInitialized { get; private set; }

        public void Init(PlayerBase player, PlayerManager manager)
        {
            _player = player;
            _playerManager = manager;
            HasInitialized = true;
        }

        public void SetDebugTagText(string value)
        {
            debugText.text = value;
        }

        public void UpdateView()
        {
            if (!HasInitialized)
                return;

            if (_player == null || _player.VrcPlayer == null)
            {
                HideObjects();
                return;
            }

            var localPlayer = _playerManager.GetLocalPlayer();
            var localPlayerTeamId = 0;
            if (localPlayer != null)
                localPlayerTeamId = localPlayer.TeamId;

            var isEitherInSpecialTeam = _playerManager.IsSpecialTeamId(_player.TeamId) ||
                                        _playerManager.IsSpecialTeamId(localPlayerTeamId);

            // Show team tag only when they are in the same team or special team
            teamTag.gameObject.SetActive(_playerManager.ShowTeamTag &&
                                         (localPlayerTeamId == _player.TeamId || isEitherInSpecialTeam));
            teamTag.color = _playerManager.GetTeamColor(_player.TeamId);

            masterTag.gameObject.SetActive(_playerManager.ShowStaffTag &&
                                           _player.VrcPlayer.isMaster && isEitherInSpecialTeam);
            debugText.transform.parent.gameObject.SetActive(_playerManager.IsDebug);

            var staffTagType = GetStaffTagType();
            var shouldShowStaffTag = _playerManager.ShowStaffTag &&
                                     _player.Role.IsGameStaff() && isEitherInSpecialTeam;
            var shouldShowCreatorTag = _playerManager.ShowCreatorTag &&
                                       _player.Role.IsGameCreator() && isEitherInSpecialTeam;

            staffTag.gameObject.SetActive(shouldShowStaffTag && staffTagType == TagType.Staff);
            ownerTag.gameObject.SetActive(shouldShowStaffTag && staffTagType == TagType.Owner);
            devTag.gameObject.SetActive(shouldShowStaffTag && staffTagType == TagType.Dev);
            creatorTag.gameObject.SetActive(shouldShowCreatorTag && staffTagType == TagType.Creator);
        }

        public void UpdatePositionAndRotation(Vector3 localHead, Vector3 localHeadForward, Vector3 followingHead)
        {
            transform.SetPositionAndRotation(followingHead + _nametagOffsetPosition,
                Quaternion.LookRotation(followingHead + _nametagOffsetPosition + localHeadForward - localHead));
        }

        private TagType GetStaffTagType()
        {
            var roleName = "Default";
            if (_player.Role != null)
                roleName = _player.Role.RoleName;

            switch (roleName)
            {
                case "Owner":
                    return TagType.Owner;
                case "Developer":
                    return TagType.Dev;
                case "Creator":
                    return TagType.Creator;
                default:
                    return TagType.Staff;
            }
        }

        private void HideObjects()
        {
            teamTag.gameObject.SetActive(false);
            staffTag.gameObject.SetActive(false);
            masterTag.gameObject.SetActive(false);
            ownerTag.gameObject.SetActive(false);
            devTag.gameObject.SetActive(false);
        }
    }

    public enum TagType
    {
        Debug,
        Team,
        Master,
        Staff,
        Dev,
        Owner,
        Creator,
        Hit
    }
}