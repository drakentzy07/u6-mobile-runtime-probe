using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.MonsterLab
{
    public enum MonsterRole { SCAVENGER, MARAUDER, WARLEADER }
    public enum MonsterLifeState { DORMANT, ALERT, ENGAGED, RETREATING, DEFEATED, ESCAPED }
    public enum GroupState { UNAWARE, ENGAGED, DISORGANIZED, RETREATING, RESOLVED }

    [Serializable]
    public sealed class MonsterDefinition
    {
        public string monsterDefinitionId;
        public string familyId = "GOBLIN";
        public MonsterRole role;
        public float maxHealth;
        public float engageRange;
        public float retreatHealthFraction;
        public bool leader;
    }

    [Serializable]
    public sealed class MonsterRuntimeState
    {
        public string runtimeId;
        public MonsterDefinition definition;
        public MonsterLifeState lifeState = MonsterLifeState.DORMANT;
        public float health;
        public Vector3 home;
        public bool rewardPublished;
    }

    [Serializable]
    public sealed class GroupBlackboard
    {
        public string encounterInstanceId;
        public GroupState state = GroupState.UNAWARE;
        public string leaderRuntimeId;
        public Vector3 lastKnownHunterPosition;
        public bool leaderDefeated;
        public int alive;
        public int escaped;
        public int defeated;
    }

    public static class GoblinDefinitions
    {
        public static MonsterDefinition Scavenger() => new MonsterDefinition { monsterDefinitionId="GOBLIN_001", role=MonsterRole.SCAVENGER, maxHealth=35f, engageRange=7.5f, retreatHealthFraction=.28f };
        public static MonsterDefinition Marauder() => new MonsterDefinition { monsterDefinitionId="GOBLIN_003", role=MonsterRole.MARAUDER, maxHealth=65f, engageRange=9f, retreatHealthFraction=.16f };
        public static MonsterDefinition Warleader() => new MonsterDefinition { monsterDefinitionId="GOBLIN_005", role=MonsterRole.WARLEADER, maxHealth=110f, engageRange=10f, retreatHealthFraction=0f, leader=true };
    }

    public sealed class GoblinAgent : MonoBehaviour
    {
        public MonsterRuntimeState State { get; private set; }
        public GroupBlackboard Blackboard { get; private set; }
        Transform _hunter;
        float _speed;
        public event Action<GoblinAgent> Defeated;
        public event Action<GoblinAgent> Escaped;

        public void Bind(MonsterDefinition definition, GroupBlackboard board, Transform hunter, Vector3 home)
        {
            State = new MonsterRuntimeState { runtimeId=Guid.NewGuid().ToString("N"), definition=definition, health=definition.maxHealth, home=home };
            Blackboard=board; _hunter=hunter;
            _speed = definition.role == MonsterRole.SCAVENGER ? 2.6f : definition.role == MonsterRole.MARAUDER ? 2.25f : 2f;
            if (definition.leader) board.leaderRuntimeId=State.runtimeId;
        }

        void Update()
        {
            if (State==null || _hunter==null || State.lifeState==MonsterLifeState.DEFEATED || State.lifeState==MonsterLifeState.ESCAPED) return;
            float d=Vector3.Distance(transform.position,_hunter.position);
            if (State.lifeState==MonsterLifeState.DORMANT && d<=State.definition.engageRange) { State.lifeState=MonsterLifeState.ENGAGED; Blackboard.state=GroupState.ENGAGED; }
            if (Blackboard.leaderDefeated && !State.definition.leader && State.definition.role==MonsterRole.SCAVENGER && State.lifeState==MonsterLifeState.ENGAGED) State.lifeState=MonsterLifeState.RETREATING;
            if (State.lifeState==MonsterLifeState.ENGAGED) MoveToward(_hunter.position, _speed);
            else if (State.lifeState==MonsterLifeState.RETREATING)
            {
                Vector3 away=(transform.position-_hunter.position).normalized;
                MoveToward(transform.position+away*8f,_speed*1.35f);
                if (Vector3.Distance(transform.position,_hunter.position)>15f) Escape();
            }
        }

        void MoveToward(Vector3 target,float speed)
        {
            Vector3 v=target-transform.position; v.y=0;
            if(v.sqrMagnitude<.3f) return;
            transform.position += v.normalized*speed*Time.deltaTime;
            transform.forward=Vector3.Slerp(transform.forward,v.normalized,8f*Time.deltaTime);
        }

        public void ApplyDamage(float amount)
        {
            if(State==null || State.lifeState==MonsterLifeState.DEFEATED || State.lifeState==MonsterLifeState.ESCAPED) return;
            State.health=Mathf.Max(0,State.health-amount);
            if(State.health<=0){ State.lifeState=MonsterLifeState.DEFEATED; Defeated?.Invoke(this); return; }
            if(!State.definition.leader && State.definition.retreatHealthFraction>0 && State.health/State.definition.maxHealth<=State.definition.retreatHealthFraction) State.lifeState=MonsterLifeState.RETREATING;
        }

        void Escape(){ State.lifeState=MonsterLifeState.ESCAPED; Escaped?.Invoke(this); gameObject.SetActive(false); }
    }

    public sealed class GoblinEncounterRuntime
    {
        public readonly GroupBlackboard Blackboard;
        public readonly List<GoblinAgent> Members=new List<GoblinAgent>();
        public GoblinEncounterRuntime(){ Blackboard=new GroupBlackboard{encounterInstanceId=Guid.NewGuid().ToString("N")}; }
        public void Register(GoblinAgent a){ Members.Add(a); Blackboard.alive++; a.Defeated+=OnDefeated; a.Escaped+=OnEscaped; }
        void OnDefeated(GoblinAgent a){ Blackboard.alive--; Blackboard.defeated++; if(a.State.definition.leader){Blackboard.leaderDefeated=true; Blackboard.state=GroupState.DISORGANIZED;} Resolve(); }
        void OnEscaped(GoblinAgent a){ Blackboard.alive--; Blackboard.escaped++; Resolve(); }
        void Resolve(){ if(Blackboard.alive<=0) Blackboard.state=GroupState.RESOLVED; }
    }
}