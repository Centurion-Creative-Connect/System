using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.GunCamera
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunCameraDefaultCustomTargets : UdonSharpBehaviour
    {
        [SerializeField]
        private Transform head;

        [SerializeField]
        private Transform chest;

        [SerializeField]
        private Transform hips;

        private Quaternion _chestOffset;

        private Quaternion _headOffset;
        private Quaternion _hipsOffset;
        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
        }

        private void Update()
        {
            head.SetPositionAndRotation(_localPlayer.GetBonePosition(HumanBodyBones.Head),
                _localPlayer.GetBoneRotation(HumanBodyBones.Head) * _headOffset);
            chest.SetPositionAndRotation(_localPlayer.GetBonePosition(HumanBodyBones.Chest),
                _localPlayer.GetBoneRotation(HumanBodyBones.Chest) * _chestOffset);
            hips.SetPositionAndRotation(_localPlayer.GetBonePosition(HumanBodyBones.Hips),
                _localPlayer.GetBoneRotation(HumanBodyBones.Hips) * _hipsOffset);
        }

        public override void OnAvatarChanged(VRCPlayerApi player)
        {
            if (!player.isLocal) return;

            _headOffset = player.GetRotation() * Quaternion.Inverse(player.GetBoneRotation(HumanBodyBones.Head));
            _chestOffset = player.GetRotation() * Quaternion.Inverse(player.GetBoneRotation(HumanBodyBones.Chest));
            _hipsOffset = player.GetRotation() * Quaternion.Inverse(player.GetBoneRotation(HumanBodyBones.Hips));
        }
    }
}