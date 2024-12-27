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

        private void Update()
        {
            // TODO: somehow spawn magazine corresponding to gun 

            if (Input.GetKeyDown(KeyCode.B))
            {
                FireModeChangeAction();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                LoadingAction();
            }

            var scrollDelta = Input.GetAxisRaw("Mouse ScrollWheel") * 80F;
            if (!Mathf.Approximately(scrollDelta, 0) && !Input.GetKey(KeyCode.LeftShift))
            {
                AddPitchOffset(scrollDelta);
            }
        }

        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value) return;

            LoadingAction();
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
        public void LoadingAction()
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (gun == null || gun.MagazineReceiver == null) continue;

                if (gun.State == GunState.InCockingPush && gun.MagazineRoundsRemaining != 0) gun.State = GunState.Idle;
                else gun.MagazineReceiver.ReleaseMagazine();
            }
        }

        [PublicAPI]
        public void AddPitchOffset(float delta)
        {
            var localGuns = gunManager.LocalHeldGuns;
            foreach (var gun in localGuns)
            {
                if (gun == null) return;
                gun.CurrentMainHandlePitchOffset += delta;
                gun.RequestSerialization();
            }
        }
    }
}