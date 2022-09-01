using CenturionCC.System.Editor.Utils;
using UnityEditor;

namespace CenturionCC.System.Editor
{
    public abstract class ShooterEditorWindow : UnityEditor.EditorWindow
    {

        public static T ShowWindow<T>() where T : UnityEditor.EditorWindow
        {
            T window = GetWindow<T>();
            window.Show();
            return window;
        }
        
        public void OnGUI()
        {
            GUIUtil.HorizontalBar();

            GUIUtil.DrawOrLabel(DrawInfo());

            GUIUtil.HorizontalBar();

            GUIUtil.DrawOrLabel(DrawProperty());

            GUIUtil.HorizontalBar();

            using (new EditorGUI.DisabledScope(!CanApply()))
            {
                GUIUtil.SmallButton(GUIUtil.AutoFixString, out bool buttonResult);
                if (buttonResult)
                    OnApplyButton();
            }

            GUIUtil.HorizontalBar();
        }

        /// <summary>
        /// Draws Information of custom property for util.
        /// </summary>
        /// <returns>If something was drawn, returns true. false otherwise.</returns>
        protected abstract bool DrawInfo();

        /// <summary>
        /// Draws custom property for util.
        /// </summary>
        /// <returns>If something was drawn, returns true. false otherwise.</returns>
        protected abstract bool DrawProperty();

        /// <summary>
        /// Calls on apply button has pressed.
        /// </summary>
        protected abstract void OnApplyButton();

        /// <summary>
        /// Returns availability of Apply button
        /// </summary>
        /// <returns>true if property is correctly set. false otherwise.</returns>
        protected abstract bool CanApply();
    }
}