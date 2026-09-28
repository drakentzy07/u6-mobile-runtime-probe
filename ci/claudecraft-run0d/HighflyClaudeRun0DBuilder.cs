#if UNITY_EDITOR
using System.IO;

namespace Highfly.ClaudeBridge.Editor
{
    public static class Run0DBuilder
    {
        public static void Build()
        {
            // Recreate the exact five authored body clips from the pinned
            // KayKit donor poses, then run only the RUN0D original-full runtime.
            HighflyClaudePoseBaker.BakeAll();

            // Frozen SKILL4 world/combat foundation remains unchanged.
            Highfly.Run0I.Editor.WebGLBuilder.Build();

            const string outDir = "build/WebGL";
            File.WriteAllText(Path.Combine(outDir, "CLAUDE_RUN0D_BUILD.txt"),
                "HIGHFLY CLAUDECRAFT BRIDGE RUN0D | ORIGINAL FULL FIVE SKILLS | " +
                "HEROIC LEAP APPROVED RUN0B UNCHANGED | " +
                "AMBUSH ORIGINAL VORTEX+IMPLOSION+VERTICAL+X+SWING2 | " +
                "BACKSTAB ORIGINAL THRUST+VOID FLIPBOOK+ECHO | " +
                "EVISCERATE ORIGINAL BLOOD FINISHER+UPPERCUT+X+PILLAR+WAVES | " +
                "PUMMEL ORIGINAL PUNCH_A+UPPERCUT+X+4S STUN STARS | " +
                "NO GAP CLOSE | NO TELEPORT | MELEE RANGE GATE | " +
                "ATTACK TIMESCALE 1.3 | INSTANT IMPACT RELEASE+0.15 | " +
                "PROCEDURAL 8X8 FLIPBOOKS VOID FLAME ELECTRIC | " +
                "SPECTACLE TIER0 CONSTANTS PORTED | " +
                "CLAUDE v0.43.3 cecebab | HIGHFLY LEGAL AUDIO ONLY | " +
                "SKILL4 BASE 7ebd380c FROZEN");
        }
    }
}
#endif
