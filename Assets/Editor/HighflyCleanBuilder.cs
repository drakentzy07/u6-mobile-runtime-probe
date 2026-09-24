#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Highfly.Clean.Editor
{
    public static class HighflyCleanBuilder
    {
        public static void Build()
        {
            const string scenePath = "Assets/Run0A_Clean.unity";
            const string outputPath = "build/WebGL";

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject root = new GameObject("HIGHFLY_CLEAN_BOOTSTRAP");
            root.AddComponent<HighflyCleanBootstrap>();

            EditorSceneManager.SaveScene(scene, scenePath);

            PlayerSettings.productName = "HIGHFLY Combat Reboot Run0A";
            PlayerSettings.companyName = "HIGHFLY";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;

            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.WebGL,
                BuildTarget.WebGL);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception(
                    "[CLEAN-RUN0A] WebGL build failed: " +
                    report.summary.result);

            string indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
                throw new FileNotFoundException(
                    "[CLEAN-RUN0A] Build reported success but index.html is missing.",
                    indexPath);

            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), string.Empty);

            Debug.Log(
                "[CLEAN-RUN0A] BUILD SUCCESS • size=" +
                report.summary.totalSize +
                " bytes • nojekyll=OK");
        }
    }
}
#endif
