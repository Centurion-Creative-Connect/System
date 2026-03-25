using UnityEngine;
namespace CenturionCC.System.Editor.Utils
{
    public class CachedEditor
    {

        public CachedEditor(Object target)
        {
            Target = target;
            Editor = CustomEditorHelper.CreateCustomEditor(Target);
        }
        public UnityEditor.Editor Editor { get; private set; }
        public Object Target { get; private set; }

        public void OnInspectorGUI() => Editor.OnInspectorGUI();
        public void SetTarget(Object target)
        {
            if (target == Target) return;

            if (Editor) Object.DestroyImmediate(Editor);

            Target = target;
            Editor = CustomEditorHelper.CreateCustomEditor(Target);
        }
    }
}
