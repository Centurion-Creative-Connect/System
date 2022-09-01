using CenturionCC.System.Utils;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

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
        private Image masterTag;
        [SerializeField]
        private Text debugText;

        private readonly Vector3 _nametagOffsetPosition = Vector3.up * 0.5F;
        private bool _isDebugTagShown;
        [UdonSynced] [FieldChangeCallback(nameof(IsRoleSpecificTagShown))]
        private bool _isRoleSpecificTagShown;

        // TODO: reduce UdonSynced for staff only property
        [UdonSynced] [FieldChangeCallback(nameof(IsStaffTagShown))]
        private bool _isStaffTagShown = true;
        private bool _isTeamTagShown;
        private ShooterPlayer _player;
        private PlayerManager _playerManager;

        public bool IsStaffTagShown
        {
            get => _isStaffTagShown;
            private set
            {
                if (_isStaffTagShown != value)
                {
                    _isStaffTagShown = value;
                    if (_player != null && _playerManager != null)
                        _playerManager.Invoke_OnPlayerTagChanged(_player, TagType.Staff, value);
                }

                UpdateView();
            }
        }

        public bool IsRoleSpecificTagShown
        {
            get => _isRoleSpecificTagShown;
            private set
            {
                if (_isRoleSpecificTagShown != value)
                {
                    _isRoleSpecificTagShown = value;

                    if (_player != null && _playerManager != null &&
                        (_player.Role.RoleName == "Developer" ||
                         _player.Role.RoleName == "Owner")
                       )
                        _playerManager.Invoke_OnPlayerTagChanged(_player,
                            _player.Role.RoleName == "Owner" ? TagType.Owner : TagType.Dev, value);
                }


                UpdateView();
            }
        }

        public bool IsTeamTagShown
        {
            get => _isTeamTagShown;
            private set
            {
                if (_isTeamTagShown != value)
                {
                    _isTeamTagShown = value;
                    if (_player != null && _playerManager != null)
                        _playerManager.Invoke_OnPlayerTagChanged(_player, TagType.Team, value);
                }

                UpdateView();
            }
        }

        public bool IsDebugTagShown
        {
            get => _isDebugTagShown;
            private set
            {
                if (_isDebugTagShown != value)
                {
                    _isDebugTagShown = value;
                    if (_player != null && _playerManager != null)
                        _playerManager.Invoke_OnPlayerTagChanged(_player, TagType.Debug, value);
                }

                UpdateView();
            }
        }

        private void Start()
        {
            _playerManager = GameManagerHelper.GetPlayerManager();
        }

        public void Init(ShooterPlayer player)
        {
            _player = player;
        }

        public void SetStaffTagShown(bool isShown)
        {
            IsStaffTagShown = isShown;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void SetRoleSpecificTagShown(bool isShown)
        {
            Debug.Log($"SetRoleSpecificTagShown: {isShown}");
            IsRoleSpecificTagShown = isShown;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        public void SetTeamTagShown(bool isShown)
        {
            IsTeamTagShown = isShown;
        }

        public void SetTeamTagColor(Color color)
        {
            teamTag.color = color;
        }

        public void SetDebugTagShown(bool isShown)
        {
            IsDebugTagShown = isShown;
        }

        public void SetDebugTagText(string str)
        {
            debugText.text = str;
        }

        public void UpdateView()
        {
            Debug.Log($"UpdateView::{gameObject.transform.parent.name}");
            if (_player == null)
            {
                HideObjects();
                return;
            }

            var role = _player.Role;
            var api = _player.VrcPlayer;

            if (api == null)
            {
                HideObjects();
                return;
            }

            teamTag.gameObject.SetActive(IsTeamTagShown);

            staffTag.gameObject.SetActive
            (
                IsStaffTagShown &&
                (
                    role.IsGameStaff() ||
                    !IsRoleSpecificTagShown && role != null &&
                    (role.RoleName == "Owner" || role.RoleName == "Developer")
                )
            );

            masterTag.gameObject.SetActive(IsStaffTagShown && api.isMaster);

            ownerTag.gameObject.SetActive(IsStaffTagShown && IsRoleSpecificTagShown && role != null &&
                                          role.RoleName == "Owner");
            devTag.gameObject.SetActive(IsStaffTagShown && IsRoleSpecificTagShown && role != null &&
                                        role.RoleName == "Developer");

            // Debug tag is independent from tag shown state
            debugText.gameObject.SetActive(IsDebugTagShown);
        }

        private void HideObjects()
        {
            teamTag.gameObject.SetActive(false);
            staffTag.gameObject.SetActive(false);
            masterTag.gameObject.SetActive(false);
            ownerTag.gameObject.SetActive(false);
            devTag.gameObject.SetActive(false);
        }

        public void UpdatePositionAndRotation(Vector3 localHead, Vector3 localHeadForward, Vector3 followingHead)
        {
            transform.SetPositionAndRotation(followingHead + _nametagOffsetPosition,
                Quaternion.LookRotation(followingHead + _nametagOffsetPosition + localHeadForward - localHead));
        }
    }

    public enum TagType
    {
        Debug,
        Team,
        Master,
        Staff,
        Dev,
        Owner
    }
}