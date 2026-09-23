# HIGHFLY DONOR LAB 2.0 — Intake Manifest

## Rule
A donor skill is counted only when it can be executed or reconstructed from its original project with a traceable dependency graph.

- FULL: original animation + logic + VFX/impact/timing are reusable under compatible licenses.
- SEMI-FULL: original skill logic/timing are reusable; one or more presentation dependencies must be replaced.
- INSPIRED: only the mechanic/concept is reused. Does not count as a donor skill.

## Wave 1 targets

### Dragon Souls — target 4–6
- Sword Throw / Recall
- Fist combat sequence
- Heal
- Mage projectile
- Dragon projectile / boss ranged attack
- Dodge/roll interaction as utility donor

### SubspaceHunter — target 6–7 (mostly SEMI-FULL until asset dependencies are cleared)
- Fire
- Electric/Thunder
- Ice
- Meteor
- Shield
- Heal
- Sword Slash generation

### ClaudeCraft — target 10–15 once full source is mounted
Warrior: Reaver Strike, Charge, Thunder Clap, Whirlwind, Heroic Leap
Rogue: Sinister Strike, Garrote, Shadowstep, Eviscerate, Vanish
Mage: Cinderbolt, Fire Blast, Blink, Frost Nova, Meteor

### Sigil Combat — target 5
- Dash Slash
- Charged Fireball
- Flash/Blink
- Melee attack
- Ranged attack

### WizardSurvival — target 5–6
- Fireball
- Ice Blast
- Forcefield
- Wind
- Arc
- Fire Shield / Fire Arc variants

### GenshinGamePlay — target 6 SEMI-FULL technical donors
- NormalAttack 1–5 timeline/ability structure
- Skill1 timeline/ability structure
Do not reuse proprietary Genshin character/assets.

### Gameplay Ability System Sample — target 3
- Attack 1
- Attack 2
- Attack 3

### AnyRPG — pending package audit
No count until the full package is mounted and inspected.

## Gate before import
1. Verify repository/package license.
2. Trace every animation/VFX/audio/model dependency.
3. Run donor RAW if possible.
4. Copy the whole unit before adapting.
5. Replace only dependencies that cannot be legally/technically reused.
6. Retarget to HIGHFLY character.
7. Connect via HIGHFLY Adapter for input, targeting, damage, resources and cooldown.
8. User test: KEEP / REJECT.

## First measurable goal
20+ verified playable FULL/SEMI-FULL donor skills in DONOR LAB 2.0 without modifying the stable APP.
