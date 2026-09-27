# HIGHFLY MONSTER CORE — PASS 07

Status: DESIGN CONTRACT / IMPLEMENTATION BACKLOG. Nothing in this file claims runtime implementation unless explicitly marked.

## 1. Authority boundary
Monster owns identity, anatomy state, AI intent, encounter state, ecology, variants/evolution, memory/nemesis, boss phases, scenario state and RewardContext facts.
Loot owns item/material selection, quantities, rarity, recipes and economy. Monster must never encode loot tables.

RewardContext v1.2 remains the only Monster -> Reward boundary. Existing partOutcomes values remain canonical: INTACT, DAMAGED, BROKEN, SEVERED, DESTROYED, HIDDEN, EXPOSED, DISRUPTED.

## 2. Authoring pipeline
Add a family-agnostic validation pass before a MonsterDefinition can become APPROVED:
- stable monsterFamilyId / definitionId / variantId
- referenced AI profile exists
- referenced anatomy profile exists
- every reward-relevant partId is unique inside the family
- bossDefinitionId only on boss definitions
- scenario restrictions reference known scenario IDs
- evolution graph is acyclic unless an edge is explicitly reversible
- population cost > 0 and zone caps are non-negative
- no Loot IDs, rarity, recipes, currency or drop chance fields are allowed in MonsterDefinition

Validation severity: ERROR blocks APPROVED; WARNING allows lab use; INFO is authoring guidance.

## 3. Runtime lifecycle
Canonical encounter lifecycle:
DORMANT -> RESERVED -> SPAWNING -> ACTIVE -> RESOLVING -> RESOLVED
with ABANDONED as a terminal escape path from RESERVED/SPAWNING/ACTIVE when the scenario explicitly permits it.

Transitions must be idempotent. Population reservation is acquired at RESERVED and released exactly once at RESOLVED/ABANDONED.

## 4. AI scalability
Use three simulation tiers:
- SLEEPING: no per-frame decision loop; population/ecology only.
- OBSERVER: low-frequency perception/state refresh; no expensive path/attack planning.
- THINKER: full combat decision runtime.

Initial mobile budget until measured on target hardware:
- recommended concurrent THINKER budget: 6
- recommended visible combatants: 8
- hard visible cap: 12
These are profiling baselines, not final performance guarantees.

Bosses reserve thinker budget before ambient mobs. Overflow mobs degrade THINKER -> OBSERVER rather than disappearing.

## 5. Ecology / population
PopulationLedger remains authoritative for capacity. Extend authoring with:
- populationCost
- habitatTags
- activityWindow tags
- socialGroup min/max
- predator/prey pressure tags
- respawn policy owned by World/Scenario configuration, not Loot

SpawnScheduler consumes capacity and habitat facts; it must not inspect rewards.

## 6. Variants and evolution
VariantDefinition should be a delta over a base MonsterDefinition, not a duplicated monster.
Allowed deltas: visual profile, stat/combat profile reference, AI tendency modifiers, anatomy visibility/exposure defaults, habitat tags, encounter weight and evolution metadata.
Forbidden deltas: loot tables/economy.

EvolutionEdge:
sourceVariantId, targetVariantId, triggerFacts, minimumEncounterCount, cooldown, reversible.
Triggers use Monster-owned facts only (survived encounter, escaped, body part loss, repeated damage archetype, territory pressure, boss promotion).

## 7. Nemesis-lite memory
Persist by lineage identity, never GameObject instance.
Minimal record:
lineageId, familyId, currentVariantId, encountersSeen, wins, escapes, defeatsSurvived, lastZoneId, learnedThreatTags, permanentPartStateOverrides, evolutionHistory.

No generated dialogue system required. Memory affects AI tendency/evolution/scenario presentation only.
RewardContext may receive firstKill/variant/scenario/performance facts already in contract; Nemesis does not directly choose rewards.

## 8. Boss runtime
BossDefinition composes MonsterDefinition plus:
- ordered/conditional phase graph
- phase entry predicates
- ability-set references
- arena/scenario signal bindings
- break/expose gates
- thinkerBudgetReservation

Boss phase changes publish facts/signals; they do not award items.
Part destruction may open a phase or disable an ability, while partOutcomes remains the reward-facing anatomical fact.

## 9. Unique Scenario runtime
Scenario owns arena rules, waves, environmental hazards, portals, scripted encounter gates and completion conditions.
Monster owns monster behavior inside those rules.
World owns travel/loading.
Loot consumes final RewardContext only.

Scenario state machine baseline:
LOCKED -> INTRO -> ACTIVE -> CLIMAX -> RESOLUTION -> EXIT
with FAIL/RETREAT branches where authored.

## 10. Family rollout
Implementation order after Goblin base:
P0 Skeleton, Orc, Lycanthrope, Dragon.
P1 Centipede, Abyssal Hive Royal/Queen, Ogre, Giant, Deep Serpent Boss.

Each family requires Definition + Anatomy + AI profile + Encounter profile + population cost + validator fixture before runtime approval.

Known reward-relevant Pass 02 IDs remain reserved:
ORC_SHIELD, ORC_WAR_HELM, ORC_SIEGE_BANNER;
DRAGON_HORN_L/R, DRAGON_WING_L/R, DRAGON_TAIL, DRAGON_ELEMENTAL_CORE;
LYCAN_CURSE_NODE.
Monster defines state/meaning; Loot decides resulting rewards.

## 11. Asset gate
Reuse-first:
APPROVED candidates already audited: Ogre, Monster_4, Undead Skeletons, Pixelius Monster10, Lizard, Creep Horror; Mushroom secondary; Black Hole donor with mobile-lite requirement.
Open marquee gaps: premium Lycanthrope, Jeju Royal/King/Queen caste, Centipede, humanoid Lizardman, premium Giant, premium Orc pending legal validation, Vampire humanoid, Ice Elf, Demon Noble.

An asset cannot become FINAL from visual fit alone. Gate requires provenance/license, rig/animation suitability, mobile cost, shader compatibility, silhouette/role fit and integration test.

## 12. Next implementation slices
M07-A: generic validator + validation report.
M07-B: generic runtime definition registry and reference resolver.
M07-C: simulation-tier scheduler with deterministic promotion/demotion.
M07-D: Skeleton family fixture exercising generic path.
M07-E: Nemesis record + deterministic evolution evaluator.
M07-F: Boss phase graph validator/runtime.
M07-G: Unique Scenario state graph validator/runtime.
M07-H: profiling counters: active thinkers, observers, visible monsters, decision ms, spawn ms, animation ms.

No new Loot decision is required by this pass.
