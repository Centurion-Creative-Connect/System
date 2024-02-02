using System;
using System.Diagnostics;
using CenturionCC.System.Editor.EditorInspector.Utilities;
using UnityEditor;
using VRC.SDK3.Editor;

namespace CenturionCC.System.Editor
{
    [InitializeOnLoad]
    public class CenturionSystemBuildProcessor
    {
        static CenturionSystemBuildProcessor()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
        }

        private static void AddBuildHook(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder))
            {
                builder.OnSdkBuildStart += OnBuildStart;
                builder.OnSdkBuildFinish += OnPostBuildCleanup;
            }
        }


        private static void PlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    TerrainMarkerEditor.BakeAll();
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    TerrainMarkerEditor.ClearBakedAll();
                    break;
            }
        }

        private static void OnBuildStart(object sender, object target)
        {
            var stopwatch = Stopwatch.StartNew();
            UnityEngine.Debug.Log("[CenturionSystemBuildProcessor] Build Preprocessing started");

            try
            {
                TerrainMarkerEditor.BakeAll();

                UnityEngine.Debug.Log(
                    $"[CenturionSystemBuildProcessor] Build Preprocessing completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.LogError(
                    $"[CenturionSystemBuildProcessor] Build Preprocessing were canceled in {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }

        private static void OnPostBuildCleanup(object sender, object target)
        {
            var stopwatch = Stopwatch.StartNew();
            UnityEngine.Debug.Log("[CenturionSystemBuildProcessor] Post Build Cleanup started");

            try
            {
                TerrainMarkerEditor.ClearBakedAll();

                UnityEngine.Debug.Log(
                    $"[CenturionSystemBuildProcessor] Post Build Cleanup completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.LogError(
                    $"[CenturionSystemBuildProcessor] Post Build Cleanup were canceled in {stopwatch.ElapsedMilliseconds}ms");
                throw;
            }
        }
    }
}