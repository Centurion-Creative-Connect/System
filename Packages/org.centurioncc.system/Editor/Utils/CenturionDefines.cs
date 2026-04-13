using System.Linq;
using UnityEditor;
namespace CenturionCC.System.Editor.Utils
{
    public static class CenturionDefines
    {
        public const string CENTURION_SYSTEM_PREFIX = "CENTURIONSYSTEM_";
        public const string VERBOSE_LOGGING = CENTURION_SYSTEM_PREFIX + "VERBOSE_LOGGING";
        public const string GUN_LOGGING = CENTURION_SYSTEM_PREFIX + "GUN_LOGGING";
        public const string PLAYER_LOGGING = CENTURION_SYSTEM_PREFIX + "PLAYER_LOGGING";

        public static bool IsSymbolDefined(string define)
        {
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out var symbols);
            return symbols.Contains(define);
        }

        public static void AddSymbols(string[] defines)
        {
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out var symbols);

            var symbolList = symbols.ToList();
            symbolList.RemoveAll(defines.Contains);
            symbolList.AddRange(defines);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbolList.ToArray());
        }

        public static void RemoveSymbols(string[] define)
        {
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, out var symbols);

            var symbolList = symbols.ToList();
            symbolList.RemoveAll(define.Contains);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbolList.ToArray());
        }
    }
}
