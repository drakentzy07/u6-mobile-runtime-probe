# HIGHFLY — ClaudeCraft → Unity Bridge LAB

Status: **RUN0A / ISOLATED**
Base: SKILL4 frozen commit `7ebd380c800e720d41262d7f54ed31d6d3a79332`
ClaudeCraft donor pin: `levy-street/world-of-claudecraft@cecebab4da06287351b37e118c630c185717b81c` (v0.43.3)

## Hard rules

1. SKILL4 is read-only. Bridge work must not modify `ci/run0i/**`, `ci/run0i2/**`, `ci/combat/**` or the frozen SKILL4 workflow.
2. ClaudeCraft is a donor, never HIGHFLY's runtime authority.
3. HIGHFLY keeps its CombatCore, Monster/Encounter, RewardContext, Loot, Crafting and RG Poly world.
4. No ClaudeCraft audio bytes are imported by default. SFX timing/cue semantics may be translated, but audio must be license-cleared/CC0/HIGHFLY-owned.
5. No proxy effect is promoted as final when a real donor implementation exists.
6. Every donor remains pinned by repository + commit + source path.

## RUN0A goal

Prove that one reproducible extraction pass can read ClaudeCraft v0.43.3 and produce a Unity-facing catalog for:

- ability VFX specs;
- ability → authored animation bindings;
- SFX cue presence and audio replacement policy;
- five representative melee/movement skills.

Initial bridge candidates:

- `heroic_leap`
- `charge`
- `backstab`
- `eviscerate`
- `pummel`

RUN0A does **not** replace HIGHFLY movement, camera, hit/damage, weapon sockets or combo foundation.

## Intended runtime seam

```text
ClaudeCraft v0.43.3
  Ability/VFX/Animation/SFX metadata
            |
            v
ClaudeCraft Bridge Extractor
            |
            v
HighflyClaudeSkillDefinition
  animation
  motion intent
  hit windows
  VFX spec
  SFX cue timing
            |
            v
HIGHFLY CombatCore
```

The next stage after RUN0A metadata green is RUN0B: import/retarget the selected animation clips and execute one converted skill through the frozen Hunter.
