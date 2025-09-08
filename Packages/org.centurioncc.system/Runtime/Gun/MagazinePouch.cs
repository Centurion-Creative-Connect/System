using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Random = UnityEngine.Random;

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
        private GunBase _activeGun;
        private Magazine _activeMagazine;

        private bool _hasGun;
        private VRCPlayerApi _localPlayer;
        private int _requiredMagazineType;

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

            DetachActiveMagazine();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            var magazine = other.GetComponent<Magazine>();
            if (magazine == null) return;
            if (magazine == _activeMagazine) return;
            if (magazine.IsAttached) return;
            if (magazine.IsHeld) return;

            if (!magazineManager.StoreMagazine(magazine)) return;
            Destroy(magazine.gameObject);

            if (_activeMagazine == null) CreateFollowingMagazine();
        }

        public override void OnPickedUpLocally(ManagedGun instance)
        {
            Debug.Log("[MagazinePouch] Now holding a gun");
            _hasGun = true;
            _activeGun = instance;
            CreateFollowingMagazine();
        }

        public override void OnDropLocally(ManagedGun instance)
        {
            if (gunManager.LocalHeldGuns.Length != 0)
            {
                _activeGun = gunManager.LocalHeldGuns[0];
                DestroyFollowingMagazine();
                CreateFollowingMagazine();
                return;
            }

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

            if (_activeGun == null)
            {
                Debug.LogWarning("[MagazinePouch] There is no active gun. ignoring create!");
                return;
            }

            if (_activeGun.AllowedMagazineTypes.Length == 0)
            {
                Debug.LogWarning("[MagazinePouch] There is no magazine allowed for active gun. ignoring create!");
                return;
            }

            var targetMagazineType =
                _activeGun.AllowedMagazineTypes[Random.Range(0, _activeGun.AllowedMagazineTypes.Length)];
            var magazine = magazineManager.RestoreMagazine(targetMagazineType);
            if (magazine == null)
            {
                Debug.LogWarning("[MagazinePouch] There is no magazine stored. ignoring create!");
                return;
            }

            magazine.Attach(transform);
            magazine.transform.localPosition = offsetFromHips;
            magazine.transform.rotation = transform.rotation;
            _activeMagazine = magazine;
        }

        private void DestroyFollowingMagazine()
        {
            if (_activeMagazine == null)
            {
                return;
            }

            magazineManager.StoreMagazine(_activeMagazine);
            Destroy(_activeMagazine.gameObject);
        }

        public void DetachActiveMagazine()
        {
            _activeMagazine.Detach();
            _activeMagazine = null;
            CreateFollowingMagazine();
        }

        public Magazine GetActiveMagazine()
        {
            return _activeMagazine;
        }
    }
}