using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public interface IGenericMonsterRuntime : IMonsterSimulationCandidate
    {
        string DefinitionId { get; }
        void Think(MonsterDefinitionV2 definition, AiProfile ai);
        void Observe(MonsterDefinitionV2 definition, AiProfile ai);
        void Sleep(MonsterDefinitionV2 definition);
    }

    public sealed class GenericMonsterRuntimeCoordinator
    {
        readonly MonsterDefinitionRegistry registry;
        public GenericMonsterRuntimeCoordinator(MonsterDefinitionRegistry registry){this.registry=registry??throw new ArgumentNullException("registry");}
        public List<string> Tick(IList<IGenericMonsterRuntime> agents,int thinkerBudget=6,int observerBudget=8)
        {
            var errors=new List<string>(); var candidates=new List<IMonsterSimulationCandidate>(); var byId=new Dictionary<string,IGenericMonsterRuntime>();
            if(agents!=null) foreach(var a in agents) {
                if(a==null||!a.Alive)continue;
                if(string.IsNullOrWhiteSpace(a.RuntimeId)||byId.ContainsKey(a.RuntimeId)){errors.Add("R003 runtime_id:missing_or_duplicate:"+(a?.RuntimeId??"null"));continue;}
                if(!registry.TryMonster(a.DefinitionId,out var d)){errors.Add("R001 definition:missing:"+a.DefinitionId);continue;}
                if(!registry.TryAi(d.aiProfileId,out _)){errors.Add("R002 ai:missing:"+a.DefinitionId+":"+d.aiProfileId);continue;}
                byId.Add(a.RuntimeId,a); candidates.Add(a);
            }
            var assignments=SimulationTierScheduler.Assign(candidates,thinkerBudget,observerBudget);
            foreach(var x in assignments) {
                var a=byId[x.runtimeId]; registry.TryMonster(a.DefinitionId,out var d); registry.TryAi(d.aiProfileId,out var ai);
                if(x.tier==SimulationTier.THINKER)a.Think(d,ai); else if(x.tier==SimulationTier.OBSERVER)a.Observe(d,ai); else a.Sleep(d);
            }
            return errors;
        }
    }

    public static class ExtendedFamilyFixtures
    {
        public static MonsterDefinitionRegistry Build()
        {
            var r=new MonsterDefinitionRegistry();
            r.Add(new AiProfile{profileId="AI_ORC_RAIDER",thinkInterval=.17f,perceptionInterval=.23f,preferredRange=2.4f,canRetreat=true,canFlank=true});
            r.Add(new AiProfile{profileId="AI_ORC_WARLEADER",thinkInterval=.14f,perceptionInterval=.20f,preferredRange=2.8f,canRetreat=false,canFlank=true});
            r.Add(new AiProfile{profileId="AI_LYCAN_HUNTER",thinkInterval=.13f,perceptionInterval=.18f,preferredRange=2.2f,canRetreat=true,canFlank=true});
            r.Add(new AiProfile{profileId="AI_DRAGON_BOSS",thinkInterval=.12f,perceptionInterval=.18f,preferredRange=8f,canRetreat=false,canFlank=false});
            r.Add(new AnatomyProfile{anatomyProfileId="ANAT_ORC",parts=new List<AnatomyPartDefinition>{
                new AnatomyPartDefinition{partId="ORC_SHIELD",breakable=true},new AnatomyPartDefinition{partId="ORC_WAR_HELM",breakable=true},new AnatomyPartDefinition{partId="ORC_SIEGE_BANNER",breakable=true}}});
            r.Add(new AnatomyProfile{anatomyProfileId="ANAT_LYCAN",parts=new List<AnatomyPartDefinition>{
                new AnatomyPartDefinition{partId="LYCAN_CURSE_NODE",breakable=false},new AnatomyPartDefinition{partId="LYCAN_FANG_L",breakable=true},new AnatomyPartDefinition{partId="LYCAN_FANG_R",breakable=true}}});
            r.Add(new AnatomyProfile{anatomyProfileId="ANAT_DRAGON",parts=new List<AnatomyPartDefinition>{
                new AnatomyPartDefinition{partId="DRAGON_HORN_L",breakable=true},new AnatomyPartDefinition{partId="DRAGON_HORN_R",breakable=true},
                new AnatomyPartDefinition{partId="DRAGON_WING_L",breakable=true},new AnatomyPartDefinition{partId="DRAGON_WING_R",breakable=true},
                new AnatomyPartDefinition{partId="DRAGON_TAIL",breakable=true,severable=true},new AnatomyPartDefinition{partId="DRAGON_ELEMENTAL_CORE",breakable=false}}});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="ORC_RAIDER",familyId="ORC",anatomyProfileId="ANAT_ORC",aiProfileId="AI_ORC_RAIDER",role=MonsterRole.MARAUDER,maxHealth=95,engageRange=10,populationCost=2});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="ORC_WARLEADER",familyId="ORC",anatomyProfileId="ANAT_ORC",aiProfileId="AI_ORC_WARLEADER",role=MonsterRole.WARLEADER,maxHealth=180,engageRange=12,populationCost=4,leader=true});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="LYCANTHROPE_HUNTER",familyId="LYCANTHROPE",anatomyProfileId="ANAT_LYCAN",aiProfileId="AI_LYCAN_HUNTER",role=MonsterRole.MARAUDER,maxHealth=145,engageRange=14,populationCost=3});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="DRAGON_ANCIENT",familyId="DRAGON",anatomyProfileId="ANAT_DRAGON",aiProfileId="AI_DRAGON_BOSS",role=MonsterRole.WARLEADER,maxHealth=1800,engageRange=30,populationCost=12,leader=true,boss=true});
            return r;
        }
    }

    public static class EvolutionValidator
    {
        public static List<string> Validate(MonsterDefinitionRegistry registry,IList<EvolutionRule> rules)
        {
            var e=new List<string>(); if(registry==null){e.Add("E001 registry:null");return e;} if(rules==null)return e; var ids=new HashSet<string>();
            foreach(var x in rules){if(x==null||string.IsNullOrWhiteSpace(x.ruleId)){e.Add("E002 rule:missing_id");continue;} if(!ids.Add(x.ruleId))e.Add("E003 rule:duplicate:"+x.ruleId);
                if(!registry.TryMonster(x.fromDefinitionId,out _))e.Add("E004 from:missing:"+x.ruleId+":"+x.fromDefinitionId);
                if(!registry.TryMonster(x.toDefinitionId,out _))e.Add("E005 to:missing:"+x.ruleId+":"+x.toDefinitionId);
                if(x.fromDefinitionId==x.toDefinitionId)e.Add("E006 self_transition:"+x.ruleId);
                if(x.minEncounters<0||x.minEscapes<0||x.minDefeatsSurvived<0)e.Add("E007 threshold:negative:"+x.ruleId);}
            return e;
        }
    }

    public static class UniqueScenarioValidator
    {
        static bool Terminal(UniqueScenarioState s)=>s==UniqueScenarioState.EXIT||s==UniqueScenarioState.FAIL||s==UniqueScenarioState.RETREAT;
        public static List<string> Validate(UniqueScenarioDefinitionV2 d)
        {
            var e=new List<string>(); if(d==null){e.Add("S001 scenario:null");return e;} var keys=new HashSet<string>(); var reachable=new HashSet<UniqueScenarioState>{d.initialState}; bool changed=true;
            foreach(var t in d.transitions){var key=t.from+"|"+(t.requiredSignal??"");if(!keys.Add(key))e.Add("S002 transition:ambiguous:"+key);}
            while(changed){changed=false;foreach(var t in d.transitions)if(reachable.Contains(t.from)&&reachable.Add(t.to))changed=true;}
            foreach(var t in d.transitions)if(!reachable.Contains(t.from))e.Add("S003 state:unreachable:"+t.from);
            bool terminal=false;foreach(var s in reachable)if(Terminal(s)){terminal=true;break;} if(!terminal)e.Add("S004 terminal:unreachable");
            if(string.IsNullOrWhiteSpace(d.scenarioId))e.Add("S005 scenario:missing_id"); return e;
        }
    }

    [Serializable] public sealed class MonsterPerfSummary { public int samples; public float avgThinkers,avgObservers,avgVisible,maxPeakFrameMs; }
    public static class MonsterPerfReporter
    {
        public static MonsterPerfSummary Summarize(MonsterPerfAccumulator a){if(a==null||a.sampleCount<=0)return new MonsterPerfSummary();return new MonsterPerfSummary{samples=a.sampleCount,avgThinkers=(float)a.thinkerSamples/a.sampleCount,avgObservers=(float)a.observerSamples/a.sampleCount,avgVisible=(float)a.visibleSamples/a.sampleCount,maxPeakFrameMs=a.maxPeakFrameMs};}
    }
}
