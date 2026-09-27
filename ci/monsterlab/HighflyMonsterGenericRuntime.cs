using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public enum SimulationTier { SLEEPING, OBSERVER, THINKER }

    [Serializable] public sealed class AnatomyPartDefinition {
        public string partId;
        public bool rewardRelevant;
        public bool breakable;
        public bool severable;
    }

    [Serializable] public sealed class AnatomyProfile {
        public string anatomyProfileId;
        public List<AnatomyPartDefinition> parts=new List<AnatomyPartDefinition>();
    }

    [Serializable] public sealed class MonsterDefinitionV2 {
        public string monsterDefinitionId, familyId, anatomyProfileId, aiProfileId;
        public MonsterRole role;
        public float maxHealth=1f, engageRange=8f, retreatHealthFraction;
        public int populationCost=1;
        public bool leader, boss;
        public List<string> habitatTags=new List<string>();
    }

    public sealed class MonsterDefinitionRegistry {
        readonly Dictionary<string,MonsterDefinitionV2> monsters=new Dictionary<string,MonsterDefinitionV2>();
        readonly Dictionary<string,AiProfile> ai=new Dictionary<string,AiProfile>();
        readonly Dictionary<string,AnatomyProfile> anatomy=new Dictionary<string,AnatomyProfile>();
        public bool Add(MonsterDefinitionV2 x)=>x!=null&&!string.IsNullOrWhiteSpace(x.monsterDefinitionId)&&!monsters.ContainsKey(x.monsterDefinitionId)&&(monsters[x.monsterDefinitionId]=x)!=null;
        public bool Add(AiProfile x)=>x!=null&&!string.IsNullOrWhiteSpace(x.profileId)&&!ai.ContainsKey(x.profileId)&&(ai[x.profileId]=x)!=null;
        public bool Add(AnatomyProfile x)=>x!=null&&!string.IsNullOrWhiteSpace(x.anatomyProfileId)&&!anatomy.ContainsKey(x.anatomyProfileId)&&(anatomy[x.anatomyProfileId]=x)!=null;
        public bool TryMonster(string id,out MonsterDefinitionV2 x)=>monsters.TryGetValue(id,out x);
        public bool TryAi(string id,out AiProfile x)=>ai.TryGetValue(id,out x);
        public bool TryAnatomy(string id,out AnatomyProfile x)=>anatomy.TryGetValue(id,out x);
        public IEnumerable<MonsterDefinitionV2> Monsters=>monsters.Values;
    }

    public static class MonsterRegistryValidator {
        public static ValidationReport Validate(MonsterDefinitionRegistry registry) {
            var r=new ValidationReport();
            if(registry==null){r.errors.Add("M001 registry:null");return r;}
            foreach(var d in registry.Monsters){
                if(string.IsNullOrWhiteSpace(d.familyId))r.errors.Add("M002 family:missing:"+d.monsterDefinitionId);
                if(d.maxHealth<=0||d.engageRange<=0)r.errors.Add("M003 stats:invalid:"+d.monsterDefinitionId);
                if(d.populationCost<1)r.errors.Add("M004 population_cost:invalid:"+d.monsterDefinitionId);
                if(d.retreatHealthFraction<0||d.retreatHealthFraction>=1)r.errors.Add("M005 retreat:invalid:"+d.monsterDefinitionId);
                if(!registry.TryAi(d.aiProfileId,out _))r.errors.Add("M006 ai:missing:"+d.monsterDefinitionId+":"+d.aiProfileId);
                if(!registry.TryAnatomy(d.anatomyProfileId,out var a))r.errors.Add("M007 anatomy:missing:"+d.monsterDefinitionId+":"+d.anatomyProfileId);
                else {
                    var ids=new HashSet<string>();
                    foreach(var p in a.parts) if(p==null||string.IsNullOrWhiteSpace(p.partId)||!ids.Add(p.partId))r.errors.Add("M008 anatomy_part:duplicate_or_missing:"+a.anatomyProfileId);
                }
            }
            return r;
        }
    }

    public interface IMonsterSimulationCandidate {
        string RuntimeId {get;}
        bool IsBoss {get;}
        bool Engaged {get;}
        bool Alive {get;}
        float DistanceToHunter {get;}
    }

    public sealed class SimulationAssignment {
        public string runtimeId;
        public SimulationTier tier;
    }

    public static class SimulationTierScheduler {
        public static List<SimulationAssignment> Assign(IList<IMonsterSimulationCandidate> source,int thinkerBudget=6,int observerBudget=8) {
            var alive=new List<IMonsterSimulationCandidate>();
            if(source!=null)for(int i=0;i<source.Count;i++)if(source[i]!=null&&source[i].Alive)alive.Add(source[i]);
            alive.Sort((a,b)=>Score(b).CompareTo(Score(a)));
            thinkerBudget=Mathf.Max(0,thinkerBudget); observerBudget=Mathf.Max(0,observerBudget);
            var result=new List<SimulationAssignment>(alive.Count);
            for(int i=0;i<alive.Count;i++) result.Add(new SimulationAssignment{
                runtimeId=alive[i].RuntimeId,
                tier=i<thinkerBudget?SimulationTier.THINKER:i<thinkerBudget+observerBudget?SimulationTier.OBSERVER:SimulationTier.SLEEPING
            });
            return result;
        }
        static float Score(IMonsterSimulationCandidate x){
            float s=x.IsBoss?100000f:0f;
            if(x.Engaged)s+=10000f;
            s+=Mathf.Max(0f,5000f-x.DistanceToHunter*100f);
            return s;
        }
    }

    public static class SkeletonFamilyFixture {
        public static MonsterDefinitionRegistry Build() {
            var r=new MonsterDefinitionRegistry();
            r.Add(new AiProfile{profileId="AI_SKEL_WARRIOR",thinkInterval=.18f,perceptionInterval=.24f,preferredRange=2.1f,canRetreat=false,canFlank=true});
            r.Add(new AiProfile{profileId="AI_SKEL_LICH",thinkInterval=.22f,perceptionInterval=.28f,preferredRange=7f,canRetreat=true,canFlank=false});
            r.Add(new AiProfile{profileId="AI_WRAITH",thinkInterval=.16f,perceptionInterval=.20f,preferredRange=2.8f,canRetreat=false,canFlank=true});
            r.Add(new AnatomyProfile{anatomyProfileId="ANAT_SKEL_HUMANOID",parts=new List<AnatomyPartDefinition>{
                new AnatomyPartDefinition{partId="SKEL_SKULL",breakable=true},
                new AnatomyPartDefinition{partId="SKEL_WEAPON_ARM",breakable=true}
            }});
            r.Add(new AnatomyProfile{anatomyProfileId="ANAT_WRAITH",parts=new List<AnatomyPartDefinition>{
                new AnatomyPartDefinition{partId="WRAITH_CORE",breakable=false,severable=false}
            }});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="SKELETON_WARRIOR",familyId="SKELETON",anatomyProfileId="ANAT_SKEL_HUMANOID",aiProfileId="AI_SKEL_WARRIOR",role=MonsterRole.MARAUDER,maxHealth=52,engageRange=9,populationCost=1});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="SKELETON_GUARD",familyId="SKELETON",anatomyProfileId="ANAT_SKEL_HUMANOID",aiProfileId="AI_SKEL_WARRIOR",role=MonsterRole.MARAUDER,maxHealth=72,engageRange=9,populationCost=2});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="SKELETON_LICH",familyId="SKELETON",anatomyProfileId="ANAT_SKEL_HUMANOID",aiProfileId="AI_SKEL_LICH",role=MonsterRole.WARLEADER,maxHealth=88,engageRange=12,populationCost=3,leader=true});
            r.Add(new MonsterDefinitionV2{monsterDefinitionId="WRAITH",familyId="SKELETON",anatomyProfileId="ANAT_WRAITH",aiProfileId="AI_WRAITH",role=MonsterRole.MARAUDER,maxHealth=64,engageRange=11,populationCost=2});
            return r;
        }
    }
}
