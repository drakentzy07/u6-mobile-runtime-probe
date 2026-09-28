using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public sealed class GenericMonsterAgent : IGenericMonsterRuntime
    {
        public string RuntimeId { get; private set; }
        public string DefinitionId { get; private set; }
        public bool IsBoss { get; private set; }
        public bool Engaged { get; set; }
        public bool Alive { get; set; } = true;
        public float DistanceToHunter { get; set; }
        public SimulationTier LastTier { get; private set; } = SimulationTier.SLEEPING;
        public int ThinkCount { get; private set; }
        public int ObserveCount { get; private set; }

        public GenericMonsterAgent(string runtimeId,string definitionId,bool isBoss=false)
        {
            if(string.IsNullOrWhiteSpace(runtimeId))throw new ArgumentException("runtimeId");
            if(string.IsNullOrWhiteSpace(definitionId))throw new ArgumentException("definitionId");
            RuntimeId=runtimeId; DefinitionId=definitionId; IsBoss=isBoss;
        }
        public void Think(MonsterDefinitionV2 definition,AiProfile ai){LastTier=SimulationTier.THINKER;ThinkCount++;}
        public void Observe(MonsterDefinitionV2 definition,AiProfile ai){LastTier=SimulationTier.OBSERVER;ObserveCount++;}
        public void Sleep(MonsterDefinitionV2 definition){LastTier=SimulationTier.SLEEPING;}
    }

    public sealed class PopulationLedgerV2 : IPopulationReservation
    {
        readonly Dictionary<string,int> held=new Dictionary<string,int>();
        public int Capacity { get; private set; }
        public int Reserved { get; private set; }
        public int Available => Mathf.Max(0,Capacity-Reserved);
        public PopulationLedgerV2(int capacity){Capacity=Mathf.Max(0,capacity);}
        public bool TryReserve(string encounterId,int cost)
        {
            if(string.IsNullOrWhiteSpace(encounterId)||cost<1)return false;
            if(held.TryGetValue(encounterId,out var existing))return existing==cost;
            if(cost>Available)return false;
            held.Add(encounterId,cost); Reserved+=cost; return true;
        }
        public void Release(string encounterId)
        {
            if(string.IsNullOrWhiteSpace(encounterId)||!held.TryGetValue(encounterId,out var cost))return;
            held.Remove(encounterId); Reserved=Mathf.Max(0,Reserved-cost);
        }
    }

    [Serializable] public sealed class EcologyZoneDefinition
    {
        public string zoneId;
        public int populationCapacity=1;
        public List<string> habitatTags=new List<string>();
        public List<string> allowedFamilyIds=new List<string>();
    }

    public static class EcologyValidator
    {
        public static List<string> Validate(EcologyZoneDefinition zone,MonsterDefinitionRegistry registry)
        {
            var e=new List<string>();
            if(zone==null){e.Add("Z001 zone:null");return e;}
            if(string.IsNullOrWhiteSpace(zone.zoneId))e.Add("Z002 zone:missing_id");
            if(zone.populationCapacity<1)e.Add("Z003 capacity:invalid:"+zone.zoneId);
            if(zone.habitatTags==null||zone.habitatTags.Count==0)e.Add("Z004 habitat:empty:"+zone.zoneId);
            if(registry==null){e.Add("Z005 registry:null");return e;}
            var allowed=new HashSet<string>(zone.allowedFamilyIds??new List<string>());
            foreach(var d in registry.Monsters)
            {
                if(allowed.Count>0&&!allowed.Contains(d.familyId))continue;
                bool match=d.habitatTags==null||d.habitatTags.Count==0;
                if(!match)foreach(var tag in d.habitatTags)if(zone.habitatTags.Contains(tag)){match=true;break;}
                if(!match)e.Add("Z006 habitat:no_match:"+zone.zoneId+":"+d.monsterDefinitionId);
            }
            return e;
        }
    }

    [Serializable] public sealed class BossPhasePredicate
    {
        public string predicateId,fromPhaseId,toPhaseId,requiredSignal,requiredPartFact;
        public float maxHealthFraction=1f;
        public int priority;
    }

    public sealed class BossPredicateContext
    {
        public float healthFraction=1f;
        public readonly HashSet<string> signals=new HashSet<string>();
        public readonly HashSet<string> partFacts=new HashSet<string>();
    }

    public static class BossPredicateEvaluator
    {
        public static BossPhasePredicate Select(string currentPhase,BossPredicateContext context,IList<BossPhasePredicate> predicates)
        {
            if(context==null||predicates==null)return null;
            BossPhasePredicate best=null;
            for(int i=0;i<predicates.Count;i++)
            {
                var p=predicates[i]; if(p==null||p.fromPhaseId!=currentPhase)continue;
                if(context.healthFraction>p.maxHealthFraction)continue;
                if(!string.IsNullOrWhiteSpace(p.requiredSignal)&&!context.signals.Contains(p.requiredSignal))continue;
                if(!string.IsNullOrWhiteSpace(p.requiredPartFact)&&!context.partFacts.Contains(p.requiredPartFact))continue;
                if(best==null||p.priority>best.priority||(p.priority==best.priority&&string.CompareOrdinal(p.predicateId,best.predicateId)<0))best=p;
            }
            return best;
        }

        public static List<string> Validate(IList<BossPhasePredicate> predicates,BossGraphDefinition graph)
        {
            var e=new List<string>(); if(predicates==null)return e;
            var phases=new HashSet<string>(); if(graph!=null&&graph.phases!=null)foreach(var p in graph.phases)if(p!=null)phases.Add(p.phaseId);
            var ids=new HashSet<string>();
            foreach(var p in predicates)
            {
                if(p==null||string.IsNullOrWhiteSpace(p.predicateId)){e.Add("P001 predicate:missing_id");continue;}
                if(!ids.Add(p.predicateId))e.Add("P002 predicate:duplicate:"+p.predicateId);
                if(!phases.Contains(p.fromPhaseId)||!phases.Contains(p.toPhaseId))e.Add("P003 phase:missing:"+p.predicateId);
                if(p.maxHealthFraction<0f||p.maxHealthFraction>1f)e.Add("P004 health_fraction:invalid:"+p.predicateId);
                if(p.fromPhaseId==p.toPhaseId)e.Add("P005 self_transition:"+p.predicateId);
            }
            return e;
        }
    }

    public sealed class ScenarioEncounterBinding
    {
        public string scenarioId;
        public UniqueScenarioState activationState=UniqueScenarioState.ACTIVE;
        public string encounterId;
        public int populationCost=1;
    }

    public static class ScenarioEncounterBindingValidator
    {
        public static List<string> Validate(ScenarioEncounterBinding b,UniqueScenarioDefinitionV2 scenario)
        {
            var e=new List<string>();
            if(b==null){e.Add("X001 binding:null");return e;}
            if(scenario==null||b.scenarioId!=scenario.scenarioId)e.Add("X002 scenario:mismatch");
            if(string.IsNullOrWhiteSpace(b.encounterId))e.Add("X003 encounter:missing_id");
            if(b.populationCost<1)e.Add("X004 population_cost:invalid");
            if(b.activationState==UniqueScenarioState.EXIT||b.activationState==UniqueScenarioState.FAIL||b.activationState==UniqueScenarioState.RETREAT)e.Add("X005 activation:terminal");
            return e;
        }
    }
}
