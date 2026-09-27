using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public enum EncounterLifecycle { DORMANT, RESERVED, SPAWNING, ACTIVE, RESOLVING, RESOLVED, ABANDONED }

    public interface IPopulationReservation
    {
        bool TryReserve(string encounterId, int cost);
        void Release(string encounterId);
    }

    public sealed class EncounterInstance
    {
        public string EncounterId { get; private set; }
        public EncounterLifecycle State { get; private set; }
        public int ReservedPopulationCost { get; private set; }
        readonly IPopulationReservation population;
        bool reservationHeld;

        public EncounterInstance(string encounterId, IPopulationReservation population)
        {
            if (string.IsNullOrWhiteSpace(encounterId)) throw new ArgumentException("encounterId");
            EncounterId = encounterId; this.population = population; State = EncounterLifecycle.DORMANT;
        }

        public bool Reserve(int cost)
        {
            if (State == EncounterLifecycle.RESERVED) return reservationHeld && ReservedPopulationCost == cost;
            if (State != EncounterLifecycle.DORMANT || cost < 1 || population == null) return false;
            if (!population.TryReserve(EncounterId, cost)) return false;
            ReservedPopulationCost = cost; reservationHeld = true; State = EncounterLifecycle.RESERVED; return true;
        }
        public bool BeginSpawn() => Move(EncounterLifecycle.RESERVED, EncounterLifecycle.SPAWNING);
        public bool Activate() => Move(EncounterLifecycle.SPAWNING, EncounterLifecycle.ACTIVE);
        public bool BeginResolve() => Move(EncounterLifecycle.ACTIVE, EncounterLifecycle.RESOLVING);
        public bool Resolve()
        {
            if (State == EncounterLifecycle.RESOLVED) return true;
            if (State != EncounterLifecycle.RESOLVING) return false;
            ReleaseOnce(); State = EncounterLifecycle.RESOLVED; return true;
        }
        public bool Abandon()
        {
            if (State == EncounterLifecycle.ABANDONED) return true;
            if (State != EncounterLifecycle.RESERVED && State != EncounterLifecycle.SPAWNING && State != EncounterLifecycle.ACTIVE) return false;
            ReleaseOnce(); State = EncounterLifecycle.ABANDONED; return true;
        }
        bool Move(EncounterLifecycle from, EncounterLifecycle to) { if (State == to) return true; if (State != from) return false; State = to; return true; }
        void ReleaseOnce() { if (!reservationHeld) return; population.Release(EncounterId); reservationHeld = false; }
    }

    [Serializable] public sealed class NemesisRecord
    {
        public string lineageId, familyId, currentDefinitionId, lastZoneId;
        public int encountersSeen, hunterWins, escapes, defeatsSurvived;
        public List<string> memoryFlags = new List<string>();
        public List<string> permanentPartFacts = new List<string>();
        public List<string> evolutionHistory = new List<string>();
    }

    public sealed class NemesisLineageStore
    {
        readonly Dictionary<string,NemesisRecord> records = new Dictionary<string,NemesisRecord>();
        public NemesisRecord GetOrCreate(string lineageId, string familyId, string definitionId)
        {
            if (string.IsNullOrWhiteSpace(lineageId)) throw new ArgumentException("lineageId");
            if (!records.TryGetValue(lineageId,out var r)) {
                r=new NemesisRecord{lineageId=lineageId,familyId=familyId,currentDefinitionId=definitionId}; records.Add(lineageId,r);
            }
            return r;
        }
        public bool TryGet(string lineageId,out NemesisRecord record)=>records.TryGetValue(lineageId,out record);
    }

    [Serializable] public sealed class EvolutionRule
    {
        public string ruleId, fromDefinitionId, toDefinitionId;
        public int minEncounters;
        public int minEscapes;
        public int minDefeatsSurvived;
        public string requiredMemoryFlag;
        public int priority;
    }

    public static class EvolutionEvaluator
    {
        public static EvolutionRule Select(NemesisRecord record, IList<EvolutionRule> rules)
        {
            if (record==null || rules==null) return null;
            EvolutionRule best=null;
            for(int i=0;i<rules.Count;i++) {
                var x=rules[i]; if(x==null || x.fromDefinitionId!=record.currentDefinitionId) continue;
                if(record.encountersSeen<x.minEncounters || record.escapes<x.minEscapes || record.defeatsSurvived<x.minDefeatsSurvived) continue;
                if(!string.IsNullOrWhiteSpace(x.requiredMemoryFlag) && !record.memoryFlags.Contains(x.requiredMemoryFlag)) continue;
                if(best==null || x.priority>best.priority || (x.priority==best.priority && string.CompareOrdinal(x.ruleId,best.ruleId)<0)) best=x;
            }
            return best;
        }
        public static bool Apply(NemesisRecord record, EvolutionRule rule)
        {
            if(record==null || rule==null || record.currentDefinitionId!=rule.fromDefinitionId) return false;
            record.currentDefinitionId=rule.toDefinitionId;
            if(!record.evolutionHistory.Contains(rule.ruleId)) record.evolutionHistory.Add(rule.ruleId);
            return true;
        }
    }

    [Serializable] public sealed class BossPhaseDefinition
    {
        public string phaseId;
        public List<string> nextPhaseIds=new List<string>();
        public List<string> entrySignals=new List<string>();
        public List<string> abilitySetIds=new List<string>();
        public int thinkerBudgetReservation=1;
    }
    [Serializable] public sealed class BossGraphDefinition
    {
        public string bossDefinitionId, entryPhaseId;
        public List<BossPhaseDefinition> phases=new List<BossPhaseDefinition>();
    }

    public static class BossGraphValidator
    {
        public static List<string> Validate(BossGraphDefinition graph)
        {
            var e=new List<string>(); if(graph==null){e.Add("B001 graph:null");return e;}
            var map=new Dictionary<string,BossPhaseDefinition>();
            foreach(var p in graph.phases) {
                if(p==null || string.IsNullOrWhiteSpace(p.phaseId)){e.Add("B002 phase:missing_id");continue;}
                if(map.ContainsKey(p.phaseId)){e.Add("B003 phase:duplicate:"+p.phaseId);continue;} map.Add(p.phaseId,p);
                if(p.thinkerBudgetReservation<1)e.Add("B004 thinker_reservation:invalid:"+p.phaseId);
            }
            if(!map.ContainsKey(graph.entryPhaseId))e.Add("B005 entry:missing:"+graph.entryPhaseId);
            foreach(var p in map.Values) foreach(var n in p.nextPhaseIds) if(!map.ContainsKey(n))e.Add("B006 edge:missing:"+p.phaseId+"->"+n);
            return e;
        }
    }

    public enum UniqueScenarioState { LOCKED, INTRO, ACTIVE, CLIMAX, RESOLUTION, EXIT, FAIL, RETREAT }
    [Serializable] public sealed class ScenarioTransition {
        public UniqueScenarioState from, to;
        public string requiredSignal;
    }
    [Serializable] public sealed class UniqueScenarioDefinitionV2 {
        public string scenarioId;
        public UniqueScenarioState initialState=UniqueScenarioState.LOCKED;
        public List<ScenarioTransition> transitions=new List<ScenarioTransition>();
    }
    public sealed class UniqueScenarioRuntimeV2
    {
        public UniqueScenarioState State {get;private set;}
        readonly UniqueScenarioDefinitionV2 definition;
        public UniqueScenarioRuntimeV2(UniqueScenarioDefinitionV2 d){definition=d??throw new ArgumentNullException("d");State=d.initialState;}
        public bool Signal(string signal)
        {
            for(int i=0;i<definition.transitions.Count;i++){var t=definition.transitions[i];if(t.from==State&&t.requiredSignal==signal){State=t.to;return true;}}
            return false;
        }
    }

    [Serializable] public sealed class MonsterPerfSample
    {
        public int thinkers, observers, visible;
        public float aiDecisionMs, perceptionMs, spawnMs, animationMs, peakFrameMs;
    }
    public sealed class MonsterPerfAccumulator
    {
        public int sampleCount; public float maxPeakFrameMs; public long thinkerSamples, observerSamples, visibleSamples;
        public void Add(MonsterPerfSample s){if(s==null)return;sampleCount++;thinkerSamples+=s.thinkers;observerSamples+=s.observers;visibleSamples+=s.visible;maxPeakFrameMs=Mathf.Max(maxPeakFrameMs,s.peakFrameMs);}
    }
}
