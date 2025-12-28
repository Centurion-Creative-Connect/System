using CenturionCC.System.Player;
using CenturionCC.System.UI;
using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
namespace CenturionCC.System.Gimmick.StickFlag
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StickFlagStick : PlayerManagerCallbackBase
    {
        [SerializeField] [HideInInspector] [NewbieInject]
        private UpdateManager updateManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private PlayerManagerBase playerManager;
        [SerializeField] [HideInInspector] [NewbieInject]
        private NotificationProvider notification;
        [SerializeField]
        public Transform reference;
        [SerializeField]
        private StickFlag flag;
        [SerializeField]
        private VRCPickup pickup;
        [SerializeField]
        private TranslatableMessage hitDropMessage;
        [UdonSynced]
        private bool _isLeftHand;

        private bool _isUpdating;

        [UdonSynced]
        private int _pickupPlayerId;
        [UdonSynced]
        private Vector3 _pickupPosOffset;
        [UdonSynced]
        private Quaternion _pickupRotOffset;

        public bool IsLocallyHeld { get; private set; }

        public void _Update()
        {
            UpdatePosition();
        }

        public override void OnPickup()
        {
            Debug.Log($"[{name}] OnPickup");

            if (_pickupPlayerId != -1)
            {
                Debug.LogWarning($"[{name}]");
                pickup.Drop(Networking.LocalPlayer);
                return;
            }

            IsLocallyHeld = true;

            var bone = pickup.currentHand == VRC_Pickup.PickupHand.Left
                ? HumanBodyBones.LeftHand
                : HumanBodyBones.RightHand;
            var pickupT = pickup.transform;

            _pickupPlayerId = Networking.LocalPlayer.playerId;
            _isLeftHand = pickup.currentHand == VRC_Pickup.PickupHand.Left;
            _pickupPosOffset = pickupT.position - Networking.LocalPlayer.GetBonePosition(bone);
            _pickupRotOffset = pickupT.rotation * Quaternion.Inverse(Networking.LocalPlayer.GetBoneRotation(bone));

            Sync();

            playerManager.Subscribe(this);
            flag.OnPickup();

            UpdateState();
        }

        public override void OnPickupUseDown()
        {
            // _isFlipped = !_isFlipped;
            Sync();
        }

        public override void OnDrop()
        {
            Debug.Log($"[{name}] OnDrop");

            IsLocallyHeld = false;

            var pickupT = pickup.transform;
            _pickupPlayerId = -1;
            _isLeftHand = false;
            _pickupPosOffset = pickupT.position;
            _pickupRotOffset = pickupT.rotation;

            Sync();

            playerManager.Unsubscribe(this);
            flag.OnDrop();

            UpdateState();
        }

        public override void OnPlayerKilled(PlayerBase attacker, PlayerBase victim, KillType type)
        {
            if (!victim.IsLocal) return;

            pickup.Drop();

            if (notification != null && hitDropMessage != null)
            {
                notification.ShowWarn(hitDropMessage.Message);
            }
        }

        public void UpdatePosition()
        {
            if (_pickupPlayerId == -1)
            {
                pickup.transform.SetPositionAndRotation(_pickupPosOffset, _pickupRotOffset);
                return;
            }

            var pickupT = pickup.transform;
            var bone = _isLeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            if (_pickupPlayerId == Networking.LocalPlayer.playerId)
            {
                var local = Networking.LocalPlayer;
                GetOffsetPositionAndRotation(local, bone, pickupT, out var currPosOffset, out var currRotOffset);
                var dist = Vector3.Distance(currPosOffset, _pickupPosOffset);

                if (dist < 0.05F) return;

                Debug.Log($"[{name}] Updating pickup offset data because distance {dist} >= 0.05");

                _pickupRotOffset = currRotOffset;
                _pickupPosOffset = currPosOffset;

                Sync();
                return;
            }

            var player = VRCPlayerApi.GetPlayerById(_pickupPlayerId);
            if (player == null || !player.IsValid())
            {
                _pickupPlayerId = -1;
                _pickupPosOffset = pickupT.position;
                _pickupRotOffset = pickupT.rotation;
                _isLeftHand = false;

                Sync();

                return;
            }

            var bonePos = player.GetBonePosition(bone);
            var boneRot = player.GetBoneRotation(bone);
            var pos = bonePos + (boneRot * _pickupPosOffset);
            var rot = boneRot * _pickupRotOffset;

            pickupT.SetPositionAndRotation(pos, rot);
        }

        public void Sync()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        // Will not work if picked up
        public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
        {
            if (_pickupPlayerId != -1) return;

            _pickupPosOffset = pos;
            _pickupRotOffset = rot;

            UpdatePosition();
        }

        public void UpdateState()
        {
            Debug.Log($"[{name}] UpdateState");

            if (_pickupPlayerId == -1 && _isUpdating)
            {
                _isUpdating = true;
                updateManager.UnsubscribeUpdate(this);
                return;
            }

            if (!_isUpdating)
            {
                _isUpdating = false;
                updateManager.SubscribeUpdate(this);
                return;
            }
        }

        public override void OnDeserialization()
        {
            UpdateState();
            UpdatePosition();
        }

        public override void OnPreSerialization()
        {
            UpdateState();
            UpdatePosition();
        }

        private static void GetOffsetPositionAndRotation(
            VRCPlayerApi api, HumanBodyBones bone, Transform t,
            out Vector3 pos, out Quaternion rot
        )
        {
            var bonePos = api.GetBonePosition(bone);
            var boneRot = api.GetBoneRotation(bone);

            pos = Quaternion.Inverse(boneRot) * (t.position - bonePos);
            rot = Quaternion.Inverse(boneRot) * t.rotation;
        }
    }
}
