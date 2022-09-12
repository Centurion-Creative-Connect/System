using CenturionCC.System.Audio;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorWindow
{
    public class CqbMapImporter : ShooterEditorWindow
    {
        private GameObject _mapRootObj;

        [MenuItem("Centurion-Utils/CQB Map Importer")]
        public static void Init()
        {
            ShowWindow<CqbMapImporter>();
        }

        protected override bool DrawInfo()
        {
            return true;
        }

        protected override bool DrawProperty()
        {
            _mapRootObj =
                (GameObject)EditorGUILayout.ObjectField("Map Root", _mapRootObj, typeof(GameObject), true);
            return true;
        }

        protected override void OnApplyButton()
        {
            var meshes = _mapRootObj.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshes)
            {
                var go = meshRenderer.gameObject;
                foreach (var meshCol in go.GetComponents<MeshCollider>())
                    DestroyImmediate(meshCol);
                go.AddComponent<MeshCollider>();
                foreach (var marker in go.GetComponentsInChildren<FootstepMarker>())
                    UdonSharpEditorUtility.DestroyImmediate(marker);

                var fm = go.AddUdonSharpComponent<FootstepMarker>();
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                fm.Internal_SetFootstepType(FootstepType.Wood);
#endif
            }
        }

        protected override bool CanApply()
        {
            return _mapRootObj != null;
        }
    }
}