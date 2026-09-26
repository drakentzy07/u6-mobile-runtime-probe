using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public enum ThreatTier { MINION, VETERAN, ELITE, BOSS }
    public enum ScenarioKind { FIELD, AMBUSH, LAIR, BOSS_ARENA, UNIQUE }
    public enum VariantKind { BASE, HARDENED, ALPHA, EVOLVED }

    [Serializable] public sealed class MonsterTuning {
        public string id; public ThreatTier tier; public VariantKind variant;
        public float healthMultiplier=1f, speedMultiplier=1f, awarenessMultiplier=1f;
        public int populationCost=1; public bool mobileApproved=true;
    }

    [Serializable] public sealed class EncounterDefinition {
        public string encounterId, familyId; public ScenarioKind scenarioKind;
        public int minPopulation=1, maxPopulation=6, populationBudget=8;
        public float activationRadius=16f, leashRadius=28f;
        public bool uniqueScenario;
    }

    [Serializable] public sealed class EcologyDefinition {
        public string zoneId, familyId; public int softCap=8, hardCap=12;
        public float respawnCooldownSeconds=45f; public bool nocturnal;
    }

    [Serializable] public sealed class NemesisMemory {
        public string lineageKey; public int hunterEscapes, monsterEscapes, defeats;
        public float aggressionBias; public bool promoted;
        public void RecordMonsterEscape(){ monsterEscapes++; aggressionBias=Mathf.Min(.35f, aggressionBias+.05f); promoted |= monsterEscapes>=2; }
        public void RecordDefeat(){ defeats++; }
    }

    [Serializable] public sealed class UniqueScenarioDefinition {
        public string scenarioId, bossDefinitionId; public string biomeTag;
        public int populationBudget=14; public float arenaRadius=22f;
        public bool allowRetreat=true; public bool mobileApproved=true;
    }

    public sealed class ValidationReport {
        public readonly List<string> errors=new List<string>();
        public readonly List<string> warnings=new List<string>();
        public bool IsValid => errors.Count==0;
    }

    public static class MonsterAuthoringValidator
    {
        public static ValidationReport Validate(MonsterDefinition d, MonsterTuning t) {
            var r=new ValidationReport();
            if(d==null){r.errors.Add("definition:null"); return r;}
            if(string.IsNullOrWhiteSpace(d.monsterDefinitionId)) r.errors.Add("definition:id_missing");
            if(string.IsNullOrWhiteSpace(d.familyId)) r.errors.Add("definition:family_missing");
            if(d.maxHealth<=0) r.errors.Add("definition:health_nonpositive");
            if(d.engageRange<=0) r.errors.Add("definition:engage_range_nonpositive");
            if(d.retreatHealthFraction<0 || d.retreatHealthFraction>=1) r.errors.Add("definition:retreat_fraction_invalid");
            if(d.leader && d.retreatHealthFraction>0) r.warnings.Add("leader:retreat_configured");
            if(t==null) r.errors.Add("tuning:null");
            else { if(t.populationCost<1) r.errors.Add("tuning:population_cost"); if(t.healthMultiplier<=0 || t.speedMultiplier<=0) r.errors.Add("tuning:multiplier_nonpositive"); }
            return r;
        }

        public static ValidationReport Validate(EncounterDefinition e) {
            var r=new ValidationReport();
            if(e==null){r.errors.Add("encounter:null"); return r;}
            if(string.IsNullOrWhiteSpace(e.encounterId)) r.errors.Add("encounter:id_missing");
            if(e.minPopulation<1 || e.maxPopulation<e.minPopulation) r.errors.Add("encounter:population_bounds");
            if(e.populationBudget<e.minPopulation) r.errors.Add("encounter:budget_too_small");
            if(e.activationRadius<=0 || e.leashRadius<=e.activationRadius) r.errors.Add("encounter:radii_invalid");
            return r;
        }
    }

    public static class GoblinAuthoringCatalog
    {
        public static readonly MonsterTuning ScavengerBase=new MonsterTuning{id="GOBLIN_001_BASE",tier=ThreatTier.MINION,variant=VariantKind.BASE,populationCost=1};
        public static readonly MonsterTuning MarauderBase=new MonsterTuning{id="GOBLIN_003_BASE",tier=ThreatTier.VETERAN,variant=VariantKind.BASE,healthMultiplier=1.15f,populationCost=2};
        public static readonly MonsterTuning WarleaderBase=new MonsterTuning{id="GOBLIN_005_BASE",tier=ThreatTier.ELITE,variant=VariantKind.BASE,healthMultiplier=1.35f,populationCost=4};
        public static readonly MonsterTuning ScavengerHardened=new MonsterTuning{id="GOBLIN_001_HARDENED",tier=ThreatTier.VETERAN,variant=VariantKind.HARDENED,healthMultiplier=1.25f,speedMultiplier=1.08f,awarenessMultiplier=1.1f,populationCost=2};

        public static EncounterDefinition Patrol()=>new EncounterDefinition{encounterId="GOB_PATROL_01",familyId="GOBLIN",scenarioKind=ScenarioKind.FIELD,minPopulation=3,maxPopulation=5,populationBudget=6};
        public static EncounterDefinition Warband()=>new EncounterDefinition{encounterId="GOB_WARBAND_01",familyId="GOBLIN",scenarioKind=ScenarioKind.AMBUSH,minPopulation=4,maxPopulation=6,populationBudget=9};
        public static EcologyDefinition Greenbelt()=>new EcologyDefinition{zoneId="GREENBELT_01",familyId="GOBLIN",softCap=8,hardCap=12,respawnCooldownSeconds=55f};
        public static UniqueScenarioDefinition BrokenWatchtower()=>new UniqueScenarioDefinition{scenarioId="UNIQUE_GOB_WATCHTOWER_01",bossDefinitionId="GOBLIN_005",biomeTag="RUINED_WATCHTOWER",populationBudget=12,arenaRadius=20f};
    }

    public sealed class PopulationRuntime
    {
        readonly EcologyDefinition _ecology; int _alive; float _lastSpawnTime=-999f;
        public PopulationRuntime(EcologyDefinition ecology){_ecology=ecology;}
        public int Alive=>_alive;
        public bool CanSpawn(int amount,float now)=>amount>0 && _alive+amount<=_ecology.hardCap && (now-_lastSpawnTime)>=_ecology.respawnCooldownSeconds;
        public bool TryRegisterSpawn(int amount,float now){if(!CanSpawn(amount,now))return false;_alive+=amount;_lastSpawnTime=now;return true;}
        public void RegisterRemoval(int amount){_alive=Mathf.Max(0,_alive-Mathf.Max(0,amount));}
    }

    // Monster publishes encounter facts only. Loot remains sole authority over reward interpretation.
    [Serializable] public sealed class MonsterRewardFactsV12 {
        public string schemaVersion="1.2", encounterInstanceId, sourceRuntimeId, familyId, monsterDefinitionId, variantId, bossId, scenarioId, zoneId, clearMethod;
        public bool firstClearCandidate; public int rewardSeed;
    }

    public static class MobileEncounterBudget
    {
        public const int RecommendedVisibleAgents=8;
        public const int HardVisibleAgents=12;
        public const int RecommendedActiveThinkers=6;
        public static bool IsApproved(int visible,int thinkers)=>visible<=HardVisibleAgents && thinkers<=RecommendedActiveThinkers;
    }
}
