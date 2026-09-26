#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.MonsterLab.Editor
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
                throw new Exception("[MONSTER RUN 0] No enabled scenes.");

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;

            const string output = "build/WebGL";

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log("[MONSTER RUN 0] Building Goblin Encounter Lab with Golden Skill Lab camera.");
            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[MONSTER RUN 0] WebGL failed: " + report.summary.result);

            File.WriteAllText(
                Path.Combine(output, "MONSTER_RUN0_BUILD.txt"),
                "HIGHFLY MONSTER RUN 0 | GOBLIN ENCOUNTER | GOLDEN SKILL LAB CAMERA | REWARD CONTEXT 1.2 | UNITY 6000.6.2");

            File.WriteAllText(Path.Combine(output, ".nojekyll"), string.Empty);
            Debug.Log("[MONSTER RUN 0] BUILD SUCCESS bytes=" + report.summary.totalSize);
        }
    }
}
#endif