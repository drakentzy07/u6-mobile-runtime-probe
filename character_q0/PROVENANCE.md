# HIGHFLY CHARACTER Q0 — Provenance / Freeze

## GOLD baseline
- Repo: drakentzy07/u6-mobile-runtime-probe
- Frozen rollback branch: highfly/affinity-gold-pre-character-q0
- Baseline commit: d25e53ea642235c2e6b6b140995ee9b9358a2720
- Baseline GREEN run: 37227101607
- Experiment branch: highfly/character-q0

## Quaternius donors
Source: user Library /PACKS/Universal Base Characters[Standard].zip
License file in pack: License_Standard.txt
License: CC0 1.0 Universal / public-domain dedication
Creator attribution in pack: Quaternius (@Quaternius)

Bodies used:
- Superhero_Male_FullBody
- Superhero_Female_FullBody

The separate /PACKS/Superhero.zip is NOT used as a body donor; it contains Mixamo animation files.

For the Q0 WebGL experiment the source GLBs are local exports of the above bodies with:
- geometry preserved;
- base-color texture retained at 512px;
- normal/roughness donor maps omitted only to keep this isolated test branch lightweight.

No downloaded, paid, Sidekick or GanzSe asset is used.

## Runtime rig contract
The donor skeleton is never a runtime skeleton.

Both bodies are rebuilt by ClaudeCraft's existing:
scripts/asset_pipeline/lib/manual_rig.mjs

against:
public/models/chars/players/knight.glb

Required runtime skeleton:
KayKit / ClaudeCraft Rig_Medium

Required joints include:
root, hips, spine, chest, head,
upperarm/lowerarm/wrist/hand on both sides,
handslot.r, handslot.l,
upperleg/lowerleg/foot/toes on both sides.

Q0 build must reject:
- leaked Quaternius runtime joints;
- missing hand slots;
- a joint vocabulary differing from the frozen Rig_Medium reference;
- a different animation count from the reference.

## Scope
Renderer-only A/B/C experiment:
CLAUDE | Q-MALE | Q-FEMALE

Never authoritative for damage, hit windows, cooldowns, resources, targeting,
movement, camera, collision, skill state or training stats.
