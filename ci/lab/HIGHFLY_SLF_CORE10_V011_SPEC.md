# HIGHFLY SKILL LAB v0.11 — PREMIUM SLF CORE 10

## Purpose

This branch turns the Skill Lab from a loose prototype space into a controlled implementation lab.

The design order is mandatory:

REFERENCE STUDY -> HIGHFLY THEORY SPEC -> IMPLEMENTATION -> 3-MONSTER TEST -> POLISH -> PREMIUM APPROVAL

SLF is the primary combat-design reference. SAO, Solo Leveling and Exiled Heavy Knight remain secondary idea libraries. We reproduce mechanics and design principles from scratch with HIGHFLY controls, visuals, animation, code and balance.

No skill is considered "ready" because it has a button, damage and particles.

## Skill quality contract

Each skill specification must define:

1. Fantasy
2. Combat function
3. Input
4. Targeting
5. Hunter movement
6. Hit geometry
7. Timing
8. Skill expression
9. Risk
10. Counterplay
11. Resource / cooldown
12. Animation
13. VFX stack
14. Visual readability
15. Impact / hit feel
16. Evolution
17. Fusion hooks
18. Apex / Unique destination
19. PvP / multiplayer rules
20. LAB acceptance test

## LAB status vocabulary

- DESIGN: theory only.
- PROTOTYPE: mechanic works but presentation or rules are incomplete.
- SEMI-COOKED: core mechanic, animation, VFX intent and 3-monster behavior exist.
- PREMIUM: mechanically and visually approved for promotion to HIGHFLY CORE.

## Target rig

Every skill must be tested against three passive targets:

- SMALL: low and narrow hit volume.
- HUMANOID: standard reference target.
- LARGE: boss-scale body and poise rules.

Target mode:
- 1 TARGET
- 3 TARGETS

Passive targets receive damage, marks, stagger, launch, pull, knockback and status, but do not attack unless AI testing is explicitly enabled.

---

# CORE 01 — DANZA GEMELA REFORGED

Status target: SEMI-COOKED -> PREMIUM

## Fantasy
The hunter enters a progressively more aggressive sword dance. The player maintains the dance through timing and directional execution.

## Input
Single skill button.

Phase 1:
- First tap starts the chain.

Phase 2:
- Follow-up input has an early reject zone, valid zone and perfect zone.

Phase 3:
- Same logic.
- Missing the late window breaks the chain.

Provisional timing targets:
- Too early: < 0.17 s
- Perfect: 0.24–0.43 s
- Valid: 0.17–0.78 s
- Too late: chain reset

These values are LAB tuning targets, not permanent constants.

## Sequence
Phase 1: 2 cuts.
Phase 2: 3 cuts + lateral displacement.
Phase 3: 5 distinct cuts + finisher.

Perfect Link on both transitions unlocks Perfect Chain:
- explicit extra player input,
- 4 delayed cuts,
- final X finisher.

## Targeting
- Lock keeps target preference.
- No magnetic auto-correction beyond each phase's correction cone.
- Direction can be adjusted between phases.
- Target switching allowed between phases, not during an individual cut.

## Movement
- Phase 1: short approach.
- Phase 2: lateral/diagonal reposition.
- Phase 3: committed predator finish.
- Perfect Chain crosses slightly through the target.

## Hit geometry
Actual sword-path-aligned arcs, not arbitrary spheres.

## Skill expression
Timing + direction + target switching + spacing.

## Risk / counterplay
- Whiffing a phase exposes recovery.
- Opponent can move outside correction cone.
- Perfect Chain is not automatic.
- Large targets resist displacement.

## Animation
Preferred UAL2 candidates:
- Sword_Regular_A
- Sword_Regular_B
- Sword_Regular_C
- Sword_Regular_Combo

Fallback: native Hunter combo if retargeting is poor.

## VFX stack
- blade-attached trail,
- distinct slash silhouette per cut,
- contact spark,
- small perfect-link cue,
- distortion/shockwave only on final impact,
- no smoke spam.

## Monster rules
SMALL:
- slash arcs lower to body center,
- light knockback only.

HUMANOID:
- full combo and hit reactions.

LARGE:
- no silly displacement,
- poise damage instead,
- final impact may stagger if threshold is crossed.

## Evolution hooks
Corte Gemelo -> Danza Gemela -> Perfect Chain -> Danza Fantasma -> Danza Umbria -> Eclipse Dual -> Unique branching chain.

## Acceptance
- Each cut visually differs.
- Timing failure is readable.
- Perfect Link is readable without text.
- Sword trail follows the weapon.
- No duplicate basic attack at chain end.
- Works on all three target scales.

---

# CORE 02 — DRIFT DE FORMULA

Status target: PREMIUM candidate.

## Fantasy
Predatory target-relative orbit that lets the player steal the enemy's flank.

## Function
Movement / positional offense.

## Input
Tap skill + horizontal movement input chooses orbit side.
Neutral input uses last safe side.

## Targeting
Lock or best target inside range.
Player maintains manual side choice.

## Movement
Fast curved orbit around the target.
Hunter continuously faces target.
Ends near flank/back, not with forced teleport.

## Hit geometry
Optional exit slash only if within valid melee distance.

## Skill expression
Left/right choice, spacing, angle, timing into next skill.

## Risk / counterplay
Opponent may move, disengage or punish destination.
No invulnerable full-circle orbit.

## VFX
Thin low-noise trail + brief afterimage.
No explosion.

## Monster rules
SMALL: reduced orbit radius.
HUMANOID: standard.
LARGE: larger radius and no clipping through body.

## Evolution
Deriva -> Formula Drift -> Predator Orbit -> multi-target route / shadow relay fusion.

## Acceptance
Movement must feel player-driven, not cinematic.

---

# CORE 03 — IMPACTO DUAL

Status target: SEMI-COOKED.

## Fantasy
First hit loads a weak point; second well-timed hit collapses it.

## Function
Timing / burst / stagger.

## Input
Tap 1 = mark hit.
Tap 2 = detonation attempt.

## Timing
Second impact has:
- weak early window,
- perfect detonation window,
- expiration.

## Targeting
Same marked target unless player intentionally cancels.

## Hit geometry
Two real melee contacts.

## Skill expression
Hit confirm + rhythm + spacing.

## Risk
Missed second hit loses detonation.

## Counterplay
Target can disengage during mark duration.

## VFX
Subtle impact mark -> compressed distortion burst.
No giant persistent circle.

## Monster rules
SMALL: burst damage, little launch.
HUMANOID: stagger.
LARGE: major poise damage, minimal displacement.

## Evolution
Dual Impact -> Resonant Impact -> stored multi-mark / Counterforge interaction.

## Acceptance
Player must clearly understand "first hit prepared / second hit detonated".

---

# CORE 04 — PILE BREAKER

Status target: SEMI-COOKED.

## Fantasy
Compact power is loaded into one committed body-driven strike.

## Function
Charge / break / heavy stagger.

## Input
Hold to charge; release to strike.
Aim remains adjustable during charge within limits.

## Movement
Short committed burst on release.

## Skill expression
Charge duration + aim + spacing + release timing.

## Risk
Large recovery on miss.

## Counterplay
Readable wind-up; dodge or interrupt before armor threshold.

## VFX
Energy compression around fist/weapon arm.
Distortion at contact.
Ground shock only on heavy threshold.

## Monster rules
SMALL: launch.
HUMANOID: heavy knockback/stagger.
LARGE: poise break only unless broken state.

## Evolution
Pile Breaker -> Human Pile concept -> charged counter-fusion.

## Acceptance
Must feel heavy without needing particle spam.

---

# CORE 05 — CADENA SIN LIMITE

Status target: SEMI-COOKED.

## Fantasy
A combo that survives only while the player keeps executing correctly.

## Function
Sustained offense.

## Input
Repeated timed inputs.
Directional input may redirect between links.

## Rule
No autoplay.
Each continuation must be earned.

## Skill expression
Timing, direction, target choice, stamina management.

## Risk
Chain ends on mistime, whiff or resource exhaustion.

## Counterplay
Movement, poise response, defensive interruption.

## VFX
Library of distinct slash silhouettes.
No repeated identical cut.

## Monster rules
SMALL: reduced correction.
HUMANOID: full chain.
LARGE: chain remains possible but displacement reduced.

## Evolution
Chain -> Branching Chain -> aerial branch -> shadow echo branch.

## Acceptance
At least 5 visually distinct cut profiles.

---

# CORE 06 — SEVEN SINKER REFORGED

Status target: PROTOTYPE -> SEMI-COOKED.

## Fantasy
Seven remote weapons create a lethal, breakable formation.

## Function
Placement / zoning / control.

## Input
Hold enters placement.
Aim chooses formation center.
Release deploys.

## Targeting
Ground/space placement, not mandatory target lock.

## Rule
Seven weapons are actual interactable threats.
Formation has openings if weapons are destroyed or avoided.

## Skill expression
Placement, orientation, timing, follow-up.

## Counterplay
Leave zone, break a weapon, exploit opening.

## VFX
Actual equipped-weapon identity where practical.
Trails only during motion.
Closing impact uses distortion, not generic explosion.

## Monster rules
SMALL: tighter radius.
HUMANOID: standard prison.
LARGE: widened formation; no clipping through body.

## Evolution
Remote blades -> formation control -> Seven Sinker -> Shadow Arsenal fusion.

## Acceptance
Formation must read spatially without UI text.

---

# CORE 07 — SCRAP & BUILD REFORGED

Status target: PROTOTYPE.

## Fantasy
Destroy/sacrifice current weapon state to rebuild the hunter into a temporary combat mode.

## Function
Risk/reward transformation buff.

## Input
Hold confirm prevents accidental sacrifice.

## Mechanic
Weapon visibly fragments.
Fragments are absorbed.
Temporary combat modifier activates.
Weapon returns after duration / rule completion.

LAB never permanently deletes inventory.

## Skill expression
When to sacrifice, which weapon identity, how to exploit temporary state.

## Risk
Temporary loss of normal weapon behavior and meaningful cooldown.

## VFX
Visible weapon fragmentation -> absorption -> altered trail/aura.
No orange placeholder rectangle.

## Evolution
Scrap & Build -> weapon-specific rebuild modes -> release fusion.

## Acceptance
Player must understand what was sacrificed and what changed.

---

# CORE 08 — ARTE DEL SACRIFICIO

Status target: PROTOTYPE -> SEMI-COOKED.

## Fantasy
The hunter physically throws the equipped weapon and turns its landing point into a tactical threat.

## Function
Projectile / placement / delayed detonation.

## Input
Hold = aim trajectory.
Release = throw.
Second input = detonate / recall depending state.

## Targeting
Trajectory, not auto-hit.

## Skill expression
Prediction, geometry, detonation timing.

## Risk
Weapon unavailable while committed.

## Counterplay
Dodge trajectory, leave detonation area.

## VFX
Actual weapon visible in flight.
Single coherent trail.
Impact embed.
One coherent detonation family.

## Monster rules
SMALL: direct-hit launch allowed.
HUMANOID: embed/impact.
LARGE: weapon hits body/surface; reduced displacement.

## Evolution
Throw -> remote detonation -> recall slash -> Future Cut fusion.

## Acceptance
No fake orange block, no particle salad.

---

# CORE 09 — COUNTERFORGE

Status target: DESIGN -> SEMI-COOKED.

## Fantasy
A perfect defense forges the enemy's force into your next chosen attack.

## Function
Perfect parry / stored counter.

## Input
Parry timing window.
Successful perfect parry creates one stored charge.
Player chooses when to spend it.

## Skill expression
Read attack + parry timing + delayed decision of where to cash out.

## Risk
Missed parry is punishable.
Stored charge expires.

## Counterplay
Feints, delays, multi-hit pressure.

## VFX
Sharp parry flash + compressed shock.
Stored charge changes weapon trail subtly.
Spend produces amplified impact.

## Monster rules
SMALL/HUMANOID: stagger on perfect parry.
LARGE: poise damage, not full stagger unless threshold breaks.

## Evolution
Parry -> Counterforge -> multi-charge / shadow echo counter.

## Acceptance
No automatic counterattack. Player owns the release timing.

---

# CORE 10 — EDGE RUNNER

Status target: PROTOTYPE -> SEMI-COOKED.

## Fantasy
Parkour and aerial combat are one continuous combat language.

## Function
Wall movement / pursuit / aerial bridge.

## Input
Jump near valid wall + directional intent.
Follow-up attack/skill while airborne.

## Movement
- wall jump,
- directional rebound,
- aerial pursuit,
- controlled landing,
- future climb/vault hooks.

## Animation
UAL2 candidates:
- NinjaJump_Start
- NinjaJump_Idle_Loop
- NinjaJump_Land
- ClimbUp_1m
- Slide_Start / Loop / Exit
- Sword_Dash for attack transition when appropriate

No random flip chance.

## Skill expression
Approach angle, wall choice, directional input, aerial follow-up.

## Risk
Bad approach loses momentum.
No infinite wall spam.

## Counterplay
Predict trajectory / deny wall route.

## Monster rules
Used to attack all three scales without camera collapse.

## Evolution
Wall Jump -> Edge Runner -> Aerial Chain -> Shadow Relay aerial fusion.

## Acceptance
Jump/landing must feel intentional, with no strange crouched residue.

---

# Premium implementation order

1. Danza Gemela Reforged
2. Drift de Formula
3. Impacto Dual
4. Pile Breaker
5. Cadena Sin Limite
6. Seven Sinker Reforged
7. Scrap & Build Reforged
8. Arte del Sacrificio
9. Counterforge
10. Edge Runner

We do not add skill 11 until these ten have passed the three-target LAB matrix.
