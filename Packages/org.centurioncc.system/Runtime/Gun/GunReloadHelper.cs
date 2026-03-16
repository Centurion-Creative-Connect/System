using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;
namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GunReloadHelper : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject(SearchScope.Self)]
        private GunBase gun;
        private bool _isReloadTicking;
        private int _nextBulletsRemaining;
        private float _reloadDuration;

        private float _reloadStartedTime;

        public bool IsReloading => Time.timeSinceLevelLoad < _reloadStartedTime + _reloadDuration;
        public bool HasMagazine { get; private set; }

        [PublicAPI]
        public void _DoSimplifiedReload(bool force = false)
        {
            _DoSimplifiedReload_Complex(gun.ReloadTimeInSeconds, gun.DefaultMagazineSize, force);
        }

        [PublicAPI]
        public void _DoSimplifiedReload_Complex(float reloadDuration, int nextBulletsRemaining, bool force)
        {
            if (IsReloading)
            {
                Debug.Log($"[GunReloadHelper-{name}] _DoSimplifiedReload_Complex: aborted because already reloading");
                return;
            }

            if (!force && nextBulletsRemaining == gun.BulletsInMagazine)
            {
                Debug.Log($"[GunReloadHelper-{name}] Internal_HandleSimplifiedReload: aborted because magazine is already full");
                return;
            }

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Internal_HandleSimplifiedReload), reloadDuration, nextBulletsRemaining, force);
        }

        [NetworkCallable]
        public void Internal_HandleSimplifiedReload(float reloadDuration, int nextBulletsRemaining, bool force)
        {
            var now = Time.timeSinceLevelLoad;
            _reloadStartedTime = now;
            _reloadDuration = reloadDuration;
            _nextBulletsRemaining = nextBulletsRemaining;

            HasMagazine = false;
            gun.BulletsInMagazine = 0;

            if (gun.AudioData != null)
            {
                gun._PlayAudio(gun.AudioData.MagazineReleased, gun.AudioData.MagazineReleasedOffset);
            }

            if (_isReloadTicking)
            {
                Debug.Log($"[GunReloadHelper-{name}] Internal_HandleSimplifiedReload: will not schedule tick because it is already ticking");
                return;
            }

            _isReloadTicking = true;

            _Internal_SimplifiedReloadTick();
        }

        public void _Internal_SimplifiedReloadTick()
        {
            var now = Time.timeSinceLevelLoad;
            var progress = (now - _reloadStartedTime) / _reloadDuration;

            gun.AnimationHelper._SetReloadProgress(Mathf.Clamp01(progress));

            if (progress < 1)
            {
                // maybe defer more if this gun was far from the player
                SendCustomEventDelayedFrames(nameof(_Internal_SimplifiedReloadTick), 1);
                return;
            }

            HasMagazine = true;
            gun.BulletsInMagazine = _nextBulletsRemaining;

            _isReloadTicking = false;

            if (gun.AudioData != null)
            {
                gun._PlayAudio(gun.AudioData.MagazineInserted, gun.AudioData.MagazineInsertedOffset);
            }

#if CENTURIONSYSTEM_GUN_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log($"[GunReloadHelper-{name}] _Internal_SimplifiedReloadTick: reload completed");
#endif
        }
    }
}
