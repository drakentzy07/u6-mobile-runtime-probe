# HIGHFLY — MONSTER ASSET AUDIT v2 — QUALITY GATE

Status: PASS 01 complete (uploaded batch, 26 Sep 2026)
Doctrine: QUALITY FIRST + REUSE FIRST + PROVENANCE GATE + MOBILE GATE

This audit does not approve reward/drop behavior. Monster assets remain independent from Loot authority.

## Quality policy

A technically usable free model is not enough for HIGHFLY's main bestiary.

Shipping/main-family candidates must pass:
1. Visual bar appropriate for HIGHFLY.
2. Clear provenance / license suitable for the project.
3. Rig / animation path.
4. Unity integration path.
5. S23 Ultra mobile budget or a credible optimization path.

Assets that fail visual quality remain SECONDARY / ECOLOGY / PLACEHOLDER even if technically excellent.
Assets with provenance red flags are REJECT_FOR_SHIPPING until provenance is independently proven.

## Strong candidates

### HUNTER_ROGUE_KHEMPAVEE
Source files inspected:
- rogue_all.fbx
- rogue_free.ma
- textures_and_weapons.zip

Observed:
- modular character
- ~22k-triangle class (source listing)
- extensive humanoid skeleton in supplied files
- 4K/2K texture source set
- weapon files available
- source listing declares fully rigged / retargetable and free commercial/personal use

Decision:
- QUALITY: PASS
- ROLE: HUNTER SUPREME BAKE-OFF
- MOBILE: OPTIMIZE textures/materials/bone usage before final approval
- SHIPPING: CANDIDATE, capture final license snapshot before release

### GOBLIN_ASTER_FORGE_FREE
Source:
- free_stylized_goblin.zip

Observed:
- stylized PBR goblin
- Epic-skeleton style rig / retarget path
- 1024 PBR texture set
- significantly stronger visual identity than LOWPO placeholder goblins
- uploaded demo videos show animated crowd/retarget usage

Decision:
- QUALITY: PASS
- ROLE: primary Goblin family candidate
- MOBILE: likely viable, profile facial/extra rig cost and strip unused complexity
- SHIPPING: LICENSE_REVIEW_REQUIRED

### PIXELIUS_MONSTER03
Source:
- Monster03Shop.rar

Archive contains:
- Monster03_AllAnim.fbx
- Monster03_AllAnim.glb
- large texture-variant set

Publisher specification:
- 2,978 vertices
- 11 combat/locomotion animations
- 9 texture types x 7 color variants

Decision:
- QUALITY: PASS
- ROLE: Abyssal Hive / Jeju soldier or elite candidate
- FIT: strong armored insectoid silhouette
- SHIPPING: LICENSE_SNAPSHOT_REQUIRED for exact asset

### PIXELIUS_MONSTER05
Source:
- Monster05Shop.rar

Archive contains:
- Monster05_AllAnim.fbx
- Monster05_AllAnim.glb
- large texture-variant set

Publisher specification:
- 3,049 vertices
- 11 combat/locomotion animations
- 9 texture types x 7 color variants

Decision:
- QUALITY: PASS
- ROLE: Void / Alien / Corrupted unique-scenario lineage
- NOTE: do not force it into medieval Demon Noble if visual language does not fit
- SHIPPING: LICENSE_SNAPSHOT_REQUIRED for exact asset

### TREANT_FREE_PACK
Source:
- Treant Package.7z

Publisher specification:
- two rigged stylized tree monsters
- 10 animations
- approx. 5.4k / 7.3k triangles
- CC0

Decision:
- QUALITY: PROMISING
- ROLE: magical forest / corrupted forest / area monster
- NEXT: in-engine style and material validation

### DARK_FANTASY_SKELETON_SAMPLE
Files inspected:
- sm_skeleton_variant_1.glb
- sm_skeleton_variant_1.fbx
- glTF/USDZ alternatives

Observed in exact supplied GLB/FBX:
- ~9,402 triangles
- textures/materials present
- the exact supplied variants inspected are STATIC (no skin / no animation)

Publisher page advertises a rigged/Mecanim-humanoid package variant.

Decision:
- QUALITY: PASS candidate
- CURRENT FILE: NOT RUNTIME READY
- ACTION: obtain original rigged additional FBX/package, not another static conversion
- KayKit Skeleton remains current production-safe skeleton solution

## Secondary / ecology / prototype pool

### QUATERNIUS EASY ENEMY
- excellent lightweight topology/animation coverage
- Spider/Snake/Wasp useful
- CC0
- visual bar below main-family target

Decision: SECONDARY / ECOLOGY / PROTOTYPE. Keep.

### GOBKIT FREE / ANIMALS / DINOS
- ultra-light meshes
- rigged/baked animation coverage
- CC0
- very low GPU cost

Decision: ECOLOGY / SWARMS / AMBIENT / distant mobs / prototype. Not marquee monsters.

### LOWPO GOBLIN FREE
- Basic / Archer / Warrior
- lightweight humanoid rigs
- mobile-friendly
- supplied GLB characters have no embedded animations
- visual bar below new primary standard

Decision: PLACEHOLDER / crowd / LOD-support candidate. Do not use as the face of Goblin family if Aster candidate passes.

### KENNEY GRAVEYARD
- CC0
- useful undead/crypt environment and secondary entities

Decision: ENVIRONMENT / AMBIENT / PLACEHOLDER, not main boss/family art.

### GIANT MUTANT
Supplied GLB inspection:
- one skinned monster
- six animations
- ~78k indexed triangles in inspected representation
- source is CC0

Decision:
- MAIN QUALITY: FAIL
- MOBILE COST: HIGH for what it provides
- REFERENCE / PROTOTYPE only

## Provenance rejects

The following uploaded models contain internal identifiers/paths strongly associated with existing Rappelz content. They must not ship in HIGHFLY unless original rights/provenance can be independently established.

### BONE_DRAGON_UPLOAD
Inspected internal identifier:
- dragon_bonedragon.nx3

Rappelz publicly documents a Bone Dragon creature.

Decision: REJECT_FOR_SHIPPING / REFERENCE_ONLY.

### WHITE_DRAGON_UPLOAD
Inspected identifiers/path:
- whitedragonlv2_body01.nx3
- source path contains Rappelz project wording

Rappelz publicly documents the White Dragon evolutionary creature family.

Decision: REJECT_FOR_SHIPPING / REFERENCE_ONLY.

### MEPHISTO_UPLOAD
Inspected internal identifier:
- mephistolv3_body.nx3

Strong naming/provenance correlation to Rappelz content.

Decision: REJECT_FOR_SHIPPING / REFERENCE_ONLY until proven otherwise.

### GALIARK / REAPER-LIKE UPLOAD
Inspected internal identifier:
- alu_galiark_body.nx3

Decision: PROVENANCE_RED_FLAG. Do not approve for shipping without original-source proof.

## Current priority after this pass

P0:
- Hunter Rogue bake-off vs current KayKit visual
- Aster Goblin in-engine test
- Monster03 Hive/Jeju in-engine test
- obtain rigged version of the dark fantasy skeleton if license/source checks pass

P1:
- Monster05 Unique Scenario test
- Treant in-engine test

Still missing at HIGHFLY quality:
- true Jeju Queen / King-tier body
- premium Ogre
- premium Lycanthrope
- premium Centipede
- premium Lizardman
- premium Giant identity beyond generic scale-up

Do not fill these slots with low-quality assets merely because they are free.
