using UnityEditor;
using UnityEngine;
namespace CenturionCC.System.Editor.Utils
{
    public static class CenturionSampleResources
    {
        public static GameObject GunManagerSample => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/Gun/CenturionGunSystemSample.prefab");
        public static GameObject GunSummonerSample => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Utilities/SampleGunSummoners.prefab");
        public static GameObject PlayerManagerSample => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/Player/CenturionPlayerSystemSample.prefab");
        public static GameObject BasicCommands => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/CenturionBasicCommands.prefab");
        public static GameObject HeadUI => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/HeadUI/SampleHeadUI.prefab");
        public static GameObject AudioManager => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/AudioManager.prefab");
        public static GameObject RoleManager => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/RoleManager.prefab");
        public static GameObject UpdateManager => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/org.centurioncc.system/Samples/Prefabs/Systems/UpdateManager.prefab");
        public static class External
        {
            public static GameObject NewbieConsole => AssetDatabase.LoadAssetAtPath<GameObject>("Packages/dev.derpynewbie.logger/Samples/Prefabs/NewbieLoggerAndConsole.prefab");
        }
    }
}
