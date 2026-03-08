using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class GunReloadHelper : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject(SearchScope.Self)]
        private GunBase gun;
        private bool _isReloadTicking;
        private int _nextBulletsRemaining;
        private float _reloadDuration;

        private float _reloadStartedTime;

        [NetworkCallable]
        public void DoSimplifiedReload()
        {
            DoSimplifiedReload_Complex(gun.ReloadTimeInSeconds, gun.DefaultMagazineSize, false);
        }

        [NetworkCallable]
        public void DoSimplifiedReload_Complex(float reloadDuration, int nextBulletsRemaining, bool force)
        {
            if (!force && nextBulletsRemaining == gun.BulletsInMagazine)
            {
                Debug.Log($"[GunReloadHelper-{name}] DoSimplifiedReload: aborted because magazine is already full");
                return;
            }

            var now = Time.timeSinceLevelLoad;
            _reloadStartedTime = now;
            _reloadDuration = reloadDuration;
            _nextBulletsRemaining = nextBulletsRemaining;

            if (_isReloadTicking)
            {
                Debug.Log($"[GunReloadHelper-{name}] DoSimplifiedReload: will not schedule tick because it is already ticking");
                return;
            }

            _isReloadTicking = true;

            _Internal_SimplifiedReloadTick();
        }

        public void _Internal_SimplifiedReloadTick()
        {
            var now = Time.timeSinceLevelLoad;
            var progress = (now - _reloadStartedTime) / _reloadDuration;

            gun.State = GunState.Reloading;
            gun.AnimationHelper._SetReloadProgress(Mathf.Clamp01(progress));

            if (progress < 1)
            {
                // maybe defer more if this gun was far from the player
                SendCustomEventDelayedFrames(nameof(_Internal_SimplifiedReloadTick), 1);
                return;
            }

            gun.State = GunState.Idle;
            gun.BulletsInMagazine = _nextBulletsRemaining;
            _isReloadTicking = false;

            Debug.Log($"[GunReloadHelper-{name}] _Internal_SimplifiedReloadTick: reload completed");
        }
    }
}
