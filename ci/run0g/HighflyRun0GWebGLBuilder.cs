#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.Run0G.Editor
{
    public static class WebGLBuilder
    {
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new Exception("[RUN0G] No enabled scenes.");

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;

            const string output = "build/WebGL";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("[RUN0G] Building CLEAN Lucid+Dragon+KayKit WebGL.");
            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[RUN0G] WebGL build failed: " + report.summary.result);

            File.WriteAllText(
                Path.Combine(output, "RUN0G_BUILD.txt"),
                "HIGHFLY RUN0G CLEAN | LUCID + DRAGON + KAYKIT | UNITY 6000.6.2");

            File.WriteAllText(Path.Combine(output, ".nojekyll"), string.Empty);

            Debug.Log("[RUN0G] BUILD SUCCESS bytes=" + report.summary.totalSize);
        }
    }
}
#endif
