using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun.DataStore;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class MigrationPanelDrawer : IControlPanelDrawer, ILogProvider
    {
        private static int _runningMigration = 0;
        private static string _logString;
        private static Vector2 _scrollPosition;

        public void Draw()
        {
            EditorGUILayout.HelpBox("Please note that migration is very experimental, and may break your scene. Backup entire project before migrating!", MessageType.Info);
            EditorGUILayout.HelpBox("Currently migration only work in the active scene. Prefab source or other unopened scenes will not be updated.", MessageType.Info);
            EditorGUILayout.HelpBox("マイグレーションは実験的な機能で、シーンやプロジェクトを壊してしまう可能性があります。実行前にプロジェクトのバックアップを!", MessageType.Info);
            EditorGUILayout.HelpBox("現在マイグレーションは開いているシーンでのみ適用されます。Prefab の大本や開いていない Scene はマイグレーションされません。", MessageType.Info);

            if (_runningMigration <= 0)
            {
                if (GUILayout.Button("Perform Migration"))
                {
                    PerformMigration();
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField($"Running migration {_runningMigration}...");
                    GUILayout.Button("Migration In Progress");
                }
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition);
            _scrollPosition = scroll.scrollPosition;
            EditorGUILayout.TextArea(_logString, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        }

        public void AppendLog(string message)
        {
            _logString += message + "\n";
        }

        public void PerformMigration()
        {
            AppendLog($"Starting migration at {DateTime.Now.ToLongTimeString()}");
            AppendLog("Searching all GunVariantDataStore");
            ++_runningMigration;
            SearchService.Request("t:GunVariantDataStore", OnGunVariantSearchCompleted, SearchFlags.WantsMore);
        }

        private void OnGunVariantSearchCompleted(SearchContext ctx, IList<SearchItem> items)
        {
            int processingCount = 0, processedCount = 0, totalCount = items.Count;
            AppendLog($"[GunVariantDataStore Migration] {totalCount} items found");
            foreach (var item in items)
            {
                ++processingCount;
                AppendLog("[GunVariantDataStore Migration] Processing item {" + processingCount + "}:");
                try
                {
                    var dataStore = TryRetrieveDataStore(item);
                    if (dataStore == null)
                    {
                        AppendLog($"[GunVariantDataStore Migration] Could not process item {processingCount}:{item}:{item.data}:{item.value}");
                        continue;
                    }

                    AppendLog($"[GunVariantDataStore Migration] Found data {dataStore.name}");
                    CenturionSystemMigrator.UpgradeGunVariantDataStore(dataStore, this);

                    ++processedCount;
                }
                catch (Exception e)
                {
                    AppendLog($"[GunVariantDataStore Migration] exception has occurred: {e}");
                    Debug.LogException(e);
                }
            }


            AppendLog($"[GunVariantDataStore Migration] Migration completed with {processedCount}/{totalCount} items processed");
            CompleteMigrationPart();
        }

        private static GunVariantDataStore TryRetrieveDataStore(SearchItem item)
        {
            var gunVariantDataStore = item.ToObject<GunVariantDataStore>();
            if (gunVariantDataStore) return gunVariantDataStore;

            var go = item.ToObject<GameObject>();
            return go ? go.GetComponent<GunVariantDataStore>() : null;
        }

        private void CompleteMigrationPart()
        {
            --_runningMigration;
            if (_runningMigration <= 0)
            {
                EditorUtility.DisplayDialog("Migration Completed", "Migration completed!", "OK");
            }
        }
    }
}
