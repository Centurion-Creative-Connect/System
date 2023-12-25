using CenturionCC.System.Editor.Utils;
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
        private static Transform _shootingRef;
        private static float _offsetMin = 1F;
        private static float _offsetMax = 5F;

        private void OnSceneGUI()
        {
            if (!_usePreview || _shootingRef == null) return;

            var data = (GunBulletDataStore)target;

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
                _shootingRef =
                    (Transform)EditorGUILayout.ObjectField("Shooting Ref", _shootingRef, typeof(Transform), true);
                EditorGUILayout.MinMaxSlider("Bullet Count", ref _offsetMin, ref _offsetMax, 1F, 10F);
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

            Handles.color = Color.red;
            Handles.DrawDottedLine(pos, pos + (rot * Vector3.forward * 2), 10);
            // var line = GunBullet.PredictTrajectory(pos, rot, data, offset, 100, 0.02F);
            // Handles.DrawPolyLine(line);
        }
    }
}