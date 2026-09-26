using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public enum EncounterOutcome { ACTIVE, CLEARED, PARTIAL_ESCAPE, RETREATED, FAILED }

    [Serializable]
    public sealed class PopulationLedger
    {
        public int hardCap = 18;
        public int committed;
        int reserved;
        public int Available => Mathf.Max(0, hardCap - committed - reserved);
        public bool TryReserve(int count)
        {
            if (count <= 0 || count > Available) return false;
            reserved += count; return true;
        }
        public void Commit(int count)
        {
            count = Mathf.Clamp(count, 0, reserved);
            reserved -= count; committed += count;
        }
        public void Cancel(int count) { reserved = Mathf.Max(0, reserved - Mathf.Max(0, count)); }
        public void Release(int count) { committed = Mathf.Max(0, committed - Mathf.Max(0, count)); }
    }

    [Serializable]
    public sealed class SpawnRequest
    {
        public string encounterInstanceId;
        public int count;
        public int priority;
        public int seed;
    }

    public sealed class SpawnScheduler
    {
        readonly List<SpawnRequest> queue = new List<SpawnRequest>();
        public int maxSpawnsPerTick = 2;
        public void Enqueue(SpawnRequest request)
        {
            if (request == null || request.count <= 0) return;
            queue.Add(request);
            queue.Sort((a,b) => b.priority.CompareTo(a.priority));
        }
        public List<SpawnRequest> TakeTick()
        {
            var result = new List<SpawnRequest>();
            int budget = Mathf.Max(1, maxSpawnsPerTick);
            for (int i=0; i<queue.Count && budget>0;)
            {
                var q=queue[i];
                int take=Mathf.Min(q.count,budget);
                result.Add(new SpawnRequest{encounterInstanceId=q.encounterInstanceId,count=take,priority=q.priority,seed=q.seed});
                q.count-=take; budget-=take;
                if(q.count<=0) queue.RemoveAt(i); else i++;
            }
            return result;
        }
    }

    public static class ActiveThinkerSelector
    {
        public static List<GoblinAgent> Select(IList<GoblinAgent> agents, Vector3 hunter, int budget=6)
        {
            var candidates=new List<GoblinAgent>();
            if(agents!=null) for(int i=0;i<agents.Count;i++)
            {
                var a=agents[i];
                if(a!=null && a.State!=null && a.gameObject.activeInHierarchy &&
                   a.State.lifeState!=MonsterLifeState.DEFEATED && a.State.lifeState!=MonsterLifeState.ESCAPED)
                    candidates.Add(a);
            }
            candidates.Sort((a,b)=>Score(b,hunter).CompareTo(Score(a,hunter)));
            if(candidates.Count>budget) candidates.RemoveRange(Mathf.Max(0,budget),candidates.Count-budget);
            return candidates;
        }
        static float Score(GoblinAgent a, Vector3 hunter)
        {
            float s=a.State.definition.leader?10000f:0f;
            if(a.State.lifeState==MonsterLifeState.ENGAGED) s+=5000f;
            s+=Mathf.Max(0f,1000f-Vector3.Distance(a.transform.position,hunter)*25f);
            return s;
        }
    }

    [Serializable]
    public sealed class MonsterRewardFacts
    {
        public string schemaVersion="1.2";
        public string encounterInstanceId;
        public string familyId;
        public string scenarioId;
        public int defeated;
        public int escaped;
        public bool bossDefeated;
        public EncounterOutcome outcome;
        public int rewardSeed;
    }

    public static class EncounterResolver
    {
        public static EncounterOutcome Resolve(GroupBlackboard board, bool hunterRetreated, bool hunterFailed)
        {
            if(hunterFailed) return EncounterOutcome.FAILED;
            if(hunterRetreated) return EncounterOutcome.RETREATED;
            if(board==null || board.alive>0) return EncounterOutcome.ACTIVE;
            return board.escaped>0 ? EncounterOutcome.PARTIAL_ESCAPE : EncounterOutcome.CLEARED;
        }

        public static MonsterRewardFacts BuildFacts(GroupBlackboard board,string familyId,string scenarioId,bool bossDefeated,int rewardSeed,bool hunterRetreated=false,bool hunterFailed=false)
        {
            return new MonsterRewardFacts{
                encounterInstanceId=board==null?string.Empty:board.encounterInstanceId,
                familyId=familyId,
                scenarioId=scenarioId,
                defeated=board==null?0:board.defeated,
                escaped=board==null?0:board.escaped,
                bossDefeated=bossDefeated,
                outcome=Resolve(board,hunterRetreated,hunterFailed),
                rewardSeed=rewardSeed
            };
        }
    }
}