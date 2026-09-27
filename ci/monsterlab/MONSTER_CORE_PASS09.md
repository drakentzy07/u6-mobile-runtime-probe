# HIGHFLY MONSTER CORE — PASS 09 IMPLEMENTATION STATUS

Commit target: highfly/monster-run0-goblin-core

## Implemented in this pass
- EncounterInstance strict lifecycle DORMANT -> RESERVED -> SPAWNING -> ACTIVE -> RESOLVING -> RESOLVED.
- ABANDONED terminal path from RESERVED/SPAWNING/ACTIVE.
- Population reservation acquired before spawn and released exactly once.
- NemesisLineageStore keyed by lineageId, independent of GameObject lifetime.
- Monster-owned Nemesis facts: encounters, escapes, survived defeats, zone, memory flags, permanent part facts, evolution history.
- Deterministic EvolutionEvaluator using Monster-owned predicates only; priority + ruleId tie-break.
- BossGraphDefinition and validator for duplicate/missing phases, invalid thinker reservation and dangling edges.
- UniqueScenarioRuntimeV2 driven by authored transitions/signals, no reward logic.
- Monster performance sample/accumulator skeleton for target-device profiling.

## Deliberately not claimed
- No MonoBehaviour bridge to production agents yet.
- No persistence serializer/save integration for Nemesis yet.
- Boss graph validates structure but does not yet evaluate rich phase predicates.
- Scenario graph has runtime transitions but no arena/world adapter yet.
- No S23 measurements have been collected.
- No Loot tables/reward selection were added.

## Next independent slices
1. Generic agent bridge: MonsterDefinitionV2 -> runtime agent without family conditionals.
2. Encounter adapter over existing PopulationLedger/SpawnScheduler.
3. Nemesis save DTO/versioning and migration guard.
4. Boss signal/predicate evaluator for health, anatomy and scenario facts.
5. Scenario validator: reachability, terminal-state checks and ambiguous signal edges.
6. Orc/Lycan/Dragon authoring fixtures using reserved canonical part IDs.
7. Profiling instrumentation hooks and lab overlay.
8. Asset gap casting remains separate from runtime approval.

## Loot boundary
No Loot decision is required. RewardContext v1.2 remains the factual output boundary. Monster owns part state and encounter/scenario facts; Loot owns item/material selection, quantities, rarity, recipes and economy.
