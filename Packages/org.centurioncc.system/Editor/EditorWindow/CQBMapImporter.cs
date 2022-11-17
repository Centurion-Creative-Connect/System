using CenturionCC.System.Audio;
using CenturionCC.System.Utils;
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
            var environmentLayer = LayerMask.NameToLayer("Environment");
            var meshes = _mapRootObj.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshes)
            {
                var go = meshRenderer.gameObject;
                foreach (var meshCol in go.GetComponents<MeshCollider>())
                    DestroyImmediate(meshCol);
                go.AddComponent<MeshCollider>();
                foreach (var footstepMarker in go.GetComponentsInChildren<FootstepMarker>())
                    UdonSharpEditorUtility.DestroyImmediate(footstepMarker);
                foreach (var objectMarker in go.GetComponentsInChildren<ObjectMarker>())
                    UdonSharpEditorUtility.DestroyImmediate(objectMarker);

                var om = go.AddUdonSharpComponent<ObjectMarker>();
#if UNITY_EDITOR && !COMPILER_UDONSHARP
                om.EditorOnly_SetObjectType(ObjectType.Wood);
#endif
                go.layer = environmentLayer;
            }

            EditorUtility.DisplayDialog("CQBMapImport", "CQB Map was successfully converted", "OK!");
        }

        protected override bool CanApply()
        {
            return _mapRootObj != null;
        }
    }
}