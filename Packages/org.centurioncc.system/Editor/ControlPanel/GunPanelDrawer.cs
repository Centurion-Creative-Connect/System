using CenturionCC.System.Editor.Utils;
using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.ControlPanel
{
    public class GunPanelDrawer : IControlPanelDrawer
    {

        public void Draw()
        {
            if (GUILayout.Button("Ping GunManager"))
            {
                EditorGUIUtility.PingObject(CenturionSystemReferenceCache.GunManager);
            }
        }
    }
}
