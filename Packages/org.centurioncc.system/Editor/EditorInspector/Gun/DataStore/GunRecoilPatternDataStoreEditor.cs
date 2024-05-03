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
        private SerializedProperty _positionOffsetPatternsProperty;

        private SerializedProperty _recoilOffsetPatternsProperty;
        private bool _showMinArraySizeHelpBox;
        private SerializedProperty _speedOffsetPatternsProperty;

        private void OnEnable()
        {
            _maxPatterns = ((GunRecoilPatternDataStore)target).MaxPatterns;

            _recoilOffsetPatternsProperty = serializedObject.FindProperty("recoilOffsetPatterns");
            _positionOffsetPatternsProperty = serializedObject.FindProperty("positionOffsetPatterns");
            _speedOffsetPatternsProperty = serializedObject.FindProperty("speedOffsetPatterns");

            CheckArraysSize();
        }

        private void OnDisable()
        {
            CheckArraysSize();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            if (_showMinArraySizeHelpBox)
                EditorGUILayout.HelpBox(
                    "Each patterns require an element for work. At least 1 element will be added automatically.",
                    MessageType.Warning
                );

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

            CheckArraysSize();
        }

        private void CheckArraysSize()
        {
            if (_recoilOffsetPatternsProperty.arraySize <= 0)
            {
                _recoilOffsetPatternsProperty.arraySize = 1;
                ApplyAndUpdate();
            }

            if (_positionOffsetPatternsProperty.arraySize <= 0)
            {
                _positionOffsetPatternsProperty.arraySize = 1;
                ApplyAndUpdate();
            }

            if (_speedOffsetPatternsProperty.arraySize <= 0)
            {
                _speedOffsetPatternsProperty.arraySize = 1;
                ApplyAndUpdate();
            }

            return;

            void ApplyAndUpdate()
            {
                serializedObject.ApplyModifiedProperties();

                _showMinArraySizeHelpBox = true;
                _maxPatterns = ((GunRecoilPatternDataStore)target).MaxPatterns;
            }
        }
    }
}