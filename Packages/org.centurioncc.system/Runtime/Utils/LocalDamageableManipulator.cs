using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalDamageableManipulator : UdonSharpBehaviour
    {
        [SerializeField] private LocalDamageable[] damageableObjects;

        [PublicAPI]
        public void EnableDamageableObjects()
        {
            SetDamageableObjectsActive(true);
        }

        [PublicAPI]
        public void DisableDamageableObjects()
        {
            SetDamageableObjectsActive(false);
        }

        [PublicAPI]
        public void ToggleDamageableObjects()
        {
            foreach (var damageable in damageableObjects)
            {
                damageable.ToggleDamageable();
            }
        }

        [PublicAPI]
        public void SetDamageableObjectsActive(bool b)
        {
            foreach (var damageable in damageableObjects)
            {
                damageable.SetDamageable(b);
            }
        }
    }
}