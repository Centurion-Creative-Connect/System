using CenturionCC.System.Audio;
using CenturionCC.System.Editor.Utils;
using UdonSharpEditor;
using UnityEditor;

namespace CenturionCC.System.Editor.EditorInspector
{
    [CustomEditor(typeof(FootstepMarker))]
    public class FootstepMarkerEditor : UnityEditor.Editor
    {
        private SerializedProperty _footstepType;

        private FootstepType _type;

        private void OnEnable()
        {
            FootstepMarker b = target as FootstepMarker;

            _footstepType = serializedObject.FindProperty("footstepType");

            FootstepType.TryParse(_footstepType.stringValue, true, out FootstepType footstep);
            _type = footstep;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            GUIUtil.EnumField<FootstepType>("Footstep Type", ref _type);

            _footstepType.stringValue = _type.ToString();
            _footstepType.stringValue = EditorGUILayout.TextField("Raw value", _footstepType.stringValue);

            serializedObject.ApplyModifiedProperties();
        }

        public enum FootstepType
        {
            Fallback,
            Ground,
            Wood,
            NoAudio,
        }
    }
}