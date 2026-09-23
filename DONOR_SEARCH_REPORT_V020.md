# HIGHFLY — DONOR SEARCH REPORT v0.20

This report is a discovery/audit snapshot for DONOR LAB 2.0. It is not a claim that every listed skill is already playable in HIGHFLY.

## Current quantitative picture

- **Initial RAW target:** 10–12 distinct donor skills.
- **Strong directly traceable Unity donor blocks found now:** 9–11, before AnyRPG.
- **FULL + SEMI-FULL + candidate mechanic pool:** already 50+ distinct mechanics.
- **AnyRPG:** not counted yet; large package pending.
- **ClaudeCraft:** 15 previously identified skills are not counted as FULL while the source project is unavailable.
- **SubspaceHunter:** counted as SEMI-FULL because public code exposes the skill flow but presentation dependencies are mixed/missing.

## Strong first-wave donors

### Lucid / 3D-Action
- Jump Smash — native control specimen.

### Sigil Combat — MIT / Unity 6
- Dash Attack / Dash Slash
- Charged Fireball
- Flash / Blink
- Melee
- Ranged

### Mage Arena / DTF Hackathon — MIT / Unity
- Charged Fireball
- Particle Shield

### Dragon Souls — MIT code / Unity
- Sword Throw -> free/unarmed state -> Recall
- Mage projectile family
- Dragon fire projectile / fireball family

## High-value SEMI-FULL sources

### SubspaceHunter
- Fire
- Electric
- Ice
- Meteor
- Shield
- Heal
- Sword slash generation

Public source contains the gameplay scripts, but presentation dependencies must be traced/replaced individually.

### ArenaGame — GPL-3.0 code
Mage:
- Teleport Bolt / Teleport
- Thunderbolt
- Thunderstrike
- Storm Shield

Warrior:
- Mechanical Hook
- Charge
- Slash
- Upward Slash
- Boomerang

Archer:
- Trap
- Clawhook
- Dancing Arrows
- Hound-style ability

The mechanics are valuable, but literal code reuse requires GPL-compatible distribution. Imported art/VFX/audio are not assumed GPL.

### Broken Seals — cross-engine
Naturalist spell resources include:
- Aspect of Scorpions
- Aspect of Wasps
- Aspect of Wolves
- Aspect of Bees
- Amplify Pain
- Rejuvenation
- Close Wounds
- Ironbark
- Nature's Swiftness
- Uproot
- Root
- Strength of Nature
- Shield of Barbs
- Calm
- Attunement
- Inner Will
- Rest
- Strike
- Regrow

Cross-engine source: preserve mechanics/data relationships, translate runtime.

### Shades SpellSystem — CC BY-SA / Unity
Interesting mechanics include:
- Teleport
- Creature summons
- Homing/targeting projectiles
- Status effects
- Magic weapons
- Shield bash
- Multi-layer casting
- Physics spells
- Raise Dead
- Heal
- Pooled spell effects

The Invector dependency and ShareAlike obligations make it a controlled SEMI-FULL source, not a blind copy source.

### GASam — Unreal GAS
- Fireball
- Lightning Bolt

Cross-engine ability examples with FX.

### Matchstick Mage — MIT / Unity
- Spell combat block
- Bomb / throw / explosion block
RAW scene verification still required.

## Large but blocked/reference sources

### Magic Mayhem
Repo contains concrete ability prefab families for:
- Chain Lightning x3
- Fireball x3
- Healing Aura x3
- Ice Block
- Ice Cone x3
- Magic Missile x3
- Meteor Strike x3
- Shield variants
- Scatter Shot variants

But the repository has no clear project-level license and mixes Mixamo / Synty / Asset Store content. Treat as autopsy/reference until permissions are proven.

### GenshinGamePlay
MIT code and excellent Ability / Modifier / Mixin / Timeline architecture.
Genshin/Keqing sample presentation assets are excluded from donor use.

### Warcraft Arena Unity
Large class/spell kit discovered, but license and presentation provenance need verification before promotion.

## Visual/presentation library already owned by HIGHFLY

Use these only to replace missing/unlicensed donor dependencies, not to redesign a donor unnecessarily:
- Human Spellcasting Animations FREE
- Human Melee Animations FREE
- Human Throwing Animations FREE
- Human Basic Motions FREE
- DoubleL RPG Animations
- EEJANAI sword animations
- Gabriel Free Quick Effects URP
- Hit Effects FREE
- AoE FREE
- Status & Auras FREE
- Orbs FREE
- Magic Circle URP
- Stylized Slash
- Distortion Shockwaves
- Easy Impact Frames URP
- Water Spell
- Effekseer Essentials
- Hero Beam
- Gravity
- Basic Effects Pack
- Nowis337 effects

## Rule for the playable DONOR LAB

A registry entry does NOT get a playable button merely because it was found.

A button appears only after:
1. RAW donor path is executable or faithfully reconstructable from complete source.
2. Dependency graph is known.
3. License gate passes.
4. Required assets are available.
5. HIGHFLY adapter is minimal and does not rewrite the skill identity.
