using System;
using System.Diagnostics;
using UdonSharp;
using UnityEngine;
namespace CenturionCC.System.Gun.Centurion
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CenturionProjectilePool : ProjectilePoolBase
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
                    UnityEngine.Debug.LogError($"[CenturionProjectilePool] GunBullet at {obj.name} is null!");
                    continue;
                }

                obj.SetActive(false);

                _bullets[_lastGeneratedBulletCount] = bullet;
                ++_lastGeneratedBulletCount;
            }

            if (_lastGeneratedBulletCount >= _bullets.Length)
            {
                UnityEngine.Debug.Log("[CenturionProjectilePool] Successfully generated all bullets!");
                _hasInit = true;
                return;
            }

            SendCustomEventDelayedFrames(nameof(GenerateSteps), 5);
        }

        private ProjectileBase GetProjectileInternal(int retryCount)
        {
            if (!HasInitialized) return null;

            while (retryCount > 0)
            {
                if (++_lastBulletIndex >= _bullets.Length)
                {
                    _lastBulletIndex = 0;
                }

                var bullet = _bullets[_lastBulletIndex];
                if (!bullet)
                {
                    UnityEngine.Debug.LogError($"[CenturionProjectilePool] bullet pool contains null at {_lastBulletIndex}");
                    --retryCount;
                    continue;
                }

                if (bullet.gameObject.activeSelf)
                {
                    --retryCount;
                    continue;
                }

                return bullet;
            }

            return _bullets[_lastBulletIndex];
        }

        public override ProjectileBase GetProjectile()
        {
            var bullet = GetProjectileInternal(gunBulletMax);

            if (bullet.gameObject.activeSelf)
            {
                UnityEngine.Debug.LogWarning(
                    $"[CenturionProjectilePool] detected bullet pool reuse at {bullet.name}, index of {_lastBulletIndex}");
            }

            return bullet;
        }

        public override ProjectileBase Shoot(Guid eventId,
                                             Vector3 pos, Quaternion rot,
                                             Vector3 velocity, Vector3 torque, float drag,
                                             string damageType, float damageAmount,
                                             DateTime time, int playerId,
                                             float trailTime, Gradient trailGradient, float lifeTimeInSeconds)
        {
            var projectile = GetProjectile();
            if (!projectile)
            {
                return null;
            }

            projectile.ResetDamageSetting();
            projectile.Shoot(
                eventId,
                pos, rot,
                velocity, torque, drag,
                damageType, damageAmount,
                time, playerId,
                trailTime, trailGradient, lifeTimeInSeconds
            );
            return projectile;
        }
    }
}
