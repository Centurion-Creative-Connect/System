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
        private readonly DataDictionary _storedMagazines = new DataDictionary();

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
                var go = (GameObject)token.Reference;
                var magazine = GetComponent<Magazine>();
                if (magazine.IsAttached) continue;
                _spawnedMagazines.Remove(token);
                Destroy(go);
                --i;
            }
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

        [PublicAPI]
        public bool StoreMagazine(Magazine magazine)
        {
            if (!_storedMagazines.ContainsKey(magazine.Type)) _storedMagazines.Add(magazine.Type, new DataList());
            var remainingBullets = _storedMagazines[magazine.Type].DataList;
            remainingBullets.Add(magazine.RoundsRemaining);
            remainingBullets.Sort();
            return true;
        }

        [PublicAPI] [CanBeNull]
        public Magazine RestoreMagazine(int type)
        {
            if (!_storedMagazines.ContainsKey(type)) return null;
            var remainingBullets = _storedMagazines[type].DataList;
            if (!remainingBullets.TryGetValue(0, out var bullets)) return null;
            var magazine = SpawnMagazine(type, Vector3.zero, Quaternion.identity);
            magazine.RoundsRemaining = bullets.Int;
            remainingBullets.RemoveAt(0);
            return magazine;
        }

        [PublicAPI]
        public void FillMagazineStore(int type, int count)
        {
            var variantData = GetMagazineVariant(type);
            if (variantData == null) return;

            ClearMagazineStore(type);

            _storedMagazines.Add(type, new DataList());
            for (var i = 0; i < count; i++)
                _storedMagazines[type].DataList.Add(variantData.RoundsCapacity);
        }

        [PublicAPI]
        public void ClearMagazineStore(int type)
        {
            _storedMagazines.Remove(type);
        }
    }
}