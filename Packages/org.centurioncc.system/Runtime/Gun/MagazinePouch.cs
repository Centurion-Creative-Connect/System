using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MagazinePouch : GunManagerCallbackBase
    {
        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject]
        private MagazineManager magazineManager;

        [SerializeField] private Vector3 offsetFromHips;
        private Magazine _activeMagazine;

        private bool _hasGun;
        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            gunManager.SubscribeCallback(this);
        }

        private void Update()
        {
            if (!_hasGun || _activeMagazine == null) return;

            // var rot = _localPlayer.GetBoneRotation(HumanBodyBones.Hips);
            //
            // transform.SetPositionAndRotation(
            //     _localPlayer.GetBonePosition(HumanBodyBones.Hips) + rot * offsetFromHips,
            //     rot
            // );

            if (!_activeMagazine.IsHeld) return;

            _activeMagazine = null;
            CreateFollowingMagazine();
        }

        public override void OnPickedUpLocally(ManagedGun instance)
        {
            Debug.Log("[MagazinePouch] Now holding a gun");
            _hasGun = true;
            CreateFollowingMagazine();
        }

        public override void OnDropLocally(ManagedGun instance)
        {
            if (gunManager.LocalHeldGuns.Length != 0) return;

            Debug.Log("[MagazinePouch] No longer holding a gun");
            _hasGun = false;
            DestroyFollowingMagazine();
        }

        private void CreateFollowingMagazine()
        {
            if (_activeMagazine != null)
            {
                Debug.LogWarning("[MagazinePouch] There is active magazine. ignoring create!");
                return;
            }

            var magazine = magazineManager.SpawnMagazine(1, transform.position, transform.rotation);
            magazine.Attach(transform);
            magazine.transform.localPosition = offsetFromHips;
            _activeMagazine = magazine;
        }

        private void DestroyFollowingMagazine()
        {
            if (_activeMagazine == null)
            {
                return;
            }

            Destroy(_activeMagazine.gameObject);
        }
    }
}