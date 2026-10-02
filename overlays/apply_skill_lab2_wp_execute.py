from pathlib import Path

ROOT=Path('.')
CLASSES=ROOT/'src/sim/content/classes.ts'
THRESHOLD=ROOT/'src/sim/combat/execute_threshold.ts'
EMPOWER=ROOT/'src/sim/combat/empower_next.ts'
TEST=ROOT/'tests/highfly_skill_lab2_wp_execute.test.ts'

def read(path: Path) -> str:
    return path.read_text(encoding='utf-8')

def write(path: Path, text: str) -> None:
    path.write_text(text,encoding='utf-8')

def rep(path: Path, old: str, new: str) -> None:
    text=read(path)
    n=text.count(old)
    if n!=1:
        raise SystemExit(f'{path}: expected 1 anchor, found {n}: {old[:180]!r}')
    write(path,text.replace(old,new,1))

def insert_roster_after(ability_id: str, additions: list[str]) -> None:
    text=read(CLASSES)
    anchor=f"      '{ability_id}',"
    n=text.count(anchor)
    if n!=1:
        raise SystemExit(f'{CLASSES}: expected one roster entry for {ability_id}, found {n}')
    block=anchor + ''.join(f"\n      '{item}'," for item in additions)
    write(CLASSES,text.replace(anchor,block,1))

# ---------------------------------------------------------------------------
# HIGHFLY Skill Lab 2.0 — Warrior Execute PRIME
#
# BASE Claude authority:
#   execute / Early Grave
#   Rage 15, instant, melee, target <20%, directDamage 60–75
#
# GOLD contract:
# - Veredicto Sangriento and Fin del Rey keep exact BASE authority.
# - Sudden Death must open the execute window, make the cast free, and be
#   consumed exactly once for BASE/EVO/MUT.
# - No kill-by-VFX / no automatic boss execution.
# ---------------------------------------------------------------------------

insert_roster_after('execute', ['hf_bloody_verdict_01','hf_kings_end_01'])

rep(
    CLASSES,
    """  slam: {
    id: 'slam',""",
    """  hf_bloody_verdict_01: {
    id: 'hf_bloody_verdict_01',
    name: 'Veredicto Sangriento',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 12,
    cost: 15,
    castTime: 0,
    cooldown: 0,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    requiresTargetHpBelow: 0.2,
    effects: [{ type: 'directDamage', min: 60, max: 75 }],
    description:
      'EVO HIGHFLY de Ejecución. Conserva umbral, Rage y daño Claude; preparación pesada y doble lectura de filo son presentación.',
  },
  hf_kings_end_01: {
    id: 'hf_kings_end_01',
    name: 'Fin del Rey',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 12,
    cost: 15,
    castTime: 0,
    cooldown: 0,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    requiresTargetHpBelow: 0.2,
    effects: [{ type: 'directDamage', min: 60, max: 75 }],
    description:
      'MUTACIÓN HIGHFLY. El sello Paladin es presentación/rider visual: nunca insta-kill; la única autoridad ofensiva sigue siendo Execute 60–75.',
  },
  slam: {
    id: 'slam',""",
)

# Shared execute-window helper: Sudden Death applies equally to BASE/EVO/MUT.
rep(
    THRESHOLD,
    """export function executeWindowBypassed(caster: ExecuteWindowCaster, abilityId: string): boolean {
  return (
    (abilityId === 'execute' && caster.auras.some((aura) => aura.kind === 'sudden_death')) ||
    paladinExecuteWindowActive(caster, abilityId) ||
    dawnsWrathHammerActive(caster, abilityId)
  );
}""",
    """const WARRIOR_EXECUTE_IDS: ReadonlySet<string> = new Set([
  'execute',
  'hf_bloody_verdict_01',
  'hf_kings_end_01',
]);

export function executeWindowBypassed(caster: ExecuteWindowCaster, abilityId: string): boolean {
  return (
    (WARRIOR_EXECUTE_IDS.has(abilityId) &&
      caster.auras.some((aura) => aura.kind === 'sudden_death')) ||
    paladinExecuteWindowActive(caster, abilityId) ||
    dawnsWrathHammerActive(caster, abilityId)
  );
}""",
)

# Free-cost + consume side must match the window bypass exactly.
rep(
    EMPOWER,
    """export const REVENGE_FREE_ABILITIES: ReadonlySet<string> = new Set(['revenge']);""",
    """export const REVENGE_FREE_ABILITIES: ReadonlySet<string> = new Set(['revenge']);

export const WARRIOR_EXECUTE_ABILITIES: ReadonlySet<string> = new Set([
  'execute',
  'hf_bloody_verdict_01',
  'hf_kings_end_01',
]);""",
)

rep(
    EMPOWER,
    """    if (aura.kind === 'sudden_death' && abilityId === 'execute') return true;""",
    """    if (aura.kind === 'sudden_death' && WARRIOR_EXECUTE_ABILITIES.has(abilityId)) return true;""",
)

rep(
    EMPOWER,
    """  if (abilityId === 'execute' && consumeAuraKind(ctx, e, 'sudden_death') !== null) return true;""",
    """  if (
    WARRIOR_EXECUTE_ABILITIES.has(abilityId) &&
    consumeAuraKind(ctx, e, 'sudden_death') !== null
  )
    return true;""",
)

TEST.write_text(
    """import { describe, expect, it } from 'vitest';
import { MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import type { Entity, SimEvent } from '../src/sim/types';
import { EMPTY_TEST_WORLD } from './sim_shared';

const IDS = ['execute','hf_bloody_verdict_01','hf_kings_end_01'] as const;

function makeWarrior(seed: number): Sim {
  const sim=new Sim({seed,playerClass:'warrior',autoEquip:true,world:EMPTY_TEST_WORLD});
  sim.setPlayerLevel(20);
  sim.player.resource=sim.player.maxResource;
  sim.player.hitBonus=1;
  return sim;
}

function addTarget(sim: Sim, hpFrac: number): Entity {
  const target=createMob(
    (sim as unknown as {nextId:number}).nextId++,
    MOBS.ridge_stalker,
    20,
    {x:sim.player.pos.x,y:sim.player.pos.y,z:sim.player.pos.z+2.5},
  );
  target.maxHp=10_000;
  target.hp=Math.floor(target.maxHp*hpFrac);
  target.weapon.min=0;
  target.weapon.max=0;
  target.weapon.speed=1000;
  target.swingTimer=1000;
  target.moveSpeed=0;
  target.hostile=true;
  sim.addEntity(target);
  sim.targetEntity(target.id);
  return target;
}

function errors(events: readonly SimEvent[]): string[] {
  return events
    .filter((event): event is Extract<SimEvent,{type:'error'}>=>event.type==='error')
    .map((event)=>event.text);
}

function armSuddenDeath(sim: Sim): void {
  sim.player.auras.push({
    id:'hf_test_sudden_death',
    name:'Sudden Death',
    kind:'sudden_death',
    value:0,
    remaining:10,
    duration:10,
    sourceId:sim.player.id,
    school:'physical',
  });
}

describe('HIGHFLY Skill Lab 2.0 Warrior Execute PRIME',()=>{
  it('BASE EVO MUT preserve exact Execute authority',()=>{
    const base=(sim: Sim,id: string)=>sim.abilityDef(id)!;
    const sim=makeWarrior(91);
    const authority=(id:string)=>{
      const d=base(sim,id);
      return {
        class:d.class,learnLevel:d.learnLevel,cost:d.cost,castTime:d.castTime,
        cooldown:d.cooldown,range:d.range,school:d.school,
        requiresTarget:d.requiresTarget,requiresTargetHpBelow:d.requiresTargetHpBelow,
        effects:d.effects,
      };
    };
    expect(authority('hf_bloody_verdict_01')).toEqual(authority('execute'));
    expect(authority('hf_kings_end_01')).toEqual(authority('execute'));
  });

  for(const id of IDS) {
    it(id+' refuses above 20% without Sudden Death before Rage spend',()=>{
      const sim=makeWarrior(100+IDS.indexOf(id));
      const target=addTarget(sim,0.8);
      const hp0=target.hp;
      const rage0=sim.player.resource;
      sim.drainEvents();
      sim.castAbility(id);
      const castErrors=errors(sim.drainEvents());

      expect(target.hp).toBe(hp0);
      expect(sim.player.resource).toBe(rage0);
      expect(castErrors).toContain('That ability requires the target below 20% health.');
    });

    it(id+' executes normally below 20% and spends exactly 15 Rage',()=>{
      const sim=makeWarrior(110+IDS.indexOf(id));
      const target=addTarget(sim,0.19);
      const hp0=target.hp;
      const rage0=sim.player.resource;
      sim.castAbility(id);

      expect(target.hp).toBeLessThan(hp0);
      expect(hp0-target.hp).toBeGreaterThanOrEqual(60);
      expect(hp0-target.hp).toBeLessThanOrEqual(75);
      expect(sim.player.resource).toBe(rage0-15);
    });

    it(id+' Sudden Death bypasses threshold, makes cast free, and consumes once',()=>{
      const sim=makeWarrior(120+IDS.indexOf(id));
      const target=addTarget(sim,0.8);
      armSuddenDeath(sim);
      const hp0=target.hp;
      const rage0=sim.player.resource;
      sim.castAbility(id);

      expect(target.hp).toBeLessThan(hp0);
      expect(sim.player.resource).toBe(rage0);
      expect(sim.player.auras.some((a)=>a.kind==='sudden_death')).toBe(false);

      // Same target is still >20%. The consumed proc must not leak to a second cast.
      sim.player.gcdRemaining=0;
      const hp1=target.hp;
      sim.drainEvents();
      sim.castAbility(id);
      const secondErrors=errors(sim.drainEvents());
      expect(target.hp).toBe(hp1);
      expect(secondErrors).toContain('That ability requires the target below 20% health.');
    });
  }

  it('Fin del Rey remains bounded direct damage, never an automatic kill rider',()=>{
    const sim=makeWarrior(140);
    const target=addTarget(sim,0.19);
    const hp0=target.hp;
    sim.castAbility('hf_kings_end_01');

    expect(target.dead).toBe(false);
    expect(target.hp).toBeGreaterThan(0);
    expect(hp0-target.hp).toBeGreaterThanOrEqual(60);
    expect(hp0-target.hp).toBeLessThanOrEqual(75);
  });
});
""",
    encoding='utf-8',
)

print('HIGHFLY_SKILL_LAB2_WP_EXECUTE=1')
