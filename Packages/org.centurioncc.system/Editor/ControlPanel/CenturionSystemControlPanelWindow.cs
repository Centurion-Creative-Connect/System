using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Editor.Validation;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private static bool _shouldPerformValidation = true;
        private static Vector2 _scrollPosition;
        private static readonly Dictionary<ControlPanelTab, IControlPanelDrawer> TabDrawers = new Dictionary<ControlPanelTab, IControlPanelDrawer>
        {
            [ControlPanelTab.Info] = new InfoPanelDrawer(),
            [ControlPanelTab.Player] = new PlayerPanelDrawer(),
            [ControlPanelTab.Gun] = new GunPanelDrawer(),
            [ControlPanelTab.Migration] = new MigrationPanelDrawer(),
        };

        private void Awake() => MarkForValidation();

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnGUI()
        {
            if (_shouldPerformValidation)
            {
                CenturionSystemBuildProcessor.BakeVersionAndLicense();
                Validator.PerformValidation();
                _shouldPerformValidation = false;
            }

            var controlPanelTabs = Enum.GetValues(typeof(ControlPanelTab));
            bool DrawTabButton(string label, ControlPanelTab tab)
            {
                var style = _currentTab == tab ? _activeStyle : _inactiveStyle;
                var scaleRatio = 100f / _headerTexture.height;
                var actualWidth = _headerTexture.width * scaleRatio;
                if (GUILayout.Button(label, style, GUILayout.Width(_headerTexture != null ? actualWidth / controlPanelTabs.Length : 200f)))
                {
                    _currentTab = tab;
                    return true;
                }

                return false;
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
                    if (DrawTabButton(controlPanelTab.ToString(), (ControlPanelTab)controlPanelTab))
                        _scrollPosition = Vector2.zero;
                GUILayout.FlexibleSpace();
            }

            GUIUtil.HorizontalBar();

            using (var scroll = new GUILayout.ScrollViewScope(_scrollPosition, GUI.skin.box))
            {
                _scrollPosition = scroll.scrollPosition;
                TabDrawers[_currentTab].Draw();
            }
        }
        private void OnHierarchyChange() => MarkForValidation();
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => MarkForValidation();

        [MenuItem("Centurion System/Control Panel")]
        public static void OpenWindow()
        {
            var window = GetWindow<CenturionSystemControlPanelWindow>();
            window.titleContent = new GUIContent("Centurion System Control Panel");
            window.Show();
        }

        public static void MarkForValidation()
        {
            _shouldPerformValidation = true;
        }

        private enum ControlPanelTab
        {
            Info,
            Player,
            Gun,
            Migration,
        }
    }
}
