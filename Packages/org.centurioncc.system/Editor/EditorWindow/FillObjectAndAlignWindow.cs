using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorWindow
{
    public class FillObjectAndAlignWindow : ShooterEditorWindow
    {
        private static GameObject _sourceObject;
        private static Transform _parentObject;
        private static int _amount;
        private static float _offset;
        private static Axis _axis;

        [MenuItem("Centurion-Utils/Fill and Align")]
        public static void Init()
        {
            ShowWindow<FillObjectAndAlignWindow>();
        }

        protected override bool DrawInfo()
        {
            return false;
        }

        protected override bool DrawProperty()
        {
            GUIUtil.ObjectField("Source Object", ref _sourceObject);
            GUIUtil.ObjectField("Parent Object", ref _parentObject);
            GUIUtil.IntField("Amount", ref _amount);
            GUIUtil.FloatField("Offset", ref _offset);
            GUIUtil.EnumField("Axis", ref _axis);

            return true;
        }

        protected override void OnApplyButton()
        {
            // ShooterEditorUtil.CreateObjects();
        }

        protected override bool CanApply()
        {
            return _sourceObject != null && _parentObject != null && _amount > 0;
        }

        public enum Axis
        {
            X,
            Y,
            Z
        }
    }
}