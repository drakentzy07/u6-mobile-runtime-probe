#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Highfly.ClaudeBridge.Editor
{
    public static class Run0BBuilder
    {
        private const string Source = "Assets/HIGHFLY/ClaudeBridge/ClaudeHeroicLeap.fbx";
        private const string TargetDir = "Assets/Resources/HIGHFLY/Run0I/Animations";
        private const string Target = TargetDir + "/Claude_Warrior_Heroic_Leap.anim";

        public static void Build()
        {
            PrepareDonorClip();
            Highfly.Run0I.Editor.WebGLBuilder.Build();

            const string outDir = "build/WebGL";
            File.WriteAllText(Path.Combine(outDir, "CLAUDE_RUN0B_BUILD.txt"),
                "HIGHFLY CLAUDECRAFT BRIDGE RUN0B | HEROIC LEAP COMPLETE PORT | " +
                "CLAUDE v0.43.3 cecebab | CLIP DONOR | FLIGHT 0.60 | APEX 3.20 | " +
                "RANGE 30 | AOE 6 | DAMAGE 24-32 | VFX CRACK RING2.2 SPARKS20 DEBRIS SMOKE OVERHEAD | " +
                "SFX RELEASE+IMPACT REPLACED LEGALLY | SKILL4 BASE 7ebd380c FROZEN");
        }

        private static void PrepareDonorClip()
        {
            AssetDatabase.Refresh();
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(Source)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
                throw new Exception("[CLAUDE RUN0B] Converted ClaudeCraft Heroic Leap donor has no AnimationClip.");

            AnimationClip donor = clips.FirstOrDefault(c =>
                c.name.IndexOf("Warrior_Heroic_Leap", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? clips[0];

            Directory.CreateDirectory(TargetDir);
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(Target) != null)
                AssetDatabase.DeleteAsset(Target);

            AnimationClip copy = UnityEngine.Object.Instantiate(donor);
            copy.name = "Claude_Warrior_Heroic_Leap";
            copy.wrapMode = WrapMode.Once;
            AssetDatabase.CreateAsset(copy, Target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AnimationClip saved = AssetDatabase.LoadAssetAtPath<AnimationClip>(Target);
            if (saved == null || saved.length < 0.45f)
                throw new Exception("[CLAUDE RUN0B] Heroic Leap .anim creation failed or clip is too short.");

            Debug.Log("[CLAUDE RUN0B] DONOR CLIP READY • source=" + donor.name +
                      " • length=" + saved.length.ToString("0.000") + "s");
        }
    }
}
