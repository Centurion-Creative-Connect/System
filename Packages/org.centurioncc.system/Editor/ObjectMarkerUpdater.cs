using System.Collections.Generic;
using CenturionCC.System.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CenturionCC.System.Editor
{
    public static class ObjectMarkerUpdater
    {
#if UNITY_EDITOR
        [MenuItem("Centurion-Utils/Update Object Marker")]
        public static void UpdateObjectMarker()
        {
            var pc = CenturionSystemReference.GetPlayerController();

            Debug.Log(
                $"[UpdateObjectMarker] Target PlayerController: {(pc != null ? pc.transform.GetHierarchyPath() : "Null")}");

            var objectMarkers = new List<ObjectMarker>();
            foreach (var o in SceneManager.GetActiveScene().GetRootGameObjects())
                objectMarkers.AddRange(o.GetComponentsInChildren<ObjectMarker>());

            foreach (var objectMarker in objectMarkers)
            {
                var so = new SerializedObject(objectMarker);
                so.FindProperty("controller").objectReferenceValue = pc;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.DisplayDialog(
                "Update Object Marker",
                $"Updated {objectMarkers.Count} object markers with player controller {(pc != null ? pc.transform.GetHierarchyPath() : "Null")}.",
                "OK!"
            );
        }
#endif
    }
}