using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace CenturionCC.System.Editor.Utils
{
    public static class CenturionSampleFactory
    {
        public enum ObjectType
        {
            UpdateManager,
            Logger,
            RoleProvider,
            AudioManager,
            CenturionSystem,
            GunManager,
            PlayerManager,
        }

        private static readonly Dictionary<ObjectType, Action<Scene>> ObjectCreators = new Dictionary<ObjectType, Action<Scene>>()
        {
            { ObjectType.UpdateManager, s => MakeObject(CenturionSampleResources.UpdateManager, s) },
            {
                ObjectType.Logger, s =>
                {
                    MakeObject(CenturionSampleResources.External.NewbieConsole, s);
                    MakeObject(CenturionSampleResources.BasicCommands, s);
                }
            },
            { ObjectType.RoleProvider, s => MakeObject(CenturionSampleResources.RoleManager, s) },
            { ObjectType.AudioManager, s => MakeObject(CenturionSampleResources.AudioManager, s) },
            {
                ObjectType.CenturionSystem, s =>
                {
                    var go = new GameObject("CenturionSystem");
                    go.AddComponent<CenturionSystem>();
                    SceneManager.MoveGameObjectToScene(go, s);
                }
            },
            {
                ObjectType.GunManager, s =>
                {
                    MakeObject(CenturionSampleResources.GunManagerSample, s);
                    MakeObject(CenturionSampleResources.GunSummonerSample, s);
                }
            },
            {
                ObjectType.PlayerManager, s =>
                {
                    MakeObject(CenturionSampleResources.PlayerManagerSample, s);
                    if (!CenturionReferenceCache.HeadUIMover) MakeObject(CenturionSampleResources.HeadUI, s);
                }
            },
        };

        public static void Create(ObjectType type, Scene scene)
        {
            ObjectCreators[type]?.Invoke(scene);
        }

        private static void MakeObject(Object o, Scene scene)
        {
            if (!CenturionReferenceCache.CenturionSystem)
            {
                var go = new GameObject("CenturionSystem");
                go.AddComponent<CenturionSystem>();
                SceneManager.MoveGameObjectToScene(go, scene);
            }

            if (PrefabUtility.IsPartOfPrefabAsset(o))
            {
                var go = PrefabUtility.InstantiatePrefab(o, scene) as GameObject;
                if (CenturionReferenceCache.CenturionSystem && go)
                {
                    go.transform.SetParent(CenturionReferenceCache.CenturionSystem.transform);
                }
            }
            else
            {
                var go = Object.Instantiate(o) as GameObject;
                if (CenturionReferenceCache.CenturionSystem && go)
                {
                    go.transform.SetParent(CenturionReferenceCache.CenturionSystem.transform);
                }
            }
        }
    }
}
