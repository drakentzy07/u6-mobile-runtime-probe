# HIGHFLY Skill Lab v0.12 — Donor Vertical Slice

## Goal
Build one nearly-final HIGHFLY skill end-to-end before expanding the roster.

The first vertical slice is:

ARTE DEL SACRIFICIO — PREMIUM

It must arrive in LAB with mechanics + animation + movement + weapon physics + VFX + hit feedback + camera + 3-target behavior together.

## Base architecture
- Lucid = player/movement/camera/targeting skeleton.
- HIGHFLY CORE = damage/stats/skill runtime/mobile controls.
- Dragon Souls = donor for physical sword throw/embedded state/recall architecture.
- SubspaceHunter = donor for projectile/cast/VFX sequencing patterns and hit-feedback logic.
- UAL2 = CC0 animation source for throw/dash/landing where retargeting is clean.
- HIGHFLY = final naming, input, balance, timing, VFX composition and evolution.

Do NOT import whole donor frameworks or duplicate PlayerControllers.

## Dragon Souls harvest
Use the concepts/code patterns from:
- CombatController.ThrowSword()
- PlayerAimState throw flow
- Sword.Throwed()
- Sword.OnTriggerEnter()
- Sword.Return()
- embedded-enemy state
- trail/hitbox lifecycle
- return curve
- hit/return/grab event hooks

Keep MIT notice for copied/substantial code.

Do NOT assume third-party raw assets inside Dragon Souls are MIT-covered.

## SubspaceHunter harvest
Use public/commercially-usable original code patterns for:
- projectile spawn points and directional emitters
- magic prepare/release lifecycle
- fire/lightning/ice/meteor architecture
- skill release particles
- hit explosion and enemy hit-feedback sequencing

Do not import SAO IP assets or mixed third-party directories blindly.
Audit every raw asset before inclusion.

## Premium VFX packages owned by user but NOT currently present in CI
Needed for final visual pass:
- Cartoon FX Remaster Free
- Free Slash VFX — MaykerStudio
- Magic Effects FREE — Hovl Studio

Because the main HIGHFLY GitHub repository is public, raw Asset Store source files must NOT be committed there unless their redistribution terms explicitly allow it.

## Optional external SubspaceHunter package
The public SubspaceHunter repository references an external UnityPackage.
Only import after auditing:
- original/commercially-usable project content
- third-party package licenses
- SAO/IP-specific content
- redistribution restrictions

## ARTE DEL SACRIFICIO — v0.12 target

### Phase A — Aim
Hold skill:
- Hunter enters throw preparation animation.
- trajectory preview optional in LAB only.
- target lock assists facing but does not auto-hit.

### Phase B — Throw
Release:
- actual equipped HIGHFLY weapon leaves hand.
- real projectile motion.
- spin.
- blade trail.
- active hitbox.
- swipe/launch VFX.

### Phase C — Impact
Enemy hit:
- weapon embeds visually where possible.
- impact spark/slash.
- brief local hitstop.
- target hit reaction.
- stored state = EMBEDDED.

World hit:
- weapon anchors at hit point.
- ground impact VFX.
- state = ANCHORED.

### Phase D — Choice
Second tap:
- RECALL.

Hold second input:
- DETONATE -> RECALL.

### Phase E — Recall
- weapon leaves embedded/anchor state.
- extraction hit if embedded.
- curved return path.
- damaging return pass.
- return trail distinct from outbound trail.
- catches at hand.
- short grab flash.

### Detonate
- compressed rune / magic circle around blade.
- short charge read.
- blast/shockwave.
- recall starts immediately after blast.

### Three-target rules
SMALL:
- direct hit may launch.
- embedded position should stay visually low enough.

HUMANOID:
- full embed/extraction behavior.

LARGE:
- no absurd body displacement.
- impact = poise damage.
- embed/anchor on body surface if stable; fallback to contact point anchor.

### Premium acceptance
- weapon is never represented by a generic cube.
- outbound and return motion are visually readable.
- impact has contact VFX and target response.
- recall is a real gameplay phase, not teleport-to-hand.
- detonation is readable without HUD text.
- no smoke/particle spam.
- Android/WebGL budget remains stable.
- works in 1 TARGET and 3 TARGETS modes.

## After Arte del Sacrificio passes
Reuse the same vertical-slice quality pipeline for:
1. Danza Gemela
2. Pile Breaker
3. Cadena Sin Limite
4. Seven Sinker
5. Counterforge
6. Drift de Formula
7. Edge Runner
8. Impacto Dual
9. Scrap & Build
