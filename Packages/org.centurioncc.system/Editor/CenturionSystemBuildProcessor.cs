using CenturionCC.System.Editor.EditorInspector.Utilities;
using JetBrains.Annotations;
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Editor;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
namespace CenturionCC.System.Editor
{
    [InitializeOnLoad]
    public class CenturionSystemBuildProcessor
    {
        static CenturionSystemBuildProcessor()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            VRCSdkControlPanel.OnSdkPanelEnable -= AddBuildHook;

            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
        }

        private static void AddBuildHook(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder))
            {
                builder.OnSdkBuildStart -= OnBuildStart;
                builder.OnSdkBuildFinish -= OnPostBuildCleanup;

                builder.OnSdkBuildStart += OnBuildStart;
                builder.OnSdkBuildFinish += OnPostBuildCleanup;
            }
        }


        private static void PlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    BakeVersionAndLicense();
                    TerrainMarkerEditor.BakeAll();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    BakeVersionAndLicense();
                    TerrainMarkerEditor.ClearBakedAll();
                    break;
            }
        }

        private static void OnBuildStart(object sender, object target)
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.Log("[CenturionSystemBuildProcessor] Build Preprocessing started");

            try
            {
                BakeVersionAndLicense();
                TerrainMarkerEditor.BakeAll();

                Debug.Log(
                    $"[CenturionSystemBuildProcessor] Build Preprocessing completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException)
            {
                Debug.LogError(
                    $"[CenturionSystemBuildProcessor] Build Preprocessing were canceled in {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        private static void OnPostBuildCleanup(object sender, object target)
        {
            var stopwatch = Stopwatch.StartNew();
            Debug.Log("[CenturionSystemBuildProcessor] Post Build Cleanup started");

            try
            {
                BakeVersionAndLicense();
                TerrainMarkerEditor.ClearBakedAll();

                Debug.Log(
                    $"[CenturionSystemBuildProcessor] Post Build Cleanup completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException)
            {
                Debug.LogError(
                    $"[CenturionSystemBuildProcessor] Post Build Cleanup were canceled in {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        public static void BakeVersionAndLicense()
        {
            var centurionSystem = GetCenturionSystem();
            if (centurionSystem == null)
            {
                Debug.LogWarning("[CenturionSystemBuildProcessor] Could not find CenturionSystem");
                return;
            }

            var packageInfo = PackageInfo.FindForAssembly(typeof(CenturionSystem).Assembly);
            if (packageInfo == null)
            {
                Debug.LogWarning("[CenturionSystemBuildProcessor] Could not find org.centurioncc.system package");
                return;
            }

            var so = new SerializedObject(centurionSystem);
            var versionProperty = so.FindProperty("version");
            var version = packageInfo.version;
            versionProperty.stringValue = version;

            var commitHashProperty = so.FindProperty("commitHash");
            var commitHash = packageInfo.git?.hash;
            var hasProjectGitInfo = TryGetProjectBranchAndHash(out var projectBranch, out var projectCommitHash);
            commitHashProperty.stringValue = string.IsNullOrWhiteSpace(commitHash) ? projectCommitHash : commitHash;

            var branchProperty = so.FindProperty("branch");
            var branch = projectBranch ?? "unknown";
            branchProperty.stringValue = branch;

            var licenseProperty = so.FindProperty("license");
            var licenseFilePath = Path.GetFullPath("LICENSE.md", packageInfo.resolvedPath);
            var license = File.ReadAllText(licenseFilePath);
            licenseProperty.stringValue = license;

            so.ApplyModifiedProperties();

            Debug.Log($"[CenturionSystemBuildProcessor] Baked version and license\nversion: {version}\nhash: {commitHash}\nbranch: {branch}\nlicense: {license}");
        }

        [CanBeNull]
        private static CenturionSystem GetCenturionSystem()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                foreach (var centurionSystem in rootGameObject.GetComponentsInChildren<CenturionSystem>())
                {
                    return centurionSystem;
                }
            }

            return null;
        }

        private static string GetProjectCommitHash()
        {
            return TryGetProjectBranchAndHash(out var branch, out var hash) ? hash : "";

        }

        private static bool TryGetProjectBranchAndHash(out string branch, out string hash)
        {
            branch = null;
            hash = null;

            var gitDir = Path.Combine(Application.dataPath, "..", ".git");
            var headPath = Path.Combine(gitDir, "HEAD");

            if (!File.Exists(headPath))
                return false;

            var head = File.ReadAllText(headPath).Trim();

            // Detached HEAD
            if (!head.StartsWith("ref: "))
            {
                hash = head;
                return true;
            }

            var refPath = head.Substring(5); // "refs/heads/xxx"
            branch = Path.GetFileName(refPath);

            var refFullPath = Path.Combine(gitDir, refPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(refFullPath))
                return false;

            hash = File.ReadAllText(refFullPath).Trim();
            return true;
        }
    }
}
