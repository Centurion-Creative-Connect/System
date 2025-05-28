using System;
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
        private bool _hasInit;

        private int _lastBulletIndex;
        private int _lastGeneratedBulletCount;

        public override bool HasInitialized => _hasInit;

        private void Start()
        {
            _hasInit = false;
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

                obj.SetActive(false);

                _bullets[_lastGeneratedBulletCount] = bullet;
                ++_lastGeneratedBulletCount;
            }

            if (_lastGeneratedBulletCount >= _bullets.Length)
            {
                UnityEngine.Debug.Log("[GunBulletHolder] Successfully generated all bullets!");
                _hasInit = true;
                return;
            }

            SendCustomEventDelayedFrames(nameof(GenerateSteps), 5);
        }

        private ProjectileBase GetProjectile(int retryCount)
        {
            while (true)
            {
                if (!HasInitialized) return null;

                if (retryCount < 0) return _bullets[_lastBulletIndex];

                if (++_lastBulletIndex >= _bullets.Length) _lastBulletIndex = 0;
                var bullet = _bullets[_lastBulletIndex];
                if (bullet == null)
                {
                    UnityEngine.Debug.LogError($"[GunBulletHolder] bullet pool contains null at {_lastBulletIndex}");
                    --retryCount;
                    continue;
                }

                if (bullet.IsCurrentlyActive)
                {
                    --retryCount;
                    continue;
                }

                return bullet;
            }
        }

        public override ProjectileBase GetProjectile()
        {
            var bullet = GetProjectile(gunBulletMax);

            if (bullet.IsCurrentlyActive)
            {
                UnityEngine.Debug.LogWarning(
                    $"[GunBulletHolder] detected bullet pool reuse at {bullet.name}, index of {_lastBulletIndex}");
            }

            return bullet;
        }

        public override ProjectileBase Shoot(Guid eventId,
            Vector3 pos, Quaternion rot,
            Vector3 velocity, Vector3 torque, float drag,
            string damageType, DateTime time,
            int playerId, float trailTime, Gradient trailGradient,
            float lifeTimeInSeconds)
        {
            var projectile = GetProjectile();
            if (projectile == null)
                return null;

            projectile.ResetDamageSetting();
            projectile.Shoot(eventId, pos, rot, velocity, torque, drag, damageType, time, playerId, trailTime,
                trailGradient,
                lifeTimeInSeconds);
            return projectile;
        }
    }
}