using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MagazineReceiver : UdonSharpBehaviour
    {
        private const float ReattachTimeoutInSeconds = 0.1F;

        [SerializeField] [NewbieInject]
        private MagazineManager magazineManager;

        protected float LastDetachedTime = 0;

        [CanBeNull] protected Magazine Magazine;

        [CanBeNull] protected GunBase ParentGun;

        [PublicAPI] public virtual bool HasMagazine => Magazine != null;

        [PublicAPI] public virtual int MagazineType => Magazine != null ? Magazine.Type : 0;
        [PublicAPI] public virtual int MagazineRoundsCapacity => Magazine != null ? Magazine.RoundsCapacity : 0;
        [PublicAPI] public virtual int MagazineRoundsRemaining => Magazine != null ? Magazine.RoundsRemaining : 0;

        private void OnTriggerEnter(Collider other)
        {
            // Possible as Udon protects player objects
            if (other == null) return;

            Debug.Log($"[{name}-MagReceiver] OnTriggerEnter: {other.name}");
            var mag = other.GetComponent<Magazine>();
            if (mag == null) return;

            InsertMagazine(mag);
        }

        public virtual bool HasNextBullet()
        {
            return Magazine != null && Magazine.HasNextBullet();
        }

        public virtual bool ConsumeBullet()
        {
            return Magazine != null && Magazine.ConsumeBullet();
        }

        public virtual void InsertMagazine(Magazine magazine)
        {
            if (magazine == null || Magazine != null || magazine.IsAttachedToReceiver) return;

            // Cooldown check
            if (Time.timeSinceLevelLoad - LastDetachedTime < ReattachTimeoutInSeconds) return;

            // Alignment Check
            if (Vector3.Dot(transform.forward, magazine.transform.forward) <= 0.8) return;

            Magazine = magazine.AttachToReceiver(this);

            if (ParentGun != null) ParentGun.OnMagazineChanged();
        }

        public virtual void ReleaseMagazine()
        {
            if (Magazine == null) return;

            Debug.Log($"[MagazineReceiver-{name}] Releasing Magazine");

            Magazine.Detach();
            Magazine = null;
            LastDetachedTime = Time.timeSinceLevelLoad;

            if (ParentGun != null) ParentGun.OnMagazineChanged();
        }

        public virtual void SetMagazineType(int magazineType)
        {
            if (magazineType == 0)
            {
                if (Magazine != null) Destroy(Magazine.gameObject);
                Magazine = null;
                return;
            }

            if (Magazine != null && Magazine.Type == magazineType) return;

            if (Magazine != null) Destroy(Magazine.gameObject);

            Magazine = magazineManager.SpawnMagazine(magazineType, transform.position, transform.rotation);
            if (Magazine == null)
            {
                Debug.LogError($"[MagazineReceiver] Could not spawn magazine with type id {magazineType}!", this);
                return;
            }

            Magazine.AttachToReceiver(this);
        }

        public virtual void Setup(GunBase gun)
        {
            ParentGun = gun;
        }
    }
}