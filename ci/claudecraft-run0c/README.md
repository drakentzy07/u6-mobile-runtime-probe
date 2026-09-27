# HIGHFLY — ClaudeCraft Bridge RUN0C

Goal: prove the approved RUN0B seam scales from one skill to a five-skill batch without touching frozen SKILL4.

Base:
- SKILL4 frozen: `7ebd380c800e720d41262d7f54ed31d6d3a79332`
- ClaudeCraft donor: `levy-street/world-of-claudecraft@cecebab4da06287351b37e118c630c185717b81c` (v0.43.3)
- Approved control: Heroic Leap RUN0B

Batch:
1. Heroic Leap — approved RUN0B runtime, unchanged.
2. Ambush — Claude `Rogue_Ambush`, shadow windup + double strike.
3. Backstab — Claude `Rogue_Backstab`, thrust impact.
4. Eviscerate — Claude `Rogue_Finisher_Slash`, two-hit finisher.
5. Pummel — Claude `Punch_A`, bare-fist interrupt marker.

LAB-only condition assists:
- Ambush stealth/behind is simulated.
- Backstab behind is simulated.
- Eviscerate uses a simulated 5-combo finisher context.
- Pummel marks the canonical 4s interrupt semantics; dummy casting school lockout is deferred to CombatCore integration.

Input:
- PC: 1–5.
- Touch: Heroic Leap keeps its approved button; RUN0C adds four touch buttons for 2–5.

VFX:
- No new geometric proxy VFX.
- Reuses real Unity particle prefabs already shipped in the pinned Lucid project.
- Runtime tinting maps the donor palettes: shadow, blood, physical.

Final balance remains owned by HIGHFLY CombatCore.
