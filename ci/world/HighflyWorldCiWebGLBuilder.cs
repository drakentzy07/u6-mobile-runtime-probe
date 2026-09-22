#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.CI
{
    public static class WorldWebGLBuilder
    {
        public static void Build()
        {
            var scenes = new[]
            {
                "Assets/Scenes/Somnia.unity"
            };

            foreach (var scene in scenes)
            {
                if (!System.IO.File.Exists(scene))
                    throw new Exception("Missing WORLD scene: " + scene);
            }

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;

            const string outputPath = "build/WebGL";
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("[HF-WORLD-CI] Direct boot scene: Somnia");
            Debug.Log("[HF-WORLD-CI] Prologue/MainMenu temporarily excluded for WORLD lab.");

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[HF-WORLD-CI] WebGL build failed: " + report.summary.result);

            Debug.Log("[HF-WORLD-CI] WORLD v0.1B build succeeded. Size: " + report.summary.totalSize);
        }
    }
}
#endif
