using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Gun.DataStore;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CenturionCC.System.Editor.EditorInspector.Gun.DataStore
{
    [CustomEditor(typeof(GunAudioDataStore))]
    public class GunAudioDataStoreEditor : UnityEditor.Editor
    {
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
                GUIUtil.FoldoutPropertyField(property, 2);
            }

            so.ApplyModifiedProperties();
        }

        public static void DrawHandles(Transform parent, GunAudioDataStore data)
        {
            var pos = parent.position;
            var rot = parent.rotation;
            var o = Vector3.up * 0.25F;
            var c = new Color(1F, 0F, 0, .1f);
            const float r = 0.025F;

            if (data.Shooting != null)
                DrawWireSphere(pos + rot * data.ShootingOffset, r, c, "Shooting Audio");
            if (data.EmptyShooting != null)
                DrawWireSphere(pos + rot * data.EmptyShootingOffset, r, c, "\nEmpty Shooting Audio");
            if (data.CockingPull != null)
                DrawWireSphere(pos + rot * data.CockingPullOffset, r, c, "\n\nCocking Pull Audio");
            if (data.CockingTwist != null)
                DrawWireSphere(pos + rot * data.CockingTwistOffset, r, c, "\n\n\nCocking Twist Audio");
            if (data.CockingRelease != null)
                DrawWireSphere(pos + rot * data.CockingReleaseOffset, r, c, "\n\n\n\nCocking Release Audio");
        }

        private static void DrawWireSphere(Vector3 pos, float radius, Color color, string text, GUIStyle style = null)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, pos, Quaternion.identity, radius, EventType.Repaint);

            if (string.IsNullOrWhiteSpace(text))
                return;

            if (style == null)
                style = new GUIStyle { fontSize = 10, normal = { textColor = Color.white } };

            Handles.zTest = CompareFunction.Disabled;
            Handles.Label(pos, text, style);
        }
    }
}