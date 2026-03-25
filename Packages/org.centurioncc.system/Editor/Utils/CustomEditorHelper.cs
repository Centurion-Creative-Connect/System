using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CenturionCC.System.Editor.Utils
{
    public static class CustomEditorHelper
    {
        private static readonly Dictionary<Type, Type> CustomEditorTypeCache = new Dictionary<Type, Type>();

        public static UnityEditor.Editor CreateCustomEditor(Object target) => CreateCustomEditor(target, target.GetType());

        public static UnityEditor.Editor CreateCustomEditor<T>(Object target) => CreateCustomEditor(target, typeof(T));

        public static UnityEditor.Editor CreateCustomEditor(Object target, Type type)
        {
            if (CustomEditorTypeCache.TryGetValue(type, out var cachedCustomEditorType)) return UnityEditor.Editor.CreateEditor(target, cachedCustomEditorType);

            var customEditor = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(UnityEditor.Editor)))
                .Where(t => t.GetCustomAttributes<CustomEditor>().Where(attr => attr.IsCustomEditorFor(type)).Any())
                .FirstOrDefault();

            CustomEditorTypeCache[type] = customEditor;
            return UnityEditor.Editor.CreateEditor(target, customEditor);
        }

        public static bool IsCustomEditorFor<T>(this CustomEditor attribute) => attribute.IsCustomEditorFor(typeof(T));

        public static bool IsCustomEditorFor(this CustomEditor attribute, Type type)
        {
            var inspectedTypeField = typeof(CustomEditor).GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inspectedTypeField == null)
            {
                Debug.LogError("Failed to find m_InspectedType field");
                return false;
            }

            if (inspectedTypeField.GetValue(attribute) is not Type targetType)
            {
                Debug.LogError("Failed to cast value of m_InspectedType field");
                return false;
            }

            return targetType.IsAssignableFrom(type);
        }
    }
}
