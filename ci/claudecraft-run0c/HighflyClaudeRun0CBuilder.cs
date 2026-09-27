#if UNITY_EDITOR
using System.IO;

namespace Highfly.ClaudeBridge.Editor
{
    public static class Run0CBuilder
    {
        public static void Build()
        {
            // One baker produces all five ClaudeCraft-derived clips.
            HighflyClaudePoseBaker.BakeAll();

            // Build the frozen SKILL4 lab exactly as before.
            Highfly.Run0I.Editor.WebGLBuilder.Build();

            const string outDir = "build/WebGL";
            File.WriteAllText(Path.Combine(outDir, "CLAUDE_RUN0C_BUILD.txt"),
                "HIGHFLY CLAUDECRAFT BRIDGE RUN0C | FIVE SKILLS TOGETHER | " +
                "HEROIC_LEAP APPROVED RUN0B + AMBUSH + BACKSTAB + EVISCERATE + PUMMEL | " +
                "FIVE CLAUDE CLIPS BAKED | SHARED BATCH RUNTIME | KEYS 1-5 | TOUCH BUTTONS | " +
                "AMBUSH STEALTH+BEHIND LAB SIMULATED | BACKSTAB BEHIND LAB SIMULATED | " +
                "EVISCERATE 5 COMBO LAB SIMULATED | PUMMEL INTERRUPT 4S CD10 | " +
                "REAL PARTICLE PREFABS SMOKE SPARKS ELECTRICAL PLASMA | " +
                "SFX HIGHFLY LEGAL REPLACEMENTS | CLAUDE v0.43.3 cecebab | " +
                "NO GLB ASSIMP PATH | SKILL4 BASE 7ebd380c FROZEN");
        }
    }
}
#endif
