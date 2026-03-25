using CenturionCC.System.Editor.Utils;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class PlayerPanelDrawer : IControlPanelDrawer, IDisposable
    {
        private static CachedEditor _playerManagerEditor;
        private static CachedEditor _roleProviderEditor;

        private bool _isPlayerManagerFoldout = true;
        private bool _isRoleManagerFoldout = true;

        public void Draw()
        {
            if (GUILayout.Button("Ping PlayerManager"))
                EditorGUIUtility.PingObject(CenturionReferenceCache.PlayerManager);

            if (GUIUtil.HeaderFoldout("PlayerManager", ref _isPlayerManagerFoldout))
                using (new EditorGUI.IndentLevelScope())
                    DrawPlayerManager();

            GUIUtil.HorizontalBar();

            if (GUILayout.Button("Ping RoleProvider"))
                EditorGUIUtility.PingObject(CenturionReferenceCache.RoleProvider);

            if (GUIUtil.HeaderFoldout("RoleProvider", ref _isRoleManagerFoldout))
                using (new EditorGUI.IndentLevelScope())
                    DrawRoleManager();
        }
        public void Dispose()
        {
            _playerManagerEditor?.Dispose();
            _roleProviderEditor?.Dispose();
        }

        private static void DrawPlayerManager()
        {
            var playerManager = CenturionReferenceCache.PlayerManager;
            if (!playerManager)
            {
                EditorGUILayout.HelpBox("PlayerManager is not in the scene!", MessageType.Error);
                if (GUILayout.Button("Generate PlayerManager Object"))
                    CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.PlayerManager, SceneManager.GetActiveScene());

                return;
            }

            _playerManagerEditor ??= new CachedEditor(playerManager);
            _playerManagerEditor.SetTarget(playerManager);
            _playerManagerEditor.OnInspectorGUI();
        }

        private static void DrawRoleManager()
        {
            var roleProvider = CenturionReferenceCache.RoleProvider;
            if (!roleProvider)
            {
                EditorGUILayout.HelpBox("RoleProvider is not in the scene!", MessageType.Error);
                if (GUILayout.Button("Generate RoleProvider Object"))
                    CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.RoleProvider, SceneManager.GetActiveScene());

                return;
            }

            _roleProviderEditor ??= new CachedEditor(roleProvider);
            _roleProviderEditor.SetTarget(roleProvider);
            _roleProviderEditor.OnInspectorGUI();
        }
    }
}
