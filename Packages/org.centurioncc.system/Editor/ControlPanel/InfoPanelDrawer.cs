using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class InfoPanelDrawer : IControlPanelDrawer
    {
        public void Draw()
        {
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
        }
    }
}
