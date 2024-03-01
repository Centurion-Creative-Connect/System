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

        private void OnSceneGUI()
        {
            if (!_usePreview || _shootingRef == null) return;

            serializedObject.ApplyModifiedProperties();
            var data = (GunBulletDataStore)target;
            if (_useRangePreview) DrawRange(_shootingRef, Vector3.zero, Quaternion.identity);
            for (int i = (int)_offsetMin; i <= (int)_offsetMax; i++)
            {
                DrawHandles(_shootingRef, Vector3.zero, Quaternion.identity, data, i);
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
            var l2w = parent.localToWorldMatrix;
            var pos = l2w.MultiplyPoint3x4(offsetPos);
            var rot = Quaternion.AngleAxis(Quaternion.Angle(Quaternion.Euler(Vector3.up), offsetRot * l2w.rotation),
                Vector3.up);
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
    }
}