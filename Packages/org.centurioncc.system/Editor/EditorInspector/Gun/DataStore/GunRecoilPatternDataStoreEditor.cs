using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun.DataStore;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.EditorInspector.Gun.DataStore
{
    [CustomEditor(typeof(GunRecoilPatternDataStore))]
    public class GunRecoilPatternDataStoreEditor : UnityEditor.Editor
    {
        private int _maxPatterns;

        private void OnEnable()
        {
            _maxPatterns = ((GunRecoilPatternDataStore)target).MaxPatterns;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            var so = serializedObject;
            var property = so.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                GUIUtil.FoldoutPropertyField(property, 2);
            }

            if (so.ApplyModifiedProperties())
            {
                _maxPatterns = ((GunRecoilPatternDataStore)target).MaxPatterns;
            }

            GUILayout.Label("Stats", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.IntField("Max Patterns", _maxPatterns);
                EditorGUILayout.HelpBox("Max Patterns does not consider duplicated elements", MessageType.Warning);
            }
        }
    }
}