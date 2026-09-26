# HIGHFLY — MONSTER ASSET AUDIT v2 — QUALITY + PROVENANCE GATE

Status: CORRECTED / PASS 02 (26 Sep 2026)
Doctrine: QUALITY FIRST + REUSE FIRST + PROVENANCE GATE + MOBILE GATE

> IMPORTANT CORRECTION
> An earlier pass treated several visually strong Fab free samples as potential shipping candidates from listing metadata alone.
> Binary inspection of the actual uploaded FBX files later exposed explicit Rappelz source paths and .nx3 identifiers.
> Those assets are now REJECT_FOR_SHIPPING / REFERENCE_ONLY.
> Binary provenance overrides storefront appearance/claims.

This audit does not approve reward/drop behavior. Monster assets remain independent from Loot authority.

## Shipping gate

A main HIGHFLY asset must pass ALL of:
1. Visual quality bar.
2. Clear commercial-use license.
3. Provenance inspection of the downloaded binary/package.
4. Rig / animation path.
5. Unity integration path.
6. S23 Ultra mobile budget or credible optimization path.

Free is not enough.
A storefront license is not enough if the delivered binary contains third-party provenance red flags.

## Current strong candidates still alive

### HUNTER_ROGUE_KHEMPAVEE
Files inspected:
- rogue_all.fbx
- rogue_free.ma
- textures_and_weapons.zip

Observed:
- modular humanoid
- approximately 22k-triangle class per source listing
- extensive humanoid skeleton
- 4K/2K texture source set
- weapon files
- no Rappelz-style internal source-path red flag found in current inspection

Decision:
- QUALITY: PASS
- ROLE: HUNTER SUPREME BAKE-OFF
- SHIPPING: CANDIDATE
- NEXT: animation/weapon/mobile runtime test + final license snapshot

### GOBLIN_ASTER_FORGE_FREE
Source:
- free_stylized_goblin.zip

Decision:
- QUALITY: PASS
- ROLE: primary Goblin candidate
- SHIPPING: CANDIDATE, pending final binary/license snapshot
- LOWPO remains placeholder/crowd/LOD support

### PIXELIUS_MONSTER03
Source:
- Monster03Shop.rar
- contains Monster03_AllAnim FBX/GLB and texture variants

Decision:
- QUALITY: PASS
- ROLE: Abyssal Hive / Jeju soldier-elite candidate
- SHIPPING: CANDIDATE, exact-license snapshot still required

### PIXELIUS_MONSTER05
Source:
- Monster05Shop.rar

Decision:
- QUALITY: PASS
- ROLE: Void / Alien / Corrupted Unique Scenario lineage
- SHIPPING: CANDIDATE, exact-license snapshot still required

### TREANT_FREE_PACK
Source:
- Treant Package.7z

Decision:
- QUALITY: PROMISING
- ROLE: magical/corrupted forest family
- LICENSE: CC0 source path previously verified
- NEXT: in-engine style/material validation

### DARK_FANTASY_SKELETON_SAMPLE
Exact supplied GLB/FBX variants inspected:
- approximately 9.4k triangles
- static converted files: no skin/animation in these variants

Decision:
- QUALITY: PASS candidate
- CURRENT FILES: NOT RUNTIME READY
- KayKit Skeleton remains production-safe until original rigged file is obtained and audited

## Secondary / ecology / prototype

### QUATERNIUS EASY ENEMY
CC0, lightweight and animated.
Use: Spider/Snake/Wasp ecology, prototype and secondary populations.
Not main visual benchmark.

### GOBKIT
CC0 and extremely lightweight.
Use: ecology, ambient, swarms, distant mobs, prototype.

### LOWPO GOBLIN FREE
Use: placeholder, crowd, possible LOD-support.
Not current face of Goblin family.

### KENNEY GRAVEYARD
Use: crypt/graveyard environment and secondary entities.
Not main boss/family art.

### GIANT MUTANT
Use: reference/prototype only.
Visual bar and cost/benefit fail current main-family gate.

## REJECT_FOR_SHIPPING — proven provenance red flags

The following actual uploaded FBX files contain explicit internal paths beginning with:
F:\Rappelz\Rappelz All you need\...
and/or original .nx3 object identifiers.

They must NOT ship in HIGHFLY unless original rights/provenance are independently established.

- Mushroom Demon
  - alu_mushroom_demon_body.nx3
- Demon Bull
  - alu_bakun_body.nx3
- Harpy Assassin
  - harpy_body.nx3
- Crimson Knight
  - ain_ancientknight_honor.nx3
- Infernal Ram Sorcerer
  - baphometlv3_body.nx3
- Vaelith Celestial Sentinel
  - beast_asura0zero_lv3.nx3
- Abyssal Reaper
  - alu_galiark_body.nx3
- Corvus Reaper
  - primalscream_body.nx3
- Glacial Empress
  - kainenlv3_body.nx3
- Rakshasa Vorn
  - tamahakan_body.nx3
- Medusara
  - beast_medusa_body.nx3
- Bloodfrost Revenant
  - alu_kratus_body.nx3
  - FBX also references Rappelz SuperstarClient DDS path
- Bone Dragon upload
  - dragon_bonedragon.nx3
- White Dragon upload
  - whitedragonlv2_body01.nx3
- Mephisto-like upload
  - mephistolv3_body.nx3

Decision for every item above:
REJECT_FOR_SHIPPING / REFERENCE_ONLY.

Do not retarget, recolor or otherwise launder these into production assets.

## New externally verified acquisition candidates

These are NOT owned until downloaded. Store/license checks are only the first gate; binary audit remains mandatory after acquisition.

### RPG - Ogre Pack - PBR/Mobile — Unity Asset Store
- FREE
- Standard Unity Asset Store EULA
- PBR Ogre: ~11.7k tris
- mobile Ogre: ~4.8k tris
- humanoid-ready rig
- PBR/mobile texture variants
- no body animations (retarget required)

Decision:
- ACQUIRE
- intended role: OGRE family base
- confidence: HIGH at storefront/license gate

### Creep Horror Creature — Unity Asset Store
- FREE
- Standard Unity Asset Store EULA
- rigged / animated / PBR
- high and decimated mesh options reported by listing ecosystem
- strong dark-fantasy visual identity

Decision:
- ACQUIRE
- intended role: mutant/void/underground Unique lineage, not forced into an existing family

### 01_Monster: Lizard — Unity Asset Store
- FREE
- Standard Unity Asset Store EULA
- rigged / animated / game-ready
- Unity 2022.3-compatible listing

Decision:
- ACQUIRE FOR AUDIT
- role: Lizard beast / possible lower-tier reptilian ecology
- does NOT automatically close premium Lizardman humanoid family

### FREE - Modular Character - Fantasy RPG Human Male — Unity Asset Store
- FREE
- Standard Unity Asset Store EULA
- PBR / stylized / modular
- render-pipeline compatible

Decision:
- ACQUIRE ONLY AS HUNTER COMPARISON / modular donor
- visual bar must be tested against Khempavee Rogue and current KayKit

### Assassin-Thief-Rogue - Rigged — Fab
- currently listed FREE
- FBX / GLB / OBJ / BLEND
- male realistic fantasy rogue
- rigged and animation-ready
- generated-with-AI flag: No

Decision:
- ACQUIRE ONLY IF download flow explicitly grants Fab Standard License
- then run binary provenance audit before any HIGHFLY use
- role: Hunter Supreme bake-off candidate

### Stylized Player Character (Free) — Fab
- listed FREE
- Unity Humanoid / Mixamo compatible
- 2K/4K PBR
- Idle / Walk / Run
- Unity pipeline support
- generated-with-AI flag: No

Decision:
- ACQUIRE ONLY IF Fab Standard License is explicitly granted
- role: Hunter comparison candidate
- visual bar still needs direct side-by-side test

## Remaining premium-quality gaps

Still OPEN:
- Jeju King / Queen / Royal caste
- premium Lycanthrope
- premium Centipede
- true premium Lizardman humanoid
- premium Giant identity
- final Hunter Supreme winner

New likely closure:
- Ogre has a high-confidence free acquisition path via Unity Asset Store.

Rule:
VACANT > MEDIOCRE > LEGALLY DOUBTFUL.
Do not fill a family merely to complete the matrix.


## PASS 03 — Library cleanup + acquisition shortlist (26 Sep 2026)

Persistent Library cleanup was performed after user approval:
- 35 rejected/redundant assets removed from Library
- approximately 1.61 GB reclaimed
- removals targeted Rappelz-provenance rejects, redundant format conversions, Giant Mutant and static skeleton conversions
- conversation attachments may still exist in chat history; cleanup refers to persistent Library entries

Kept intentionally:
- rogue_all.fbx + textures_and_weapons.zip
- free_stylized_goblin.zip
- Monster03Shop.rar
- Monster05Shop.rar
- Treant Package.7z
- LOWPO Goblin
- Quaternius Easy Enemy
- Gobkit ecology packs
- Kenney Graveyard

Monster03 / Monster05 status correction:
- exact packages are kept in QUARANTINE/CANDIDATE state
- their itch pages confirm free/name-your-price, rig/animation specs and no generative AI flag
- exact commercial-use license for those two specific 3D packages has NOT yet been captured
- do not ship until exact-license evidence is stored

Externally verified FREE + Standard Unity Asset Store EULA shortlist:
1. RPG - Ogre Pack - PBR/Mobile
   - role: Ogre family candidate
2. Creep Horror Creature
   - role: Void / underground / extraordinary creature
3. 01_Monster: Lizard
   - role: reptile ecology / lower-tier reptilian creature
   - does not close humanoid Lizardman family
4. [Free] Fantasy Monster 10 (Rig + Animation) – PixeliusVita
   - role: legally-clean Pixelius benchmark / optional fantasy creature candidate

Hunter:
- Khempavee Rogue remains current strongest free candidate
- do not download lower-quality modular humans merely to increase candidate count

Open quality gaps remain:
- premium Lycanthrope
- Jeju King / Queen / Royal caste
- premium Centipede
- true humanoid Lizardman
- premium Giant identity

Policy remains:
VACANT > MEDIOCRE > LEGALLY DOUBTFUL.


## PASS 04 — Compatibility-first Hunter + safe monster expansion

Hunter search policy changed:
VISUAL QUALITY is no longer the first gate for the player character.
For the Skill Lab bottleneck, priority is:
1. Unity Humanoid / Mecanim or clean Mixamo-compatible humanoid rig
2. correct hand/finger bones
3. T-pose/A-pose and stable retargeting
4. predictable weapon sockets / IK compatibility
5. clear commercial license
6. mobile viability
7. visual quality

Compatibility candidates:
- Synty Sidekick FREE Starter Pack
  - FREE / Standard Unity Asset Store EULA
  - fully rigged Unity Humanoid / Mecanim
  - modular human parts
  - Unity 2021.3+, URP/Built-in
  - can bake modular parts into one optimized prefab
  - primary compatibility-first candidate

- LOWPO Adventure Character Pack
  - CC0
  - free Basic Adventurer / Healer / Monk
  - standard FBX
  - fully rigged humanoid T-pose
  - Mixamo compatible
  - Basic Adventurer includes sword
  - strong calibration candidate

- Stylized Player Character (Free)
  - FREE / Standard Unity Asset Store EULA
  - Humanoid rig compatible with Unity + Mixamo
  - Idle / Walk / Run in-place
  - PBR 2K/4K
  - Built-in / URP / HDRP
  - candidate for appearance + compatibility test

- Low Poly 3D Male Knight Fantasy Character
  - FREE / Standard Unity Asset Store EULA
  - humanoid-rig tagged
  - 2.9 MB
  - very lightweight compatibility fallback

- Shyr Base Character
  - CC0
  - fully rigged humanoid
  - explicitly Mixamo-compatible
  - includes armored sword character + attacks
  - use as CONTROL RIG, not necessarily final art

Weapon-grip architecture requirement:
- normalize all weapons to canonical local forward/up axes
- each weapon gets WeaponGripProfile:
  RightGrip pose
  LeftGrip pose
  Shield/Forearm pose
  category offset
- one-hand weapons parent to primary hand socket
- dual weapons use independent left/right grip profiles
- two-hand weapons parent to primary hand and use left-hand IK against weapon LeftGrip target
- shields use dedicated left-hand/forearm profile
- do not encode weapon correction into individual attack clips

If the CC0/Mecanim control character shows the same grip defect, the bug is in HIGHFLY attachment/IK/weapon-axis logic, not in character art.

Safe free monster shortlist with clear license:
- RPG Ogre Pack PBR/Mobile — FREE / Standard Unity Asset Store EULA
- Creep Horror Creature — FREE / Standard Unity Asset Store EULA
- 01_Monster: Lizard — FREE / Standard Unity Asset Store EULA
- [Free] Fantasy Monster 10 (Rig + Animation) — FREE / Standard Unity Asset Store EULA
- Stylized Zombie (Low Poly) — FREE / Standard Unity Asset Store EULA
- Undead Skeleton Enemies — FREE / Standard Unity Asset Store EULA

Continue using already-owned CC0:
- KayKit Skeletons
- Quaternius Ultimate Monsters
- Quaternius Easy Enemy
- Quaternius Animated Animals
- Treant CC0
- Gobkit ecology

No need to download duplicates merely to increase asset count.
