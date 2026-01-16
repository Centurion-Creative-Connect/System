using CenturionCC.System.Gun.DataStore;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.EditorWindow
{
    // ReSharper disable once InconsistentNaming
    public class BBBulletTrailDrawer : UnityEditor.EditorWindow
    {

        private List<List<Vector3>> _cachedLines;
        private int _pointsOfLine = 100;
        private int _projectileCount = 1;
        private ProjectileDataProvider _projectileData;
        private int _projectileDataHash;
        private int _projectileOffset;
        private GameObject _shootingRef;

        private int _shootingRefHash;

        private void OnEnable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnGUI()
        {
            _shootingRef = (GameObject)EditorGUILayout.ObjectField(_shootingRef, typeof(GameObject), true);
            _projectileData =
                (ProjectileDataProvider)EditorGUILayout.ObjectField(_projectileData, typeof(ProjectileDataProvider),
                    true);
            _projectileOffset = EditorGUILayout.IntField("Projectile Offset", _projectileOffset);
            _projectileCount = EditorGUILayout.IntField("Projectile Count", _projectileCount);
            _pointsOfLine = EditorGUILayout.IntField("Points Of Line", _pointsOfLine);

            var isUpdated = false;
            if (_shootingRef != null && _shootingRefHash != _shootingRef.GetHashCode() ||
                _projectileData != null && _projectileDataHash != _projectileData.GetHashCode())
            {
                if (_shootingRef != null)
                    _shootingRefHash = _shootingRef.GetHashCode();
                if (_projectileData != null)
                    _projectileDataHash = _projectileData.GetHashCode();
                isUpdated = true;
            }

            if (isUpdated || EditorApplication.isPlaying || GUILayout.Button("Refresh"))
            {
                if (_shootingRef == null || _projectileData == null)
                    return;

                _cachedLines = new List<List<Vector3>>();
                for (int i = 0; i < _projectileCount; i++)
                {
                    // TODO: impl
                    // _cachedLines.Add(
                    //     GunBullet.PredictTrajectory(
                    //         _shootingRef.transform.position,
                    //         _shootingRef.transform.rotation,
                    //         _projectileData,
                    //         _projectileOffset + i,
                    //         _pointsOfLine
                    //     ).ToList()
                    // );
                }

                HandleUtility.Repaint();
            }
        }

        [MenuItem("Centurion System/Utils/BB Bullet Trail Drawer")]
        public static void Open()
        {
            var window = GetWindow<BBBulletTrailDrawer>();
            window.titleContent.text = "BB Bullet Trail Drawer";
            window.Show();
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (_cachedLines == null || _cachedLines.Count == 0)
                return;

            foreach (var points in _cachedLines)
                DrawLines(points);
        }

        private static void DrawLines(List<Vector3> points)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                var v1 = points[i];
                var v2 = points[i + 1];
                Handles.DrawLine(v1, v2);
            }
        }
    }
}
