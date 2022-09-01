using CenturionCC.System.Editor.Utils;
using UdonSharpEditor;
using UnityEditor;

namespace CenturionCC.System.Editor
{
    public abstract class ShooterEditor : UnityEditor.Editor
    {
        private static bool _infoFoldout = true;
        private static bool _rawPropsFoldout;
        private static bool _utilsFoldout = true;

        //TODO: add default raw value drawer
        public sealed override void OnInspectorGUI()
        {
            // Draws the default convert to UdonBehaviour button, program asset field, sync settings, etc.
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            GUIUtil.HorizontalBar();

            if (GUIUtil.HeaderFoldout(GUIUtil.InfoHeaderString, ref _infoFoldout))
                using (new EditorGUI.IndentLevelScope())
                    GUIUtil.DrawOrLabel(DrawInfo());

            GUIUtil.HorizontalBar();

            if (GUIUtil.HeaderFoldout(GUIUtil.RawPropertyHeaderString, ref _rawPropsFoldout))
                using (new EditorGUI.IndentLevelScope())
                    DrawDefaultInspector();

            GUIUtil.HorizontalBar();

            if (GUIUtil.HeaderFoldout(GUIUtil.UtilHeaderString, ref _utilsFoldout))
                using (new EditorGUI.IndentLevelScope())
                    GUIUtil.DrawOrLabel(DrawUtils());

            GUIUtil.HorizontalBar();

            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Draws Information of Component status.
        /// </summary>
        /// <returns>If something was drawn, returns true. false otherwise.</returns>
        protected abstract bool DrawInfo();

        /// <summary>
        /// Draws Utility buttons for Component.
        /// </summary>
        /// <returns>If something was drawn, returns true. false otherwise.</returns>
        protected abstract bool DrawUtils();
        
        public void DrawDefaultInspector(SerializedObject obj)
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    EditorGUILayout.PropertyField(iterator, true);
            }
        }
    }
}