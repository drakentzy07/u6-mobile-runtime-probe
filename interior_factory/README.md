# HIGHFLY — AI Interior Factory

Branch laboratory for data-driven, asset-grounded interior generation.

## Non-negotiable rules
- REUSE FIRST: only catalogued, verified assets may be selected.
- No WORLD/exterior mutation from this lab.
- No Combat Core mutation from this lab.
- Every generated interior must pass Auto-QA before it can be exported.
- Raw third-party art is not committed here; the catalog stores provenance and local/source paths only.

## Run 0A — Forge seed
First benchmark: `HF_SMITHY_01` (Forja Definitiva).

Human donor references verified from HIGHFLY's library:
- EmacEArt `EA_BlackSmith_B_PRE` — 37 mapped prefab dependencies.
- EmacEArt `EA_Room_Int_Store_Comp_7x7_01a_PRE` — store composition reference.
- AnyRPG blacksmith/crafting/vendor/recipe patterns (functional donor; art reviewed per asset).
- KayKit RPG Tools / Resource Bits, Fantasy Armory, RG Poly and Quaternius as approved catalog sources.

Pipeline:
`Catalog -> InteriorSpec -> Generator -> Auto-QA -> Preview -> Approved Export`

Run 0A currently validates the two-floor topology and deterministic seed placement. The first generated layout was rejected because forge/anvil distance was 4.22m; after correction it passes the current QA gate.

Next gates:
1. Expand full asset index from verified local packs.
2. Add bounding boxes / pivots / footprint / clearance metadata.
3. Add overlap, door, stair, circulation and interaction-reach QA.
4. Instantiate actual prefabs in isolated Unity preview scene.
5. Reuse HIGHFLY PC WASD + mobile movement/camera for WebGL validation.
