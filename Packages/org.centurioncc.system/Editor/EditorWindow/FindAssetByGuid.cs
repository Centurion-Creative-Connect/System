using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorWindow
{
    public class FindAssetByGuid : ShooterEditorWindow
    {
        private string _guid;
        private string _path;

        [MenuItem("Centurion System/Utils/FindAssetByGuid")]
        public static void Open()
        {
            ShowWindow<FindAssetByGuid>();
        }

        protected override bool DrawInfo()
        {
            return false;
        }

        protected override bool DrawProperty()
        {
            GUILayout.Label("Path");
            _path = GUILayout.TextField(_path);
            GUILayout.Label("Guid");
            _guid = GUILayout.TextField(_guid);
            return true;
        }

        protected override void OnApplyButton()
        {
            _path = AssetDatabase.GUIDToAssetPath(_guid);
        }

        protected override bool CanApply()
        {
            return true;
        }
    }
}
