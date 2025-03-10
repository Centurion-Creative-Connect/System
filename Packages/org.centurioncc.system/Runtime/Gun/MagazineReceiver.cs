using DerpyNewbie.Common;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MagazineReceiver : UdonSharpBehaviour
    {
        protected const float ReattachTimeoutInSeconds = 0.2F;

        [SerializeField] [NewbieInject]
        protected MagazineManager magazineManager;

        [SerializeField] protected bool releaseMagazineOnReleaseButton = true;
        [SerializeField] protected bool makeMagazinePickupableOnReleaseButton = true;
        [SerializeField] protected Transform downReference;
        [SerializeField] protected float releaseImpulseMultiplier = 0.5F;

        protected float LastDetachedTime = 0;

        [CanBeNull] protected Magazine Magazine;

        [CanBeNull] protected GunBase ParentGun;

        [PublicAPI] public virtual bool HasMagazine => Magazine != null;

        [PublicAPI] public virtual int MagazineType => Magazine != null ? Magazine.Type : 0;
        [PublicAPI] public virtual int MagazineRoundsCapacity => Magazine != null ? Magazine.RoundsCapacity : 0;
        [PublicAPI] public virtual int MagazineRoundsRemaining => Magazine != null ? Magazine.RoundsRemaining : 0;

        protected virtual void Start()
        {
            if (downReference == null)
                downReference = transform;
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            // Possible as Udon protects player objects
            if (other == null) return;

            Debug.Log($"[{name}-MagReceiver] OnTriggerEnter: {other.name}");
            var mag = other.GetComponentInParent<Magazine>();
            if (mag == null)
            {
                Debug.Log($"[{name}-MagReceiver] Magazine not found");
                return;
            }

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
            if (!CanAttach(magazine)) return;

            Magazine = magazine.AttachToReceiver(this);

            if (ParentGun != null) ParentGun.OnMagazineChanged();
        }

        public virtual void ReleaseMagazine()
        {
            if (Magazine == null) return;

            Debug.Log($"[MagazineReceiver-{name}] Releasing Magazine");

            Magazine.Detach();
            Magazine.Rigidbody.AddForce(downReference.up * (-1 * releaseImpulseMultiplier), ForceMode.Impulse);
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

        public virtual void OnMagazineReleaseButtonDown()
        {
            if (Magazine == null) return;

            if (releaseMagazineOnReleaseButton)
            {
                ReleaseMagazine();
                return;
            }

            if (makeMagazinePickupableOnReleaseButton)
            {
                Magazine.Pickupable = true;
            }
        }

        public virtual void OnMagazineReleaseButtonUp()
        {
            if (Magazine == null) return;

            if (makeMagazinePickupableOnReleaseButton)
            {
                Magazine.Pickupable = false;
            }
        }

        [PublicAPI]
        public virtual bool IsCompatibleWith(Magazine magazine)
        {
            return magazine != null && ParentGun != null && ParentGun.AllowedMagazineTypes.ContainsItem(magazine.Type);
        }

        [PublicAPI]
        public virtual bool CanAttach(Magazine magazine)
        {
            if (magazine == null || Magazine != null || magazine.IsAttachedToReceiver) return false;

            if (magazine.IsAttached && !magazine.IsAttachedToMagazine && !magazine.IsAttachedToReceiver)
            {
                Debug.Log($"[{name}-MagReceiver] Attached to pouch");
                return false;
            }

            // Magazine type check
            if (!IsCompatibleWith(magazine))
            {
                Debug.Log($"[{name}-MagReceiver] Incompatible magazine type: {magazine.Type}");
                return false;
            }

            // Cooldown check
            if (Time.timeSinceLevelLoad - LastDetachedTime < ReattachTimeoutInSeconds)
            {
                Debug.Log($"[{name}-MagReceiver] In cooldown");
                return false;
            }

            // Alignment Check
            if (Vector3.Dot(transform.forward, magazine.transform.forward) <= 0.8)
            {
                Debug.Log($"[{name}-MagReceiver] Wrong alignment");
                return false;
            }

            return true;
        }

        public virtual void Setup(GunBase gun)
        {
            ParentGun = gun;
        }
    }
}