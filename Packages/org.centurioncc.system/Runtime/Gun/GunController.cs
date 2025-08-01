using System;
using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunController : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private GunManager gunManager;

        [SerializeField] [NewbieInject]
        private MagazinePouch magazinePouch;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                FireModeChangeAction();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                LoadingActionDown();
            }

            if (Input.GetKeyUp(KeyCode.E))
            {
                LoadingActionUp();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                SimpleReload();
            }

            var scrollDelta = Input.GetAxisRaw("Mouse ScrollWheel") * 80F;
            if (!Mathf.Approximately(scrollDelta, 0) && !Input.GetKey(KeyCode.LeftShift))
            {
                AddPitchOffset(scrollDelta);
            }
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value) LoadingActionUp();
            else LoadingActionDown();
        }

        [PublicAPI]
        public void FireModeChangeAction()
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (gun == null) continue;
                gun.FireMode = GunUtility.CycleFireMode(gun.FireMode, gun.AvailableFireModes);
            }
        }

        [PublicAPI]
        public void LoadingActionDown()
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (gun == null || gun.MagazineReceiver == null) continue;

                if (gun.State != GunState.Idle && gun.MagazineRoundsRemaining != 0 || !gun.HasMagazine)
                    gun.State = GunState.Idle;
                else gun.MagazineReceiver.OnMagazineReleaseButtonDown();
            }
        }

        [PublicAPI]
        public void LoadingActionUp()
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (gun == null || gun.MagazineReceiver == null) continue;

                if (gun.State != GunState.InHoldOpen && gun.MagazineRoundsRemaining != 0 || !gun.HasMagazine) continue;
                gun.MagazineReceiver.OnMagazineReleaseButtonUp();
            }
        }

        [PublicAPI]
        public void SimpleReload()
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (!gun || !gun.MagazineReceiver) continue;

                gun.MagazineReceiver.ReleaseMagazine();
                var magazine = magazinePouch.GetActiveMagazine();
                if (!magazine) continue;
                magazinePouch.DetachActiveMagazine();
                gun.MagazineReceiver.InsertMagazine(magazine);
                if (gun.State != GunState.Idle && gun.MagazineRoundsRemaining != 0 || !gun.HasMagazine)
                    gun.State = GunState.Idle;
                // TODO: somehow dropping after insertion
            }
        }

        [PublicAPI]
        public void AddPitchOffset(float delta)
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (!gun) return;
                gun.CurrentMainHandlePitchOffset += delta;
                gun.RequestSerialization();
            }
        }
    }
}