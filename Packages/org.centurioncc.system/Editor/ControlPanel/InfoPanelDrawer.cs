using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class InfoPanelDrawer : IControlPanelDrawer
    {
        private bool _isSceneValidationFoldout = true;
        private bool _isVersionInfoFoldout = true;

        public void Draw()
        {
            using (new GUILayout.VerticalScope("GroupBox"))
            {
                GUILayout.Label("Shortcuts", "BoldLabel", GUILayout.ExpandWidth(true));
                var selected = GUILayout.SelectionGrid(-1, new[] { "Getting Started", "How To", "Discord" }, 3);
                switch (selected)
                {
                    case -1: break;
                    case 0: Application.OpenURL("https://system.centurioncc.org/getting-started/"); break;
                    case 1: Application.OpenURL("https://system.centurioncc.org/category/how-to"); break;
                    case 2: Application.OpenURL("https://discord.gg/CFw8Bhdgjq"); break;
                }
            }

            using (new GUILayout.VerticalScope("GroupBox"))
            {
                if (GUIUtil.HeaderFoldout("Version Info", ref _isVersionInfoFoldout))
                {
                    GUILayout.Space(5);
                    DrawVersionInfo();
                }
            }

            using (new GUILayout.VerticalScope("GroupBox"))
            {
                if (GUIUtil.HeaderFoldout("Scene Validation", ref _isSceneValidationFoldout))
                {
                    GUILayout.Space(5);
                    DrawSceneValidation();
                }
            }
        }

        private static void DrawVersionInfo()
        {
            var system = CenturionReferenceCache.CenturionSystem;
            if (!system)
            {
                EditorGUILayout.HelpBox("CenturionSystem component is not in the scene!", MessageType.Error);
                if (GUILayout.Button("Generate CenturionSystem Object"))
                {
                    CenturionSampleFactory.Create(CenturionSampleFactory.ObjectType.CenturionSystem, SceneManager.GetActiveScene());
                    CenturionSystemControlPanelWindow.MarkForValidation();
                }

                return;
            }

            EditorGUILayout.LabelField("Version", system.GetVersion());
            EditorGUILayout.LabelField("Branch", system.GetBranch());
            EditorGUILayout.LabelField("Commit", system.GetCommitHash());
            if (GUILayout.Button("Refresh"))
            {
                CenturionSystemBuildProcessor.BakeVersionAndLicense();
            }
        }

        private static void DrawSceneValidation()
        {
            var validationResult = Validator.GetLastValidationResult();
            if (validationResult.Count == 0)
            {
                EditorGUILayout.HelpBox("The scene looks OK!", MessageType.Info);
            }

            foreach (var validationInfo in validationResult)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(validationInfo.Message, validationInfo.MessageType);
                    if (validationInfo.AutoFix != null && GUILayout.Button("Auto Fix", GUILayout.Width(80)))
                    {
                        validationInfo.AutoFix();
                        CenturionSystemControlPanelWindow.MarkForValidation();
                    }

                    if (validationInfo.TargetObject != null && GUILayout.Button("Ping Object", GUILayout.Width(80)))
                    {
                        EditorGUIUtility.PingObject(validationInfo.TargetObject);
                    }
                }
            }

            if (GUILayout.Button("Run Validation"))
            {
                Validator.PerformValidation();
            }
        }
    }
}
