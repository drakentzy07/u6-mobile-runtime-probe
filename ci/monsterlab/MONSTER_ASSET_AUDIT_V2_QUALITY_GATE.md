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
