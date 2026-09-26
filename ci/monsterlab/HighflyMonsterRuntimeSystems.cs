using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public enum AiIntent { IDLE, INVESTIGATE, PRESSURE, FLANK, GUARD, RETREAT, ESCAPE }
    public enum BossPhase { DORMANT, PHASE_1, PHASE_2, ENRAGED, DEFEATED }
    public enum ScenarioState { LOCKED, AVAILABLE, ACTIVE, RESOLVED, ABANDONED }
    public enum AssetApproval { UNKNOWN, APPROVED, REJECTED, PLACEHOLDER_ONLY }

    [Serializable] public sealed class AiProfile
    {
        public string profileId;
        public float thinkInterval=.15f, perceptionInterval=.20f, preferredRange=2.2f, flankChance=.2f;
        public bool canRetreat=true, canFlank=true, protectsLeader;
    }

    [Serializable] public sealed class VariantEvolutionRule
    {
        public string ruleId, sourceVariantId, targetVariantId;
        public int minimumMonsterEscapes=2, minimumHunterEscapes;
        public float maxHealthScale=1.35f, speedScale=1.08f;
        public bool Matches(NemesisMemory m) => m!=null && m.monsterEscapes>=minimumMonsterEscapes && m.hunterEscapes>=minimumHunterEscapes;
    }

    [Serializable] public sealed class BossDefinition
    {
        public string bossId, monsterDefinitionId, aiProfileId;
        public float phase2HealthFraction=.65f, enrageHealthFraction=.25f;
        public bool uniqueScenarioOnly=true, mobileApproved=true;
    }

    [Serializable] public sealed class AssetRequirement
    {
        public string requirementId, familyId, role, purpose;
        public AssetApproval approval;
        public string candidatePath, licenseNote;
    }

    public sealed class MonsterAssetRegistry
    {
        readonly Dictionary<string,AssetRequirement> _items=new Dictionary<string,AssetRequirement>();
        public IEnumerable<AssetRequirement> Items=>_items.Values;
        public void Register(AssetRequirement item){ if(item!=null && !string.IsNullOrWhiteSpace(item.requirementId)) _items[item.requirementId]=item; }
        public bool RuntimeReady(string id)=>_items.TryGetValue(id,out var x) && x.approval==AssetApproval.APPROVED && !string.IsNullOrWhiteSpace(x.candidatePath);
        public List<string> MissingApprovedAssets(){
            var gaps=new List<string>();
            foreach(var x in _items.Values) if(x.approval!=AssetApproval.APPROVED || string.IsNullOrWhiteSpace(x.candidatePath)) gaps.Add(x.requirementId);
            return gaps;
        }
    }

    public sealed class AiDecisionRuntime
    {
        readonly AiProfile _profile; float _nextThink, _nextPerception;
        public AiIntent Intent {get; private set;}=AiIntent.IDLE;
        public Vector3 LastKnownHunter {get; private set;}
        public AiDecisionRuntime(AiProfile profile){_profile=profile ?? throw new ArgumentNullException(nameof(profile));}
        public bool PerceptionDue(float now)=>now>=_nextPerception;
        public bool ThinkDue(float now)=>now>=_nextThink;
        public void Observe(Vector3 hunter,float now){LastKnownHunter=hunter;_nextPerception=now+Mathf.Max(.05f,_profile.perceptionInterval);}
        public AiIntent Think(float now,float distance,float healthFraction,bool leaderAlive,int deterministicRoll){
            if(!ThinkDue(now)) return Intent;
            _nextThink=now+Mathf.Max(.05f,_profile.thinkInterval);
            if(_profile.canRetreat && healthFraction<=.18f) return Intent=AiIntent.RETREAT;
            if(_profile.protectsLeader && leaderAlive && distance<_profile.preferredRange*1.5f) return Intent=AiIntent.GUARD;
            if(_profile.canFlank && distance>_profile.preferredRange && Mathf.Abs(deterministicRoll)%100 < Mathf.RoundToInt(_profile.flankChance*100f)) return Intent=AiIntent.FLANK;
            return Intent=distance>_profile.preferredRange ? AiIntent.PRESSURE : AiIntent.GUARD;
        }
    }

    public sealed class BossRuntime
    {
        readonly BossDefinition _definition;
        public BossPhase Phase {get; private set;}=BossPhase.DORMANT;
        public event Action<BossPhase> PhaseChanged;
        public BossRuntime(BossDefinition definition){_definition=definition ?? throw new ArgumentNullException(nameof(definition));}
        public void Activate(){Set(BossPhase.PHASE_1);}
        public void Evaluate(float healthFraction){
            healthFraction=Mathf.Clamp01(healthFraction);
            if(Phase==BossPhase.DEFEATED)return;
            if(healthFraction<=0){Set(BossPhase.DEFEATED);return;}
            if(healthFraction<=_definition.enrageHealthFraction){Set(BossPhase.ENRAGED);return;}
            if(healthFraction<=_definition.phase2HealthFraction && Phase==BossPhase.PHASE_1) Set(BossPhase.PHASE_2);
        }
        void Set(BossPhase p){if(Phase==p)return;Phase=p;PhaseChanged?.Invoke(p);}
    }

    public sealed class EncounterPlan
    {
        public readonly List<MonsterDefinition> members=new List<MonsterDefinition>();
        public int spentBudget;
        public bool Valid;
    }

    public static class GoblinEncounterPlanner
    {
        // Deterministic composition: reproducible in tests and compatible with RewardContext rewardSeed ownership boundary.
        public static EncounterPlan Build(EncounterDefinition e,int seed){
            var p=new EncounterPlan();
            if(e==null)return p;
            var rng=new System.Random(seed);
            int target=Mathf.Clamp(rng.Next(e.minPopulation,e.maxPopulation+1),e.minPopulation,e.maxPopulation);
            for(int i=0;i<target;i++){
                bool addMarauder=i>0 && p.spentBudget+2<=e.populationBudget && rng.NextDouble()>.55;
                if(addMarauder){p.members.Add(GoblinDefinitions.Marauder());p.spentBudget+=2;}
                else if(p.spentBudget+1<=e.populationBudget){p.members.Add(GoblinDefinitions.Scavenger());p.spentBudget++;}
            }
            if(e.scenarioKind==ScenarioKind.AMBUSH && p.members.Count>0 && p.spentBudget+4<=e.populationBudget){
                p.members[p.members.Count-1]=GoblinDefinitions.Warleader(); p.spentBudget+=3;
            }
            p.Valid=p.members.Count>=e.minPopulation && p.members.Count<=e.maxPopulation && p.spentBudget<=e.populationBudget;
            return p;
        }
    }

    public sealed class UniqueScenarioRuntime
    {
        readonly UniqueScenarioDefinition _definition;
        public ScenarioState State {get;private set;}=ScenarioState.LOCKED;
        public int Attempts {get;private set;}
        public UniqueScenarioRuntime(UniqueScenarioDefinition definition){_definition=definition ?? throw new ArgumentNullException(nameof(definition));}
        public void MakeAvailable(){if(State==ScenarioState.LOCKED)State=ScenarioState.AVAILABLE;}
        public bool TryEnter(){if(State!=ScenarioState.AVAILABLE && State!=ScenarioState.ABANDONED)return false;Attempts++;State=ScenarioState.ACTIVE;return true;}
        public void Resolve(){if(State==ScenarioState.ACTIVE)State=ScenarioState.RESOLVED;}
        public bool Retreat(){if(State!=ScenarioState.ACTIVE || !_definition.allowRetreat)return false;State=ScenarioState.ABANDONED;return true;}
    }

    public static class MonsterRuntimeCatalog
    {
        public static AiProfile ScavengerAi()=>new AiProfile{profileId="AI_GOB_SCAVENGER",thinkInterval=.22f,perceptionInterval=.28f,preferredRange=1.8f,flankChance=.08f,canRetreat=true};
        public static AiProfile MarauderAi()=>new AiProfile{profileId="AI_GOB_MARAUDER",thinkInterval=.16f,perceptionInterval=.20f,preferredRange=2.1f,flankChance=.28f,canRetreat=true};
        public static AiProfile WarleaderAi()=>new AiProfile{profileId="AI_GOB_WARLEADER",thinkInterval=.14f,perceptionInterval=.18f,preferredRange=2.4f,flankChance=.12f,canRetreat=false,protectsLeader=false};
        public static BossDefinition WatchtowerBoss()=>new BossDefinition{bossId="BOSS_GOB_WATCHTOWER_01",monsterDefinitionId="GOBLIN_005",aiProfileId="AI_GOB_WARLEADER",phase2HealthFraction=.62f,enrageHealthFraction=.22f};
        public static VariantEvolutionRule HardenedScavenger()=>new VariantEvolutionRule{ruleId="EVO_GOB_SCAV_01",sourceVariantId="GOBLIN_001_BASE",targetVariantId="GOBLIN_001_HARDENED",minimumMonsterEscapes=2,maxHealthScale=1.25f,speedScale=1.08f};
    }

    public static class MonsterAdvancedValidator
    {
        public static ValidationReport Validate(BossDefinition b){
            var r=new ValidationReport();
            if(b==null){r.errors.Add("boss:null");return r;}
            if(string.IsNullOrWhiteSpace(b.bossId)||string.IsNullOrWhiteSpace(b.monsterDefinitionId))r.errors.Add("boss:id_missing");
            if(b.phase2HealthFraction<=b.enrageHealthFraction || b.phase2HealthFraction>=1f || b.enrageHealthFraction<=0f)r.errors.Add("boss:phase_thresholds_invalid");
            if(!b.mobileApproved)r.warnings.Add("boss:not_mobile_approved");
            return r;
        }
        public static ValidationReport Validate(UniqueScenarioDefinition s){
            var r=new ValidationReport();
            if(s==null){r.errors.Add("scenario:null");return r;}
            if(string.IsNullOrWhiteSpace(s.scenarioId)||string.IsNullOrWhiteSpace(s.bossDefinitionId))r.errors.Add("scenario:id_missing");
            if(s.populationBudget<1 || s.arenaRadius<=5f)r.errors.Add("scenario:budget_or_arena_invalid");
            if(!s.mobileApproved)r.warnings.Add("scenario:not_mobile_approved");
            return r;
        }
    }
}
