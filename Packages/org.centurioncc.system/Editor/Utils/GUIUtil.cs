using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CenturionCC.System.Editor.Utils
{
    internal static class GUIUtil
    {
        public enum Alignment
        {
            None,
            Left,
            Right
        }

        private static Dictionary<string, bool> _expandMap = new Dictionary<string, bool>();
        public static Color SurfaceIndentColor = Color.black;
        public static Color DeepIndentColor = Color.red;

        public static float HelpBoxWidth = 200;
        public static float HelpBoxHeight = 40;
        public static float HelpBoxButtonWidth = 100;

        public static string NotAssignedStringFormat =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "{0} が指定されていません"
                : "{0} is null";
        public static string ApplyStringFormat =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "{0} を適用する"
                : "Apply {0}";
        public static string NothingHereString =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "だいじょうぶっぽい!"
                : "oh well... nothing to see here!";
        public static string InfoHeaderString =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "情報"
                : "Info";
        public static string RawPropertyHeaderString =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "生のデータ"
                : "Raw Value";
        public static string UtilHeaderString =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "ユーティリティ"
                : "Utilities";
        public static string AmountString =>
            Application.systemLanguage == SystemLanguage.Japanese
                ? "量"
                : "Amount";
        public static string AutoFixString => "Auto Fix";

        public static string BoldStringFormat => "<b>{0}</b>";

        public static string ColoredStringFormat => "<color=#{0}>{1}</color>";

        public static Color DepthColor() =>
            Color.Lerp(SurfaceIndentColor, DeepIndentColor, EditorGUI.indentLevel / 5.0F);

        public static string ToDepthColor(string text) =>
            string.Format(ColoredStringFormat, ColorUtility.ToHtmlStringRGB(DepthColor()), text);

        public static string ToBold(string text) =>
            string.Format(BoldStringFormat, text);

        public static void DrawOrLabel(bool drawn) => DrawOrLabel(drawn, NothingHereString);

        public static void DrawOrLabel(bool drawn, string label)
        {
            if (!drawn)
                HelpBox(label, MessageType.Info);
        }

        public static void HelpBox(string text, MessageType msgType)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.MinWidth(HelpBoxWidth), GUILayout.MaxWidth(1000),
                GUILayout.Height(HelpBoxHeight), GUILayout.ExpandWidth(true));
            EditorGUI.HelpBox(rect, text, msgType);
        }

        public static GUIStyle GetButtonStyleWithAnchor(TextAnchor anchor)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = anchor };
            return style;
        }

        public readonly struct IconType
        {
            public static readonly IconType Info = new IconType("console.infoicon");
            public static readonly IconType Warn = new IconType("console.warnicon");
            public static readonly IconType Error = new IconType("console.erroricon");

            public readonly string Name;

            private IconType(string name)
            {
                this.Name = name;
            }
        }

        public struct StyleSet
        {
            public static readonly GUIStyle ButtonStyle = GetButtonStyleWithAnchor(TextAnchor.MiddleLeft);
        }

        #region Labels

        public static void HorizontalBar() =>
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        public static void Label(string text, bool depthColor = true) => Impl_Label(text, depthColor);

        public static void Header(string text, bool depthColor = true) => Impl_Label(ToBold(text), depthColor);

        #endregion

        #region Foldouts

        public static bool Foldout(string text, ref bool result) => Impl_Foldout(text, ref result);
        public static bool HeaderFoldout(string text, ref bool result) => Impl_Foldout(ToBold(text), ref result);

        #endregion

        #region Buttons

        public static bool SmallButton(string label) =>
            GUILayout.Button(label, GUILayout.Width(HelpBoxButtonWidth), GUILayout.ExpandHeight(true));

        public static bool SmallButton(string label, out bool result) => result = SmallButton(label);
        public static void Button(string label, out bool result) => Impl_Button(label, out result, IconType.Info);

        public static void Button(string label, out bool result, IconType iconType) =>
            Impl_Button(label, out result, iconType);

        #endregion

        #region HelpBoxWithButton

        public static bool HelpBoxWithButton(string text, MessageType msgType, string buttonLabel = "Auto Fix")
        {
            bool buttonResult;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                HelpBox(text, msgType);
                using (new EditorGUILayout.VerticalScope())
                {
                    buttonResult = SmallButton(buttonLabel);
                }

                GUILayout.FlexibleSpace();
            }

            return buttonResult;
        }

        public static bool HelpBoxWithButton<T>(string text, MessageType msgType, ref T result,
            string buttonLabel = "Auto Fix")
            where T : UnityEngine.Object
        {
            bool buttonResult;

            using (new EditorGUILayout.HorizontalScope())
            {
                HelpBox(text, msgType);
                using (new EditorGUILayout.VerticalScope())
                {
                    buttonResult = SmallButton(buttonLabel);
                    result = (T)EditorGUILayout.ObjectField(result, typeof(T), true,
                        GUILayout.Width(HelpBoxButtonWidth));
                }
            }

            return buttonResult;
        }

        #endregion

        #region Fields

        public static void ObjectField<T>(string label, ref T value, bool allowSceneObjects = true,
            bool labelDepthColor = false, Alignment alignment = Alignment.None, float labelWidth = 30F)
            where T : UnityEngine.Object
        {
            T FieldFunc(T v)
            {
                EditorGUIUtility.labelWidth = labelWidth;
                Label(label, labelDepthColor);
                return (T)EditorGUILayout.ObjectField(v, typeof(T), allowSceneObjects,
                    GUILayout.Width(HelpBoxButtonWidth));
            }

            FieldBase(FieldFunc, ref value, alignment);
        }

        public static void IntField(string label, ref int value, bool labelDepthColor = false, float labelWidth = 30F,
            Alignment alignment = Alignment.None)
        {
            int FieldFunc(int v)
            {
                EditorGUIUtility.labelWidth = labelWidth;
                //EditorGUILayout.PrefixLabel(label);
                Label(label, labelDepthColor);
                return EditorGUILayout.IntField(v, GUILayout.Width(HelpBoxButtonWidth - 50F));
            }

            FieldBase(FieldFunc, ref value, alignment);
        }

        public static void FloatField(string label, ref float value, bool labelDepthColor = false,
            float labelWidth = 30F, Alignment alignment = Alignment.None)
        {
            float FieldFunc(float v)
            {
                EditorGUIUtility.labelWidth = labelWidth;
                Label(label, labelDepthColor);
                return EditorGUILayout.FloatField(v, GUILayout.Width(HelpBoxButtonWidth - 50F));
            }

            FieldBase(FieldFunc, ref value, alignment);
        }

        public static void EnumField<T>(string label, ref T value, bool labelDepthColor = false, float labelWidth = 30F,
            Alignment alignment = Alignment.None) where T : Enum
        {
            T FieldFunc(T v)
            {
                EditorGUIUtility.labelWidth = labelWidth;
                Label(label, labelDepthColor);
                return (T)EditorGUILayout.EnumPopup(v);
            }

            FieldBase(FieldFunc, ref value, alignment);
        }

        public static bool FoldoutPropertyField(SerializedProperty property, int depth = 1)
        {
            var key = property.propertyPath;
            _expandMap.TryGetValue(key, out var isExpanded);
            return _expandMap[key] = FoldoutPropertyField(property, isExpanded, depth);
        }

        public static bool FoldoutPropertyField(SerializedProperty property, bool foldedOut, int depth = 1)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(5, false);
                EditorGUILayout.PropertyField(property, true);
                if (property.propertyType == SerializedPropertyType.ObjectReference && depth != 0)
                {
                    var lastRect = GUILayoutUtility.GetLastRect();
                    // I have no idea why this weird offsets work, but it works so...
                    var rect = new Rect(lastRect.x - 5, lastRect.y + lastRect.height - 15, 10, 10);
                    foldedOut = EditorGUI.Foldout(rect, foldedOut, "");
                }
            }

            EditorGUILayout.Space(3);

            if (property.propertyType == SerializedPropertyType.ObjectReference && foldedOut && depth != 0 &&
                property.objectReferenceValue != null)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var so = new SerializedObject(property.objectReferenceValue);
                    var soProperty = so.GetIterator();
                    soProperty.NextVisible(true);
                    while (soProperty.NextVisible(false))
                    {
                        var key = soProperty.propertyPath;
                        _expandMap.TryGetValue(key, out var isExpanded);
                        _expandMap[key] = FoldoutPropertyField(soProperty, isExpanded, depth - 1);
                    }

                    so.ApplyModifiedProperties();
                }
            }

            return foldedOut;
        }

        private static void FieldBase<T>(Func<T, T> func, ref T obj, Alignment alignment)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (alignment == Alignment.Right)
                    GUILayout.FlexibleSpace();

                obj = func.Invoke(obj);

                if (alignment == Alignment.Left)
                    GUILayout.FlexibleSpace();
            }
        }

        #endregion

        #region IconContent

        public static GUIContent IconContent(string name, string text = "", string tooltip = "") =>
            Impl_IconContent(name, text, tooltip);

        public static GUIContent IconContent(IconType type, string text = "", string tooltip = "") =>
            Impl_IconContent(type.Name, text, tooltip);

        #endregion

        #region Impls

        private static GUIContent Impl_IconContent(string name, string text, string tooltip)
        {
            return new GUIContent
            {
                image = EditorGUIUtility.IconContent(name).image, text = text, tooltip = tooltip
            };
        }

        private static void Impl_Button(string text, out bool result, IconType iconType)
        {
            result = GUILayout.Button(IconContent(iconType, text), StyleSet.ButtonStyle);
        }

        private static bool Impl_Foldout(string text, ref bool result)
        {
            return result = EditorGUILayout.Foldout(result, ToDepthColor(text), true,
                new GUIStyle("foldout") { richText = true });
        }

        private static void Impl_Label(string text, bool depthColor = true)
        {
            EditorGUILayout.LabelField(
                depthColor ? ToDepthColor(text) : text,
                new GUIStyle("label") { richText = true });
        }

        #endregion
    }
}