using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Utils;
using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CenturionCC.System.Editor.EditorInspector.Utilities
{
    [CustomEditor(typeof(TerrainMarker))]
    public class TerrainMarkerEditor : UnityEditor.Editor
    {
        private const string InspectionInPlayModeLabel =
            "TerrainMarker has baked data. Showing inspector will cause massive lag!";
        private const string ShouldNotHaveBakedDataLabel =
            "TerrainMarker should not have baked data in editor";
        private const string TerrainUpdatedLabel =
            "Terrain was updated! You will have to reload terrain data to apply changes.";
        private const string MarkerDataHeader = "Terrain ObjectMarker Mapping";
        private const string ClearBakedDataLabel = "Clear Baked Data";
        private const string ReloadDataLabel = "Reload";

        private const string GettingAllTerrainMarkerLabel = "Getting TerrainMarkers in scene";
        private const string ClearBakedDataTitleLabel = "Clear Baked TerrainMarker data";
        private const string BakeTerrainTitleLabel = "Bake TerrainMarker data";
        private const string ProgressLabel = "Processing {0}/{1}";
        private static bool _hasListFoldedOut;
        private readonly Dictionary<Texture2D, (ObjectType, float, ReorderableList, bool)> _markerData =
            new Dictionary<Texture2D, (ObjectType, float, ReorderableList, bool)>();
        private TerrainLayer[] _lastCheckedTerrainLayers;


        private Terrain _targetTerrain;

        private void OnEnable()
        {
            var marker = (TerrainMarker)target;
            if (HasBakedData(marker))
                ClearBakedData(marker);
            Reload();
        }

        private void OnDisable()
        {
            WriteData();
        }

        private void Reload()
        {
            var terrainMarker = (TerrainMarker)target;
            var terrain = terrainMarker.GetComponent<Terrain>();
            _targetTerrain = terrain;
            _lastCheckedTerrainLayers = terrain.terrainData.terrainLayers;
            // Construct _markerData
            ReadData();
        }

        private void ReadData()
        {
            _markerData.Clear();
            var marker = (TerrainMarker)target;
            for (var i = 0; i < marker.terrainTextureCount; i++)
            {
                var reorderableList = new ReorderableList(marker.correspondingTags[i].ToList(), typeof(string))
                {
                    displayAdd = true,
                    displayRemove = true,
                    draggable = true,
                    onAddCallback = list => { list.list.Add(""); }
                };
                reorderableList.drawElementCallback = (rect, index, active, focused) =>
                {
                    if (!_hasListFoldedOut) return;

                    rect.height = EditorGUIUtility.singleLineHeight;
                    var v = EditorGUI.TextField(
                        EditorGUI.PrefixLabel(rect, new GUIContent($"Element {index}")),
                        (string)reorderableList.list[index]
                    );

                    if (!v.Equals(reorderableList.list[index]))
                    {
                        reorderableList.list[index] = v;
                        reorderableList.onChangedCallback.Invoke(reorderableList);
                    }
                };

                _markerData.Add(
                    marker.correspondingTextureHint[i],
                    (
                        marker.correspondingObjectType[i],
                        marker.correspondingSpeedMultiplier[i],
                        reorderableList,
                        false
                    )
                );
            }
        }

        private void WriteData()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUI.BeginChangeCheck();
            var marker = (TerrainMarker)target;
            var terrainLayers = _targetTerrain.terrainData.terrainLayers;
            var size = terrainLayers.Length;
            marker.terrainTextureCount = size;
            marker.correspondingTextureHint = new Texture2D[size];
            marker.correspondingObjectType = new ObjectType[size];
            marker.correspondingSpeedMultiplier = new float[size];
            marker.correspondingTags = new string[size][];

            for (var i = 0; i < terrainLayers.Length; i++)
            {
                if (!_markerData.TryGetValue(terrainLayers[i].diffuseTexture, out var obj))
                {
                    UnityEngine.Debug.Log($"Tex did not exist for {terrainLayers[i].name}. Adding!", terrainLayers[i]);
                    marker.correspondingTextureHint[i] = terrainLayers[i].diffuseTexture;
                    marker.correspondingObjectType[i] = default;
                    marker.correspondingSpeedMultiplier[i] = 1F;
                    marker.correspondingTags[i] = Array.Empty<string>();
                    continue;
                }

                marker.correspondingTextureHint[i] = terrainLayers[i].diffuseTexture;
                marker.correspondingObjectType[i] = obj.Item1;
                marker.correspondingSpeedMultiplier[i] = obj.Item2;
                marker.correspondingTags[i] = obj.Item3.list.Cast<string>().ToArray();
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            if (HasBakedData((TerrainMarker)target))
            {
                if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox(InspectionInPlayModeLabel, MessageType.Warning);
                    return;
                }

                if (GUIUtil.HelpBoxWithButton(ShouldNotHaveBakedDataLabel, MessageType.Warning, ClearBakedDataLabel))
                {
                    ClearBakedData((TerrainMarker)target);
                    return;
                }
            }

            if (!_targetTerrain.terrainData.terrainLayers.SequenceEqual(_lastCheckedTerrainLayers))
            {
                if (GUIUtil.HelpBoxWithButton(TerrainUpdatedLabel, MessageType.Warning, ReloadDataLabel))
                {
                    WriteData();
                    Reload();
                    return;
                }
            }

            GUILayout.Space(5F);
            using (new EditorGUILayout.VerticalScope("RL Header"))
            {
                EditorGUILayout.LabelField(MarkerDataHeader);
            }

            using (new EditorGUILayout.VerticalScope("RL Background"))
            {
                DrawTerrainObjectMarkerMap();
            }
        }

        private void DrawTerrainObjectMarkerMap()
        {
            var kvpList = _markerData.ToList();
            foreach (var kvp in kvpList)
            {
                DrawElement(kvp.Key, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4);
            }
        }

        private void DrawElement(
            Texture2D key, ObjectType type, float speedMultiplier,
            ReorderableList reorderableList, bool hasFoldedOut
        )
        {
            using (new EditorGUILayout.HorizontalScope(new GUIStyle("RL Element")
                       { padding = new RectOffset(10, 10, 10, 0) }))
            {
                // Terrain Texture Preview
                GUILayout.Box(
                    key,
                    GUILayout.Width(EditorGUIUtility.singleLineHeight * 3),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight * 3)
                );

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(key.name, EditorStyles.boldLabel);

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField("Object Type", GUILayout.MinWidth(70));
                    var newType = (ObjectType)EditorGUILayout.EnumPopup(type, GUILayout.MinWidth(70));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("Speed Multiplier", GUILayout.MinWidth(100));
                    var newSpeedMultiplier = EditorGUILayout.DelayedFloatField(speedMultiplier, GUILayout.MinWidth(70));

                    EditorGUILayout.EndHorizontal();

                    var newHasFoldedOut = hasFoldedOut;
                    reorderableList.drawHeaderCallback = rect =>
                    {
                        var foldedOutRect = new Rect(rect);
                        foldedOutRect.x += 15;
                        foldedOutRect.width = 100;
                        newHasFoldedOut = EditorGUI.Foldout(foldedOutRect, hasFoldedOut,
                            hasFoldedOut ? "Tags" : "Tags (Folded)");
                        var sizeRect = new Rect(rect);
                        sizeRect.x = sizeRect.xMax - 48;
                        sizeRect.width = 48;
                        EditorGUI.IntField(sizeRect, reorderableList.list.Count);
                        sizeRect.x -= 30;
                        sizeRect.width = 30;
                        EditorGUI.LabelField(sizeRect, "Size");
                    };

                    var hasListChanged = false;
                    reorderableList.onChangedCallback = list =>
                    {
                        hasListChanged = true;
                        newHasFoldedOut = true;
                    };

                    reorderableList.draggable = newHasFoldedOut;
                    reorderableList.elementHeight = newHasFoldedOut ? EditorGUIUtility.singleLineHeight * 1.2F : 0F;
                    _hasListFoldedOut = newHasFoldedOut;
                    reorderableList.DoLayoutList();

                    var objTypeChanged = type != newType;
                    var spdMultChanged = !Mathf.Approximately(speedMultiplier, newSpeedMultiplier);
                    var foldOutChanged = hasFoldedOut != newHasFoldedOut;
                    if (objTypeChanged || spdMultChanged || hasListChanged || foldOutChanged)
                    {
                        _markerData[key] = (newType, newSpeedMultiplier, reorderableList, newHasFoldedOut);
                        if (!foldOutChanged) WriteData();
                    }
                }
            }
        }

        public static void BakeAll()
        {
            EditorUtility.DisplayProgressBar(BakeTerrainTitleLabel, GettingAllTerrainMarkerLabel, 0F);

            var terrainMarkers = GetTerrainMarkers();
            var count = 0;
            try
            {
                foreach (var terrainMarker in terrainMarkers)
                {
                    var progressDescription = string.Format(ProgressLabel, count, terrainMarkers.Count);
                    EditorUtility.DisplayProgressBar(
                        BakeTerrainTitleLabel, progressDescription, (float)count / terrainMarkers.Count
                    );

                    DoBake(terrainMarker, true, BakeTerrainTitleLabel, progressDescription);
                    ++count;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void ClearBakedAll()
        {
            EditorUtility.DisplayProgressBar(ClearBakedDataTitleLabel, GettingAllTerrainMarkerLabel, 0F);

            var terrainMarkers = GetTerrainMarkers();
            var count = 0;
            try
            {
                foreach (var terrainMarker in terrainMarkers)
                {
                    var progressDescription = string.Format(ProgressLabel, count, terrainMarkers.Count);
                    EditorUtility.DisplayProgressBar(
                        ClearBakedDataTitleLabel, progressDescription, (float)count / terrainMarkers.Count
                    );

                    ClearBakedData(terrainMarker);
                    ++count;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static List<TerrainMarker> GetTerrainMarkers()
        {
            var scene = SceneManager.GetActiveScene();
            var rootGos = scene.GetRootGameObjects();
            var terrainMarkers = new List<TerrainMarker>();
            foreach (var go in rootGos)
                terrainMarkers.AddRange(go.GetComponentsInChildren<TerrainMarker>());
            return terrainMarkers;
        }

        public static void ClearBakedData(TerrainMarker marker)
        {
            marker.terrainDataSignificantLayer = Array.Empty<int>();

            var so = new SerializedObject(marker);

            so.FindProperty(nameof(TerrainMarker.terrainDataHeight)).intValue = 0;
            so.FindProperty(nameof(TerrainMarker.terrainDataWidth)).intValue = 0;
            so.FindProperty(nameof(TerrainMarker.terrainDataSize)).vector3Value = Vector3.zero;
            so.FindProperty(nameof(TerrainMarker.terrainDataSignificantLayer)).arraySize = 0;
            so.ApplyModifiedProperties();
        }

        public static bool HasBakedData(TerrainMarker marker)
        {
            return marker.terrainDataSignificantLayer != null && marker.terrainDataSignificantLayer.Length != 0;
        }

        public static void DoBake(
            TerrainMarker marker,
            bool showProgressbar = false,
            string progressBarTitle = null,
            string progressBarDescription = null
        )
        {
            var terrain = marker.GetComponent<Terrain>();
            var terrainData = terrain.terrainData;
            var so = new SerializedObject(marker);

            var alphamapHeight = terrainData.alphamapHeight;
            var alphamapWidth = terrainData.alphamapWidth;
            var sourceAlphamaps = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
            var alphamapTextureCount = sourceAlphamaps.GetLength(2);
            so.FindProperty(nameof(TerrainMarker.terrainDataHeight)).intValue = alphamapHeight;
            so.FindProperty(nameof(TerrainMarker.terrainDataWidth)).intValue = alphamapWidth;
            so.FindProperty(nameof(TerrainMarker.terrainTextureCount)).intValue = alphamapTextureCount;
            so.FindProperty(nameof(TerrainMarker.terrainDataSize)).vector3Value = terrainData.size;

            var terrainDataSigLayerData = so.FindProperty(nameof(TerrainMarker.terrainDataSignificantLayer));
            terrainDataSigLayerData.ClearArray();
            so.ApplyModifiedProperties();
            var max = alphamapHeight * alphamapWidth * alphamapTextureCount;

            if (sourceAlphamaps.GetLength(0) != alphamapWidth)
                UnityEngine.Debug.LogWarning(
                    $"AlphamapWidth mismatch! {sourceAlphamaps.GetLength(0)}, {alphamapWidth}");
            if (sourceAlphamaps.GetLength(1) != alphamapHeight)
                UnityEngine.Debug.LogWarning(
                    $"AlphamapHeight mismatch! {sourceAlphamaps.GetLength(1)}, {alphamapHeight}");
            if (sourceAlphamaps.GetLength(2) != alphamapTextureCount)
                UnityEngine.Debug.LogWarning(
                    $"Texture count mismatch! {sourceAlphamaps.GetLength(2)}, {alphamapTextureCount}");

            // Cache it as 1D array for perf reasons: https://mdfarragher.medium.com/high-performance-arrays-in-c-2d55c04d37b5
            var sourceArray = new float[max];
            Buffer.BlockCopy(sourceAlphamaps, 0, sourceArray, 0, max * sizeof(float));
            var stopwatch = Stopwatch.StartNew();
            showProgressbar = showProgressbar && progressBarDescription != null;
            var array = new int[alphamapWidth * alphamapHeight];
            var processedCount = 0;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            // Scary asynchronous handling
            var counterLock = new object();
            Task.Run(() => Parallel.For(0, alphamapHeight, options, y =>
            {
                var ySrcIndexBase = y * alphamapWidth * alphamapTextureCount;
                var yDstIndexBase = y * alphamapWidth;
                for (var x = 0; x < alphamapWidth; x++)
                {
                    var srcIndexBase = ySrcIndexBase + x * alphamapTextureCount;
                    var dstIndex = yDstIndexBase + x;
                    var maxWeight = float.MinValue;
                    var maxWeightIndex = 0;
                    for (var i = 0; i < alphamapTextureCount; i++)
                    {
                        var weight = sourceArray[srcIndexBase + i];
                        if (maxWeight > weight) continue;
                        maxWeight = weight;
                        maxWeightIndex = i;
                    }

                    array[dstIndex] = maxWeightIndex;
                    lock (counterLock)
                        ++processedCount;
                }
            }));

            // Wait until asynchronously running process are completed
            // Hoping this count wont mess up at some weird situation
            while (processedCount < alphamapWidth * alphamapHeight)
            {
                // Show progressbar if wanted
                if (showProgressbar)
                {
                    var currentProgress = (float)processedCount / (alphamapWidth * alphamapHeight);

                    if (EditorUtility.DisplayCancelableProgressBar(
                            progressBarTitle +
                            $" {currentProgress * 100:F1}% ({stopwatch.Elapsed:hh':'mm':'ss}) ({processedCount}/{alphamapWidth * alphamapHeight})",
                            progressBarDescription,
                            currentProgress
                        ))
                        throw new OperationCanceledException();
                }

                Task.Delay(1000);
            }

            UnityEngine.Debug.Log($"Time took to bake: {stopwatch.Elapsed:hh':'mm':'ss}");

            marker.terrainDataSignificantLayer = array;
            so.Update();
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(marker);
        }
    }
}