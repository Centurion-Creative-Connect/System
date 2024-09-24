using CenturionCC.System.Audio;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace CenturionCC.System.Gun.MassGun
{
    /*
    - [] source GameObject model to use for visuals because creating them separately on View would be super slow
    - [] source GunVariantData to provide more information used for View
    - [] synced information of gun (handle pos, shot data, gun states)
    */

    [DefaultExecutionOrder(110)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunModel : ManagedGun
    {
        public Vector3 Position => IsOccupied ? MainHandle.transform.position : Vector3.positiveInfinity;

        protected override void Start()
        {
            Debug.Log($"{Prefix}GunModel Start");
            base.Start();

            if (MainHandle == null)
                Debug.LogError($"{Prefix}MainHandle should not be null at this point!");
            if (SubHandle == null)
                Debug.LogError($"{Prefix}SubHandle should not be null at this point!");
            if (CustomHandle == null)
                Debug.LogError($"{Prefix}CustomHandle should not be null at this point!");
            if (pivotHandle == null)
                Debug.LogError($"{Prefix}PivotHandle should not be null at this point!");

            MainHandle.Detach();
            SubHandle.Detach();

            UpdateManager.UnsubscribeUpdate(this);
            UpdateManager.UnsubscribeSlowUpdate(this);
        }

        public void OnGunUpdate()
        {
            Internal_UpdateIsPickedUpState();
            UpdatePosition();

            if (!IsLocal)
                return;

            Internal_CheckForHandleDistance();

            if (TargetAnimator != null)
            {
                TargetAnimator.SetFloat(TriggerProgressAnimHash, GetMainTriggerPull());
                TargetAnimator.SetInteger(CurrentBulletsCountAnimHash, CurrentBulletsCount);
                TargetAnimator.SetInteger(ReservedBulletsCountAnimHash, ReservedBulletsCount);
            }

            if (Behaviour != null)
                Behaviour.OnGunUpdate(this);

            if (!IsVR)
                Internal_HandleDesktopInputs();

            if (IsInWall)
            {
                Networking.LocalPlayer.PlayHapticEventInHand(MainHandle.CurrentHand, .2F, .02F, .1F);
                Networking.LocalPlayer.PlayHapticEventInHand(SubHandle.CurrentHand, .2F, .02F, .1F);
            }
        }

        protected override void Internal_PlayAudio(AudioDataStore audioStore, Vector3 offset)
        {
            AudioManager.PlayAudioAtTransform(audioStore, MainHandle.transform, offset);
        }
    }
}