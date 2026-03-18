using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.Validation
{
    public class ReferencesValidator : IValidator
    {

        public void Validate(ValidationTarget target)
        {
            CenturionSystemReferenceCache.TargetScene = target.Scene;
            if (CenturionSystemReferenceCache.UpdateManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () =>
                    {
                        MakeObject(CenturionSystemSampleResources.UpdateManager, target.Scene);
                    },
                    Message = "UpdateManager is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionSystemReferenceCache.Logger == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () =>
                    {
                        MakeObject(CenturionSystemSampleResources.External.NewbieConsole, target.Scene);
                        MakeObject(CenturionSystemSampleResources.BasicCommands, target.Scene);
                    },
                    Message = "Logger is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionSystemReferenceCache.RoleProvider == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo()
                {
                    AutoFix = () => MakeObject(CenturionSystemSampleResources.RoleManager, target.Scene),
                    Message = "RoleProvider is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionSystemReferenceCache.AudioManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo()
                {
                    AutoFix = () =>
                    {
                        MakeObject(CenturionSystemSampleResources.AudioManager, target.Scene);
                    },
                    Message = "AudioManager is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionSystemReferenceCache.CenturionSystem == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () =>
                    {
                        var go = new GameObject("CenturionSystem");
                        go.AddComponent<CenturionSystem>();
                        SceneManager.MoveGameObjectToScene(go, target.Scene);
                    },
                    Message = "CenturionSystem is not in the scene!",
                    MessageType = MessageType.Error,
                    IsValid = false,
                });
            }

            if (CenturionSystemReferenceCache.GunManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () =>
                    {
                        MakeObject(CenturionSystemSampleResources.GunManagerSample, target.Scene);
                        MakeObject(CenturionSystemSampleResources.GunSummonerSample, target.Scene);
                    },
                    Message = "GunManager is not in the scene. Gun related systems will not work!",
                    MessageType = MessageType.Warning,
                    IsValid = true,
                });
            }

            if (CenturionSystemReferenceCache.PlayerManager == null)
            {
                target.Collector.AddValidationInfo(new ValidationInfo
                {
                    AutoFix = () =>
                    {
                        MakeObject(CenturionSystemSampleResources.PlayerManagerSample, target.Scene);
                        if (!CenturionSystemReferenceCache.HeadUIMover) MakeObject(CenturionSystemSampleResources.HeadUI, target.Scene);
                    },
                    Message = "PlayerManager is not in the scene. Player related systems will not work!",
                    MessageType = MessageType.Warning,
                    IsValid = true,
                });
            }

        }
        private static void MakeObject(Object o, Scene scene)
        {
            if (!CenturionSystemReferenceCache.CenturionSystem)
            {
                var go = new GameObject("CenturionSystem");
                go.AddComponent<CenturionSystem>();
                SceneManager.MoveGameObjectToScene(go, scene);
            }

            if (PrefabUtility.IsPartOfPrefabAsset(o))
            {
                var go = PrefabUtility.InstantiatePrefab(o, scene) as GameObject;
                if (CenturionSystemReferenceCache.CenturionSystem && go)
                {
                    go.transform.SetParent(CenturionSystemReferenceCache.CenturionSystem.transform);
                }
            }
            else
            {
                var go = Object.Instantiate(o) as GameObject;
                if (CenturionSystemReferenceCache.CenturionSystem && go)
                {
                    go.transform.SetParent(CenturionSystemReferenceCache.CenturionSystem.transform);
                }
            }
        }
    }
}
