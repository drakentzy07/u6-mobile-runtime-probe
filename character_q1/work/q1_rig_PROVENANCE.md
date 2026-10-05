# HIGHFLY Character Q1 donor inputs

Source: Quaternius Universal Base Characters Kit, Standard FREE edition,
`Universal Base Characters[Standard].zip` supplied by the user.

Author: Quaternius (@Quaternius).
License: CC0 1.0 Universal, Public Domain Dedication, as stated in the
archive's `License_Standard.txt`.
Official source: https://quaternius.com
License: https://creativecommons.org/publicdomain/zero/1.0/

`q1_qmale_source.glb` contains the original Superhero_Male_FullBody geometry,
UVs, authored anatomical joint weights, donor skeleton and actual original
body, eyes and eyebrow materials. `q1_qfemale_source.glb` contains the matching
Superhero_Female_FullBody data. Donor textures are resized to at most 1024px
and encoded as WebP for reproducible lightweight input. Two absent optional
normal-map filenames in the vendor export are omitted; all actual base-color
textures are present. The materials are not white placeholders or recolors.

The source donor skeletons are offline build inputs. The shipping runtime
bodies keep ClaudeCraft's Rig_Medium joint names and complete clip coverage.
Q1 maps and merges the original authored anatomical weights, adapts bind
positions and local translation baselines to donor proportions, and preserves
canonical clip rotations, scale samples, key times and animation displacement.
This deliberate visual-only proportion adaptation replaces Q0's geometric
nearest-bone weighting. It does not alter simulation hitboxes, movement,
targeting, hit windows, damage, cooldowns or any gameplay authority.

Runtime imported class animation position tracks require the same proportion
retargeting. `src/highfly/character_q1_clip_retarget.ts` shifts their local rest
baseline while retaining timing, movement deltas, rotations and spline tangents.
Q1's native already-adapted GLB clips must not receive that shift a second time.

Build source: `overlays/build_highfly_character_q1.mjs`.
Build output report: `character-q1-rig-report.json`.
Offline animation deformation check: `overlays/q1_rig_pose_check.mjs`.
Actual in-game animation, grip and skill playback still require visual review.
