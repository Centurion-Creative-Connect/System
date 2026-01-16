using CenturionCC.System.Editor.EditorInspector.Gun.DataStore;
using CenturionCC.System.Gun.DataStore;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorWindow
{
    public class GunUtilitiesWindow : UnityEditor.EditorWindow
    {
        private GunBulletDataStore _dataStore;
        private Transform _shootingRef;
        private bool _showPreview;
        private bool _showRange;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnDrawPreviewGizmos;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnDrawPreviewGizmos;
        }

        private void OnGUI()
        {
            _shootingRef =
                EditorGUILayout.ObjectField("Shooting Ref", _shootingRef, typeof(Transform), true) as Transform;
            _dataStore =
                EditorGUILayout.ObjectField("Data Store", _dataStore, typeof(GunBulletDataStore), true) as
                    GunBulletDataStore;

            _showRange = EditorGUILayout.Toggle("Show Range", _showRange);
            _showPreview = EditorGUILayout.Toggle("Show Preview", _showPreview);
        }

        [MenuItem("Centurion System/Utils/Gun Utilities")]
        public static void InitMenu()
        {
            GunUtilitiesWindow window = GetWindow<GunUtilitiesWindow>();
            window.titleContent.text = "Gun Utilities";
            window.Show();
        }

        private void OnDrawPreviewGizmos(SceneView sceneView)
        {
            if (_shootingRef != null && _showPreview)
            {
                GunBulletDataStoreEditor.DrawRange(_shootingRef, Vector3.zero, Quaternion.identity);
            }

            if (_shootingRef != null && _dataStore != null && _showPreview)
            {
                GunBulletDataStoreEditor.DrawHandles(_shootingRef, Vector3.zero, Quaternion.identity, _dataStore);
            }
        }
    }
}
