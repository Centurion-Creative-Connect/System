using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class PlayerPanelDrawer : IControlPanelDrawer
    {
        public void Draw()
        {
            if (GUILayout.Button("Ping PlayerManager"))
            {
                EditorGUIUtility.PingObject(CenturionSystemReferenceCache.PlayerManager);
            }
        }
    }
}
