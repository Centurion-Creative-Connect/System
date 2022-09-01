using System.Diagnostics;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunBulletHolder : ProjectilePool
    {
        [SerializeField]
        private GameObject gunBulletSource;
        [SerializeField]
        private int gunBulletMax = 300;
        [SerializeField]
        private GameObject gunBulletPoolRoot;
        private ProjectileBase[] _bullets;

        private bool _isInitialized;

        private int _lastBulletIndex;
        private int _lastGeneratedBulletCount;

        private void Start()
        {
            _bullets = new ProjectileBase[gunBulletMax];
            GenerateSteps();
        }

        public void GenerateSteps()
        {
            // Not sure about using Stopwatch everytime for generating steps, though i'm super lazy.
            var stopwatch = Stopwatch.StartNew();

            while (_lastGeneratedBulletCount < _bullets.Length)
            {
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    stopwatch.Stop();
                    break;
                }

                var obj = Instantiate(gunBulletSource, gunBulletPoolRoot.transform, true);
                obj.SetActive(true);
                obj.name = $"Bullet (Generated-{_lastGeneratedBulletCount})";
                var bullet = obj.GetComponent<ProjectileBase>();
                if (bullet == null)
                {
                    UnityEngine.Debug.LogError($"[GunBulletHolder] GunBullet at {obj.name} is null!");
                    continue;
                }

                _bullets[_lastGeneratedBulletCount] = bullet;
                ++_lastGeneratedBulletCount;
            }

            if (_lastGeneratedBulletCount >= _bullets.Length)
            {
                UnityEngine.Debug.Log("[GunBulletHolder] Successfully generated all bullets!");
                _isInitialized = true;
                return;
            }

            SendCustomEventDelayedFrames(nameof(GenerateSteps), 5);
        }

        public override ProjectileBase GetProjectile()
        {
            if (!_isInitialized)
                return null;

            if (++_lastBulletIndex >= _bullets.Length)
                _lastBulletIndex = 0;
            var bullet = _bullets[_lastBulletIndex];
            if (bullet == null)
            {
                UnityEngine.Debug.LogError($"[GunBulletHolder] bullet pool contains null at {_lastBulletIndex}");
                return null;
            }

            if (bullet.IsCurrentlyActive)
            {
                UnityEngine.Debug.LogWarning(
                    $"[GunBulletHolder] detected bullet pool reuse at {bullet.name}, index of {_lastBulletIndex}");
            }

            return bullet;
        }
    }
}