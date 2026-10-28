using System;
using UnityEditor;
using Object = UnityEngine.Object;
namespace CenturionCC.System.Editor.Utils
{
    public class CachedEditor : IDisposable
    {
        public CachedEditor(Object target)
        {
            Target = target;
            Editor = CustomEditorHelper.CreateCustomEditor(Target);
        }

        public UnityEditor.Editor Editor { get; private set; }
        public Object Target { get; private set; }

        public void Dispose()
        {
            Object.DestroyImmediate(Editor);
            Editor = null;
            Target = null;
        }

        /// <summary>
        /// Draws inspector without UdonSharp header.
        /// </summary>
        public void OnInspectorGUI()
        {
            var wasDrawingEmbedded = GUIUtil.IsDrawingEmbedded;
            GUIUtil.IsDrawingEmbedded = true;
            try
            {
                if (CustomEditorHelper.HasCustomEditor(Target)) Editor.OnInspectorGUI();
                else DrawProperties(Editor.serializedObject);
            }
            finally
            {
                GUIUtil.IsDrawingEmbedded = wasDrawingEmbedded;
            }
        }

        /// <summary>
        /// Same as UdonSharp default inspector, without header.
        /// </summary>
        private static void DrawProperties(SerializedObject so)
        {
            so.Update();

            var property = so.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.propertyPath == "m_Script") continue;
                    EditorGUILayout.PropertyField(property, true);
                } while (property.NextVisible(false));
            }

            so.ApplyModifiedProperties();
        }

        public void SetTarget(Object target)
        {
            if (target == Target) return;

            if (Editor) Object.DestroyImmediate(Editor);

            Target = target;
            Editor = CustomEditorHelper.CreateCustomEditor(Target);
        }
    }
}
