using System;
using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun.DataStore;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturionCC.System.Editor.EditorInspector.Gun.DataStore
{
    [CustomEditor(typeof(GunVariantDataStore))]
    public class GunVariantDataStoreEditor : UnityEditor.Editor
    {
        private static readonly string[] PlayerMovementMessages =
        {
            "Will be using default values specified in PlayerController",
            "Values specified here will be directly applied.",
            "Values specified here will be multiplied by PlayerController values.",
            "Will not change movement behavior by gun's direction. Player will move freely unless they have other guns activating MovementOption."
        };

        private static readonly string[] CombatTagMessages =
        {
            "Will be using default values specified in PlayerController",
            "Values specified here will be directly applied.",
            "Values specified here will be multiplied by PlayerController values.",
            "Will not slow down when shooting unless they have other guns activating CombatTag."
        };


        public void OnSceneGUI()
        {
            DrawHandles((GunVariantDataStore)target);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var so = serializedObject;
            var property = so.GetIterator();
            property.NextVisible(true);
            int count = 0;
            while (property.NextVisible(false))
            {
                if (property.name == "m_Script") continue;
                if (count > 27) break;

                GUIUtil.FoldoutPropertyField(property, 3);
                ++count;
            }

            DrawPlayerControllerInspector(so);

            so.ApplyModifiedProperties();
        }

        public static void DrawHandles(GunVariantDataStore data)
        {
            var s = new GUIStyle
                { fontSize = 16, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
            var t = data.transform;
            var pos = t.position;
            var rot = t.rotation;
            Handles.Label(pos + Vector3.up * .3F, $"{data.WeaponName}\nId: {data.UniqueId}", s);

            var pickupColor = new Color(.5F, .5F, 1F, .2F);
            DrawWireSphere(pos + rot * data.MainHandlePositionOffset, .05F, pickupColor, "MainHandle");
            if (data.IsDoubleHanded)
                DrawWireSphere(pos + rot * data.SubHandlePositionOffset, .05F, pickupColor, "SubHandle");

            if (data.AudioData != null)
                GunAudioDataStoreEditor.DrawHandles(data.transform, data.AudioData);

            if (data.ProjectileData != null && data.ProjectileData as GunBulletDataStore != null)
                GunBulletDataStoreEditor.DrawHandles(data.transform, data.FiringPositionOffset,
                    data.FiringRotationOffset,
                    (GunBulletDataStore)data.ProjectileData);
        }

        private static void DrawWireSphere(Vector3 pos, float radius, Color color, string text, GUIStyle style = null)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, pos, Quaternion.identity, radius, EventType.Repaint);

            if (string.IsNullOrWhiteSpace(text))
                return;

            if (style == null)
                style = new GUIStyle { fontSize = 16, normal = { textColor = Color.white } };

            Handles.zTest = CompareFunction.Disabled;
            Handles.Label(pos + Vector3.up * 0.025F, text, style);
        }

        private static void DrawPlayerControllerInspector(SerializedObject so)
        {
            var movementOptionProperty = so.FindProperty("movementOption");
            DrawEnumDisabledProperties(movementOptionProperty, 3, false, i => i == 0 || i == 3, i =>
            {
                EditorGUILayout.HelpBox(
                    PlayerMovementMessages[i],
                    MessageType.Info
                );
            });

            var combatTagOptionProperty = so.FindProperty("combatTagOption");
            DrawEnumDisabledProperties(combatTagOptionProperty, 2, false, i => i == 0 || i == 3, i =>
            {
                EditorGUILayout.HelpBox(
                    CombatTagMessages[i],
                    MessageType.Info
                );
            });
        }

        private static void DrawEnumDisabledProperties(SerializedProperty property, int count, bool drawDisabled,
            Func<int, bool> disableFunc, Action<int> beforeDisabledGroup)
        {
            GUIUtil.FoldoutPropertyField(property);

            var disabled = disableFunc.Invoke(property.enumValueIndex);
            beforeDisabledGroup.Invoke(property.enumValueIndex);
            if (!drawDisabled && disabled)
            {
                EditorGUILayout.HelpBox("Disabled properties are hidden.", MessageType.None);
                return;
            }

            using (new EditorGUI.DisabledScope(disabled))
            {
                for (int i = 0; i < count; i++)
                {
                    property.NextVisible(false);
                    GUIUtil.FoldoutPropertyField(property);
                }
            }
        }
    }
}