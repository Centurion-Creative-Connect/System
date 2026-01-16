using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.ControlPanel
{
    public interface IControlPanelDrawer
    {
        void Draw();
    }

    public class CenturionSystemControlPanelWindow : UnityEditor.EditorWindow
    {

        private static Texture2D _headerTexture;
        private static GUIStyle _centeredLabel;
        private static GUIStyle _activeStyle;
        private static GUIStyle _inactiveStyle;
        private static ControlPanelTab _currentTab = ControlPanelTab.Info;
        private static Dictionary<ControlPanelTab, IControlPanelDrawer> _tabDrawers = new Dictionary<ControlPanelTab, IControlPanelDrawer>
        {
            [ControlPanelTab.Info] = new InfoPanelDrawer(),
            [ControlPanelTab.Player] = new PlayerPanelDrawer(),
            [ControlPanelTab.Gun] = new GunPanelDrawer(),
        };

        public void OnGUI()
        {
            var controlPanelTabs = Enum.GetValues(typeof(ControlPanelTab));
            void DrawTabButton(string label, ControlPanelTab tab)
            {
                var style = _currentTab == tab ? _activeStyle : _inactiveStyle;
                var scaleRatio = 100f / _headerTexture.height;
                var actualWidth = _headerTexture.width * scaleRatio;
                if (GUILayout.Button(label, style, GUILayout.Width(_headerTexture != null ? actualWidth / controlPanelTabs.Length : 200f)))
                {
                    _currentTab = tab;
                }
            }

            _headerTexture ??= AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/org.centurioncc.system/Editor/Textures/centurion-system-banner-v2.png");
            _centeredLabel ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(0, 0, 0, 0),
            };

            _activeStyle ??= new GUIStyle(GUI.skin.button) { border = new RectOffset(0, 0, 0, 0) };
            _inactiveStyle ??= new GUIStyle(_activeStyle) { normal = { textColor = Color.gray } };
            GUILayout.Label(_headerTexture, _centeredLabel, GUILayout.Height(100));
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                foreach (var controlPanelTab in controlPanelTabs)
                    DrawTabButton(controlPanelTab.ToString(), (ControlPanelTab)controlPanelTab);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Separator();

            _tabDrawers[_currentTab].Draw();
        }


        [MenuItem("Centurion-Utils/Centurion System Control Panel")]
        public static void OpenWindow()
        {
            var window = GetWindow<CenturionSystemControlPanelWindow>();
            window.name = "Centurion System Control Panel";
            window.Show();
        }
        private enum ControlPanelTab
        {
            Info,
            Player,
            Gun,
        }
    }
}
