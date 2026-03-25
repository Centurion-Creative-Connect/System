using System;
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
