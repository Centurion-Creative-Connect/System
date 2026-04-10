using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Player;
using DerpyNewbie.Common.Role;
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
            using (new GUILayout.VerticalScope("GroupBox"))
            {
                var playerManager = CenturionReferenceCache.PlayerManager;
                if (GUIUtil.HeaderFoldoutWithObjectSelection("PlayerManager", playerManager, ref _isPlayerManagerFoldout))
                {
                    GUILayout.Space(5);
                    DrawPlayerManager(playerManager);
                }
            }

            using (new GUILayout.VerticalScope("GroupBox"))
            {
                var roleProvider = CenturionReferenceCache.RoleProvider;
                if (GUIUtil.HeaderFoldoutWithObjectSelection("RoleProvider", roleProvider, ref _isRoleManagerFoldout))
                {
                    GUILayout.Space(5);
                    DrawRoleManager(roleProvider);
                }
            }
        }
        public void Dispose()
        {
            _playerManagerEditor?.Dispose();
            _roleProviderEditor?.Dispose();
        }

        private static void DrawPlayerManager(PlayerManagerBase playerManager)
        {
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

        private static void DrawRoleManager(RoleProvider roleProvider)
        {
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
