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
            while (property.NextVisible(false))
            {
                if (property.name == "m_Script") continue;
                GUIUtil.FoldoutPropertyField(property, 3);
            }

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
    }
}