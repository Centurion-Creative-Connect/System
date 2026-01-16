using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun.DataStore;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor
{
    public class CenturionSystemMigrator : ILogProvider
    {

        public void AppendLog(string message)
        {
            Debug.Log(message);
        }
        public static void UpgradeGunVariantDataStore(GunVariantDataStore dataStore, ILogProvider logger = null, bool ignoreVersion = false)
        {
            logger ??= new CenturionSystemMigrator();

            var prefix = $"[CenturionSystemMigrator] UpgradeGunVariantDataStore({dataStore.name}): ";
            logger.AppendLog($"{prefix}upgrading {dataStore.name}");
            Undo.RecordObject(dataStore, "Centurion System Migration: Upgrade Gun Variant Data Store");
            var serializedObject = new SerializedObject(dataStore);
            var dataVersionProperty = serializedObject.FindProperty("dataVersion");
            var dataVersion = dataVersionProperty.intValue;

            // begin upgrade data prior to 0.7.x
            if (dataVersion <= 1 || ignoreVersion)
            {
                logger.AppendLog($"{prefix}upgrading data prior to 0.7.x");

                // upgrade behaviour -> behaviours (0.7.x)
                {
                    var oldBehaviour = serializedObject.FindProperty("behaviour").objectReferenceValue;
                    var behavioursProperty = serializedObject.FindProperty("behaviours");
                    behavioursProperty.arraySize = 1;
                    var firstBehavioursProperty = behavioursProperty.GetArrayElementAtIndex(0);
                    if (firstBehavioursProperty.objectReferenceValue == null)
                    {
                        firstBehavioursProperty.objectReferenceValue = oldBehaviour;
                    }
                    else
                    {
                        logger.AppendLog($"{prefix}Skipping behaviour migration because it's already set");
                    }
                }

                // upgrade fire mode properties (0.7.x)
                {
                    var oldRpsProperty = serializedObject.FindProperty("maxRoundsPerSecond");
                    var fireModeArrayProperty = serializedObject.FindProperty("availableFiringModes");
                    var rpsArrayProperty = serializedObject.FindProperty("secondsPerRoundArray");
                    var perBurstIntervalArrayProperty = serializedObject.FindProperty("perBurstIntervalArray");
                    rpsArrayProperty.arraySize = fireModeArrayProperty.arraySize;
                    perBurstIntervalArrayProperty.arraySize = fireModeArrayProperty.arraySize;
                    var oldRps = oldRpsProperty.floatValue;
                    var nextSpr = !float.IsInfinity(oldRps) && oldRps != 0 ? 1 / oldRps : oldRps;
                    for (var i = 0; i < fireModeArrayProperty.arraySize; i++)
                    {
                        rpsArrayProperty.GetArrayElementAtIndex(i).floatValue = nextSpr;
                    }
                }

                // search for newly added animator property (0.7.x)
                {
                    var animatorProperty = serializedObject.FindProperty("animator");
                    if (animatorProperty.objectReferenceValue == null)
                    {
                        var animator = dataStore.GetComponentInChildren<Animator>();
                        animatorProperty.objectReferenceValue = animator;
                    }
                    else
                    {
                        logger.AppendLog($"{prefix}Skipping animator migration because it's already set");
                    }
                }
            }
            // end upgrade data prior to 0.7.x

            // update data version to suppress warnings in inspector
            serializedObject.FindProperty("dataVersion").intValue = GunVariantDataStore.DataVersion;

            EditorUtility.SetDirty(dataStore);
            serializedObject.ApplyModifiedProperties();

            logger.AppendLog($"{prefix}finished migration for {dataStore.name}");
        }
    }
}
