using CenturionCC.System.Gun.Behaviour;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.EditorInspector.Gun.Behaviour
{
    [CustomEditor(typeof(CockingGunBehaviour))]
    public class CockingGunBehaviourEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var cockingPositionProperty = serializedObject.FindProperty("cockingPosition");
            var t = cockingPositionProperty.objectReferenceValue as Transform;
            if (t == null) return;

            EditorGUI.BeginChangeCheck();

            var pos = t.position;
            var rot = t.rotation;

            Handles.TransformHandle(ref pos, ref rot);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Move Cocking Position");
                t.position = pos;
                t.rotation = rot;
            }
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            DrawDefaultInspector();
        }
    }
}
