using System;
using System.Linq;
using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun;
using CenturionCC.System.Gun.DataStore;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorInspector.Gun.DataStore
{
    [CustomEditor(typeof(GunBulletDataStore))]
    public class GunBulletDataStoreEditor : UnityEditor.Editor
    {
        private static bool _usePreview;
        private static bool _useRangePreview = true;
        private static Transform _shootingRef;
        private static float _offsetMin = 1F;
        private static float _offsetMax = 5F;
        private static int _simPoints = 200;
        private float _cachedAvgDistance = 0F;
        private float _cachedAvgHighestPoint = 0F;
        private int _cachedMaxPatterns = 0;

        private bool _hasCache = false;

        private void OnSceneGUI()
        {
            var shootingRef = _shootingRef;
            if (!_usePreview) return;

            serializedObject.ApplyModifiedProperties();
            var data = target as GunBulletDataStore;
            if (data == null) return;
            if (shootingRef == null) shootingRef = data.transform;

            if (_useRangePreview) DrawRange(shootingRef, Vector3.zero, Quaternion.identity);
            for (int i = (int)_offsetMin; i <= (int)_offsetMax; i++)
            {
                DrawHandles(shootingRef, Vector3.zero, Quaternion.identity, data, i);
            }
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            var so = serializedObject;
            var property = so.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                if (property.name == "m_Script") continue;
                GUIUtil.FoldoutPropertyField(property, 2);
            }

            so.ApplyModifiedProperties();

            GUILayout.Label("Preview", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                _usePreview = EditorGUILayout.Toggle("Use Preview", _usePreview);
                if (_usePreview)
                    EditorGUILayout.HelpBox("Preview trajectory is just an approximation, Do not expect accuracy!",
                        MessageType.Warning);

                using (new EditorGUI.DisabledGroupScope(!_usePreview))
                {
                    _useRangePreview = EditorGUILayout.Toggle("Use Range Preview", _useRangePreview);
                    _shootingRef =
                        (Transform)EditorGUILayout.ObjectField("Shooting Ref", _shootingRef, typeof(Transform), true);
                    EditorGUILayout.MinMaxSlider("Bullet Count", ref _offsetMin, ref _offsetMax, 1F, 10F);
                    _simPoints = EditorGUILayout.IntField("Sim Points", _simPoints);
                }

                if (_usePreview && _shootingRef != null)
                    GUILayout.Label($"Previewing pattern {(int)_offsetMin} to {(int)_offsetMax}");

                if (GUILayout.Button("Calculate Stats"))
                {
                    var data = target as GunBulletDataStore;
                    if (data == null) throw new ArgumentNullException(nameof(data));
                    var recoils = data.RecoilPattern;
                    var maxPatterns = recoils.MaxPatterns;
                    var dists = new float[maxPatterns];
                    var highests = new float[maxPatterns];
                    for (int i = 0; i < maxPatterns; i++)
                    {
                        GetPredictedStats(data, i, out var dist, out var highest);
                        dists[i] = dist;
                        highests[i] = highest;
                    }

                    _cachedMaxPatterns = maxPatterns;
                    _cachedAvgDistance = dists.Average();
                    _cachedAvgHighestPoint = highests.Average();
                    _hasCache = true;
                }

                if (_hasCache)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.HelpBox("This is just an approximation. Expect around +-5m error.",
                            MessageType.Warning);
                        EditorGUILayout.IntField("Patterns", _cachedMaxPatterns);
                        EditorGUILayout.FloatField("Avg Distance", _cachedAvgDistance);
                        EditorGUILayout.FloatField("Avg Highest", _cachedAvgHighestPoint);
                    }
                }
            }
        }

        public static void DrawHandles(Transform parent, Vector3 offsetPos, Quaternion offsetRot,
            GunBulletDataStore data, int offset = 0)
        {
            var l2w = parent.localToWorldMatrix;
            var pos = l2w.MultiplyPoint3x4(offsetPos);
            var rot = offsetRot * l2w.rotation;

            var line = GunBullet.PredictTrajectory(pos, rot, data, offset, _simPoints);
            var highestPoint = Vector3.negativeInfinity;
            var zeroedInPoint = Vector3.negativeInfinity;
            for (int i = 1; i < line.Length; i++)
            {
                var p = line[i];
                if (p.y >= highestPoint.y) highestPoint = p;
                if (p.y <= pos.y && line[i - 1].y >= pos.y) zeroedInPoint = p;
            }

            Handles.color = Color.red;
            Handles.DrawPolyLine(line);

            var left = rot * Vector3.left;
            var highestPointZeroed = new Vector3(highestPoint.x, pos.y, highestPoint.z);
            Handles.color = Color.yellow;
            Handles.DrawLine(highestPoint, highestPoint + left);
            Handles.Label(highestPoint + left,
                $"Highest: {highestPoint.y - pos.y:F2}m ({(highestPointZeroed - pos).magnitude:F2}m)");

            Handles.color = Color.green;
            Handles.DrawLine(zeroedInPoint, zeroedInPoint + left);
            Handles.Label(zeroedInPoint + left, $"Zero-in: {(zeroedInPoint - pos).magnitude:F2}m");
        }

        public static void DrawRange(Transform parent, Vector3 offsetPos, Quaternion offsetRot, int length = 75,
            int part = 5)
        {
            var parentRot = parent.rotation;
            var rot = new Quaternion(0, parentRot.y, 0, parentRot.w).normalized * offsetRot;
            var pos = parent.position + offsetRot * offsetPos;
            var forward = rot * Vector3.forward;
            var left = rot * Vector3.left;

            Handles.color = Color.gray;

            Handles.DrawLine(pos, pos + forward * length);

            Handles.color = Color.white;
            for (int i = 0; i < length; i += part)
            {
                var point = pos + forward * i;
                Handles.DrawLine(point, point + left);
                Handles.Label(point + left, $"{i}m");
            }
        }

        public static void GetPredictedStats(GunBulletDataStore data, int offset, out float distance, out float highest)
        {
            var line = GunBullet.PredictTrajectory(Vector3.zero, Quaternion.identity, data, offset, _simPoints);
            highest = float.NegativeInfinity;
            distance = float.NegativeInfinity;
            for (int i = 1; i < line.Length; i++)
            {
                var p = line[i];
                if (p.y >= highest) highest = p.y;
                if (p.y <= 0 && line[i - 1].y >= 0) distance = p.magnitude;
            }
        }
    }
}