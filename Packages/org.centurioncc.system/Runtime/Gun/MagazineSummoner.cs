using DerpyNewbie.Common;
using UdonSharp;
using UnityEngine;

namespace CenturionCC.System.Gun
{
    public class MagazineSummoner : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private MagazineManager magazineManager;

        [SerializeField] private int magazineType;
        [SerializeField] private Transform summonPosition;

        public void Spawn()
        {
            magazineManager.SpawnMagazine(magazineType, summonPosition.position, summonPosition.rotation);
        }

        public override void Interact()
        {
            Spawn();
        }
    }
}