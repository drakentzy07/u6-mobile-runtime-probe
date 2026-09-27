# HIGHFLY Monster Core

Implemented modules on this branch:
- HighflyMonsterCore.cs — legacy Goblin Run0 agent/runtime.
- HighflyMonsterAuthoring.cs — tuning, encounter/ecology authoring, validation and RewardContext v1.2 factual DTO.
- HighflyMonsterRuntimeSystems.cs — AI intent, evolution rule, boss/scenario runtime, asset registry.
- HighflyMonsterOrchestration.cs — population reservation, spawn scheduler, encounter resolution.
- HighflyMonsterGenericRuntime.cs — generic MonsterDefinitionV2 registry/reference resolver, cross-reference validator, family-agnostic SLEEPING/OBSERVER/THINKER assignment and Skeleton family fixture.

Implemented in Pass 09:
- M07-A generic cross-reference validator: implemented initial executable slice.
- M07-B generic definition registry/resolver: implemented initial executable slice.
- M07-C family-agnostic simulation-tier assignment: implemented initial executable slice. Bosses and engaged monsters receive deterministic priority; overflow degrades rather than despawns.
- M07-D Skeleton fixture: implemented authoring fixture for SKELETON_WARRIOR, SKELETON_GUARD, SKELETON_LICH and WRAITH. It exercises the generic path and contains no Loot interpretation.

Still design/backlog, NOT claimed implemented:
- generic MonoBehaviour agent replacing GoblinAgent;
- encounter lifecycle RESERVED/SPAWNING/RESOLVING state machine;
- persistent Nemesis lineage store and permanent part-state overrides;
- generic evolution graph/cycle validator;
- conditional boss phase graph;
- full Unique Scenario INTRO/CLIMAX/RESOLUTION graph;
- profiler telemetry and measured S23 budgets;
- Orc/Lycan/Dragon runtime fixtures.

Authority boundary remains unchanged: Monster publishes facts/anatomy/encounter state; Loot alone interprets rewards under RewardContext v1.2.

Next independent slice:
1. generic encounter instance + idempotent population reservation/release;
2. lineage Nemesis store + deterministic evolution evaluator;
3. boss/scenario graph validators;
4. profiling counters and lab assertions.

No new Loot decision is required by this status.
