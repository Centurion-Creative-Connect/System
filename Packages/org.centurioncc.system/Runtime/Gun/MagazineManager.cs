using System;
using CenturionCC.System.Gun.DataStore;
using DerpyNewbie.Common;
using DerpyNewbie.Logger;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace CenturionCC.System.Gun
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class MagazineManager : UdonSharpBehaviour
    {
        [SerializeField] [NewbieInject]
        private NewbieLogger logger;

        [SerializeField] [NewbieInject]
        private MagazineVariantDataStore[] magazineVariants;

        [SerializeField]
        private GameObject sourceMagazine;

        private readonly DataList _spawnedMagazines = new DataList();

        private void Start()
        {
            sourceMagazine.SetActive(false);
        }

        [PublicAPI]
        public Magazine SpawnMagazine(int type, Vector3 position, Quaternion rotation)
        {
            return SpawnMagazine(GetMagazineVariant(type), position, rotation);
        }

        [PublicAPI]
        public Magazine SpawnMagazine(MagazineVariantDataStore dataStore, Vector3 position, Quaternion rotation)
        {
            var instantiatedMagazine = Instantiate(sourceMagazine);
            instantiatedMagazine.SetActive(true);
            instantiatedMagazine.name = $"{instantiatedMagazine.name}-{dataStore.Type}-{_spawnedMagazines.Count}";

            var magazine = instantiatedMagazine.GetComponent<Magazine>();
            magazine.SetVariantData(dataStore);
            _spawnedMagazines.Add(instantiatedMagazine);
            magazine.transform.SetPositionAndRotation(position, rotation);
            return magazine;
        }

        [PublicAPI]
        public void ResetMagazines()
        {
            logger.Log("MagazineManager::ResetMagazines");
            // foreach cannot be used to DataList in UdonSharp
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _spawnedMagazines.Count; i++)
            {
                var token = _spawnedMagazines[i];
                if (token.TokenType != TokenType.Reference) continue;
                Destroy((GameObject)token.Reference);
            }

            _spawnedMagazines.Clear();
        }

        [PublicAPI] [CanBeNull]
        public MagazineVariantDataStore GetMagazineVariant(int type)
        {
            // LINQ cannot be used in UdonSharp
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var dataStore in magazineVariants)
                if (dataStore.Type == type)
                    return dataStore;
            return null;
        }
    }
}