using CenturionCC.System.Editor.Utils;
using CenturionCC.System.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class InfoPanelDrawer : IControlPanelDrawer
    {
        public void Draw()
        {
            EditorGUILayout.LabelField("Version Info", EditorStyles.boldLabel);
            var system = CenturionSystemReferenceCache.CenturionSystem;
            if (system != null)
            {
                EditorGUILayout.LabelField("Version", system.GetVersion());
                EditorGUILayout.LabelField("Branch", system.GetBranch());
                EditorGUILayout.LabelField("Commit", system.GetCommitHash());
                if (GUILayout.Button("Refresh"))
                {
                    CenturionSystemBuildProcessor.BakeVersionAndLicense();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("CenturionSystem component is not in the scene!", MessageType.Error);
                if (GUILayout.Button("Generate CenturionSystem Object"))
                {
                    var go = new GameObject("CenturionSystem");
                    go.AddComponent<CenturionSystem>();
                    SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
                    CenturionSystemControlPanelWindow.MarkForValidation();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Validation", EditorStyles.boldLabel);

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

            EditorGUILayout.Space();
        }
    }
}
