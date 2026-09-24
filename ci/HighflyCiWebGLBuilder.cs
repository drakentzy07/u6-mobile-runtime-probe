#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.CI
{
    public static class WebGLBuilder
    {
        public static void Build()
        {
            PrepareParkourAnimations();
            PrepareRebootKayKitAnimations();

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new Exception("No enabled scenes found in EditorBuildSettings.");

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

            Debug.Log("[HF-CI] Building WebGL to " + outputPath);
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[HF-CI] WebGL build failed: " + report.summary.result);
            Debug.Log("[HF-CI] WebGL build succeeded. Size: " + report.summary.totalSize);
        }

        private static void PrepareRebootKayKitAnimations()
        {
            const string source =
                "Assets/HIGHFLY/Reboot/KayKitAnimSrc/Rig_Medium_CombatMelee.fbx";

            if (!File.Exists(source))
                throw new Exception(
                    "[HF-CI] Missing CC0 KayKit combat animation source: " +
                    source);

            ModelImporter importer =
                AssetImporter.GetAtPath(source) as ModelImporter;

            if (importer == null)
                throw new Exception(
                    "[HF-CI] KayKit combat ModelImporter not available.");

            bool changed =
                importer.animationType != ModelImporterAnimationType.Human ||
                !importer.importAnimation;

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (changed)
                importer.SaveAndReimport();

            const string targetDir =
                "Assets/Resources/HIGHFLY/Reboot/KayKit";

            Directory.CreateDirectory(targetDir);

            string[] wanted =
            {
                "Melee_1H_Attack_Chop",
                "Melee_1H_Attack_Slice_Horizontal",
                "Melee_1H_Attack_Slice_Diagonal",
                "Melee_1H_Attack_Stab",
                "Melee_1H_Attack_Jump_Chop"
            };

            AnimationClip[] clips =
                AssetDatabase.LoadAllAssetsAtPath(source)
                    .OfType<AnimationClip>()
                    .Where(x =>
                        x != null &&
                        !x.name.StartsWith("__preview__"))
                    .ToArray();

            foreach (string wantedName in wanted)
            {
                AnimationClip src =
                    clips.FirstOrDefault(
                        x =>
                            x.name.Equals(
                                wantedName,
                                StringComparison.OrdinalIgnoreCase) ||
                            x.name.IndexOf(
                                wantedName,
                                StringComparison.OrdinalIgnoreCase) >= 0);

                if (src == null)
                    throw new Exception(
                        "[HF-CI] KayKit clip missing: " +
                        wantedName +
                        ". Imported: " +
                        string.Join(", ", clips.Select(x => x.name)));

                string outPath =
                    targetDir + "/" + wantedName + ".anim";

                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null)
                    AssetDatabase.DeleteAsset(outPath);

                AnimationClip copy =
                    UnityEngine.Object.Instantiate(src);

                copy.name = wantedName;

                AssetDatabase.CreateAsset(copy, outPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[HF-CI] KAYKIT_CC0_COMBAT_READY clips=" +
                wanted.Length);
        }

        private static void PrepareParkourAnimations()
        {
            const string source =
                "Assets/HIGHFLY/Parkour/UAL2_Standard.fbx";

            if (!File.Exists(source))
                throw new Exception(
                    "[HF-CI] Missing CC0 UAL2 parkour source: " +
                    source);

            ModelImporter importer =
                AssetImporter.GetAtPath(source) as ModelImporter;

            if (importer == null)
                throw new Exception(
                    "[HF-CI] UAL2 ModelImporter not available.");

            bool changed =
                importer.animationType !=
                    ModelImporterAnimationType.Human ||
                !importer.importAnimation;

            importer.importAnimation = true;
            importer.animationType =
                ModelImporterAnimationType.Human;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;

            if (changed)
                importer.SaveAndReimport();

            string targetDir =
                "Assets/Resources/HIGHFLY/Parkour";

            Directory.CreateDirectory(targetDir);

            string[] wanted =
            {
                "NinjaJump_Start",
                "NinjaJump_Idle_Loop",
                "NinjaJump_Land",
                "ClimbUp_1m",
                "Slide_Start",
                "Slide_Loop",
                "Slide_Exit",
                "Sword_Dash",
                "Sword_Regular_A",
                "Sword_Regular_B",
                "Sword_Regular_C",
                "Sword_Regular_Combo",
                "Sword_Heavy_Combo",
                "Melee_Hook",
                "OverhandThrow",
                "Hit_Knockback"
            };

            AnimationClip[] clips =
                AssetDatabase.LoadAllAssetsAtPath(source)
                    .OfType<AnimationClip>()
                    .Where(x =>
                        x != null &&
                        !x.name.StartsWith("__preview__"))
                    .ToArray();

            foreach (string wantedName in wanted)
            {
                AnimationClip src =
                    clips.FirstOrDefault(
                        x =>
                            x.name.Equals(
                                wantedName,
                                StringComparison.OrdinalIgnoreCase) ||
                            x.name.IndexOf(
                                wantedName,
                                StringComparison.OrdinalIgnoreCase) >= 0);

                if (src == null)
                    throw new Exception(
                        "[HF-CI] UAL2 clip missing: " +
                        wantedName +
                        ". Imported: " +
                        string.Join(
                            ", ",
                            clips.Select(x => x.name)));

                string outPath =
                    targetDir +
                    "/" +
                    wantedName +
                    ".anim";

                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null)
                    AssetDatabase.DeleteAsset(outPath);

                AnimationClip copy =
                    UnityEngine.Object.Instantiate(src);

                copy.name = wantedName;

                AssetDatabase.CreateAsset(
                    copy,
                    outPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[HF-CI] UAL2_CC0_PARKOUR_READY clips=" +
                wanted.Length);
        }
    }
}
#endif
