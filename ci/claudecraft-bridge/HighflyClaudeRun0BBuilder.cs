#if UNITY_EDITOR
using System.IO;

namespace Highfly.ClaudeBridge.Editor
{
    public static class Run0BBuilder
    {
        public static void Build()
        {
            // Recreate all five selected ClaudeCraft body clips from the
            // already-proven KayKit FBX donors. This intentionally avoids
            // ClaudeCraft's animation-only GLB transport and Assimp.
            HighflyClaudePoseBaker.BakeAll();

            // Build the exact frozen SKILL4 lab unchanged.
            Highfly.Run0I.Editor.WebGLBuilder.Build();

            const string outDir = "build/WebGL";
            File.WriteAllText(Path.Combine(outDir, "CLAUDE_RUN0B_BUILD.txt"),
                "HIGHFLY CLAUDECRAFT BRIDGE RUN0B | NATIVE UNITY POSE BAKER | " +
                "FIVE CLAUDE CLIPS BAKED | HEROIC LEAP LIVE PORT | " +
                "CLAUDE v0.43.3 cecebab | FLIGHT 0.60 | APEX 3.20 | " +
                "RANGE 30 | AOE 6 | DAMAGE 24-32 | " +
                "VFX CRACK RING2.2 SPARKS20 DEBRIS SMOKE OVERHEAD | " +
                "SFX RELEASE+IMPACT REPLACED LEGALLY | " +
                "NO GLB ASSIMP PATH | SKILL4 BASE 7ebd380c FROZEN");
        }
    }
}
#endif
