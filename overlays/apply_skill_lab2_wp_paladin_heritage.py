from pathlib import Path

ROOT=Path('.')
CLASSES=ROOT/'src/sim/content/classes.ts'
TEST=ROOT/'tests/highfly_skill_lab2_wp_paladin_heritage.test.ts'

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
# HIGHFLY Skill Lab 2.0 — Warrior + Paladin Heritage BASE adapters
#
# Principal authority stays Warrior:
# - class/body/weapon/equipment = Warrior
# - resource = Rage only
# - no Mana or Devotion HUD/state is imported
#
# Lab resource translation (balance provisional, adapter behavior authoritative):
# - Holy Ground:  Mana 35  -> Rage 20
# - Valkyr:       Mana 50  -> Rage 35
# - Aegis:        Mana 150 -> Rage 60
#
# Paladin mechanics are reused, not reimplemented:
# - groundAoE zone/ticks/threat
# - Valkyr's real 2 sec flight + swept landing + landing AoE
# - Aegis real channel/heal/DR/final heal/speed + cancel cleanup
# ---------------------------------------------------------------------------

insert_roster_after(
    'execute',
    ['hf_wp_consecration_01','hf_wp_valkyrs_calling_01','hf_wp_aegis_first_dawn_01'],
)

rep(
    CLASSES,
    """  slam: {
    id: 'slam',""",
    """  hf_wp_consecration_01: {
    id: 'hf_wp_consecration_01',
    name: 'Tierra Consagrada',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 5,
    cost: 20,
    castTime: 0,
    cooldown: 12,
    range: 0,
    school: 'holy',
    requiresTarget: false,
    threat: { mult: 1.75 },
    effects: [
      {
        type: 'groundAoE',
        min: 9,
        max: 12,
        radius: 6,
        duration: 9,
        interval: 1,
      },
    ],
    description:
      'Heritage Paladin adaptada a Warrior. Reutiliza Holy Ground real con Rage; no genera Devotion ni cambia arma/cuerpo.',
  },
  hf_wp_valkyrs_calling_01: {
    id: 'hf_wp_valkyrs_calling_01',
    name: 'Llamado de Valquiria',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 13,
    cost: 35,
    castTime: 0,
    cooldown: 60,
    range: 20,
    school: 'holy',
    projectile: false,
    requiresTarget: true,
    effects: [{ type: 'valkyrsCalling', min: 150, max: 180, radius: 8, softCap: 5 }],
    description:
      'Heritage Paladin adaptada a Warrior. Reutiliza el vuelo/landing real con Rage y cuerpo/arma Warrior; Devotion permanece ausente.',
  },
  hf_wp_aegis_first_dawn_01: {
    id: 'hf_wp_aegis_first_dawn_01',
    name: 'Égida del Primer Alba',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 60,
    castTime: 0,
    cooldown: 180,
    range: 0,
    school: 'holy',
    requiresTarget: false,
    channel: { duration: 5, ticks: 5 },
    effects: [
      {
        type: 'paladinAegis',
        radius: 10,
        tickMin: 35,
        tickMax: 45,
        finalMin: 120,
        finalMax: 150,
        damageReduction: 0.5,
        speedMult: 1.3,
        speedDuration: 4,
      },
    ],
    description:
      'Heritage Paladin adaptada a Warrior. Reutiliza channel/heal/DR/final heal/speed con Rage y cleanup nativo.',
  },
  slam: {
    id: 'slam',""",
)

TEST.write_text(
    """import { describe, expect, it } from 'vitest';
import { cancelCast } from '../src/sim/combat/casting_lifecycle';
import { ABILITIES, MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import type { SimContext } from '../src/sim/sim_context';
import type { Entity } from '../src/sim/types';
import { EMPTY_TEST_WORLD } from './sim_shared';

const HOLY_GROUND='hf_wp_consecration_01';
const VALKYR='hf_wp_valkyrs_calling_01';
const AEGIS='hf_wp_aegis_first_dawn_01';
const HERITAGE=[HOLY_GROUND,VALKYR,AEGIS] as const;

function ctx(sim: Sim): SimContext {
  return (sim as unknown as {ctx:SimContext}).ctx;
}

function grantHidden(sim: Sim, id: string): void {
  const meta=sim.meta(sim.playerId)!;
  if(meta.known.some((known)=>known.def.id===id)) return;
  const def=ABILITIES[id]!;
  meta.known.push({
    def,
    rank:1,
    cost:def.cost,
    castTime:def.castTime,
    cooldown:def.cooldown,
    effects:def.effects,
    threatFlat:def.threat?.flat ?? 0,
    threatMult:def.threat?.mult ?? 1,
    bonusCharges:0,
  });
}

function makeWarrior(seed: number): Sim {
  const sim=new Sim({seed,playerClass:'warrior',autoEquip:true,world:EMPTY_TEST_WORLD});
  sim.setPlayerLevel(20);
  sim.player.resource=sim.player.maxResource;
  sim.player.hitBonus=1;
  for(const id of HERITAGE) grantHidden(sim,id);
  return sim;
}

function addTarget(sim: Sim, z=3): Entity {
  const target=createMob(
    (sim as unknown as {nextId:number}).nextId++,
    MOBS.ridge_stalker,
    20,
    {x:sim.player.pos.x,y:sim.player.pos.y,z:sim.player.pos.z+z},
  );
  target.maxHp=100_000;
  target.hp=target.maxHp;
  target.weapon.min=0;
  target.weapon.max=0;
  target.weapon.speed=1000;
  target.swingTimer=1000;
  target.moveSpeed=0;
  target.hostile=true;
  sim.addEntity(target);
  return target;
}

function tickSeconds(sim: Sim, seconds: number): void {
  for(let i=0;i<Math.ceil(seconds*20)+2;i++) sim.tick();
}

describe('HIGHFLY Skill Lab 2.0 Warrior + Paladin Heritage BASE adapters',()=>{
  it('keeps all three Heritage abilities on Warrior with Rage-only costs',()=>{
    expect(ABILITIES[HOLY_GROUND]).toMatchObject({class:'warrior',cost:20,cooldown:12});
    expect(ABILITIES[VALKYR]).toMatchObject({class:'warrior',cost:35,cooldown:60});
    expect(ABILITIES[AEGIS]).toMatchObject({class:'warrior',cost:60,cooldown:180});

    const sim=makeWarrior(201);
    expect(sim.meta(sim.playerId)?.cls).toBe('warrior');
    expect(sim.player.resourceType).toBe('rage');
    expect(sim.player.maxResource).toBe(100);
    expect(sim.player.paladinDevotion).toBeFalsy();
  });

  it('Tierra Consagrada reuses real groundAoE with Rage and never creates Devotion',()=>{
    const sim=makeWarrior(202);
    const target=addTarget(sim,3);
    const p=sim.player;
    const equipmentBefore=JSON.stringify(sim.meta(sim.playerId)?.equipment);
    const rage0=p.resource;
    const hp0=target.hp;

    sim.castAbility(HOLY_GROUND);

    expect(p.resource).toBe(rage0-20);
    expect(ctx(sim).groundAoEs.some((zone)=>zone.sourceId===p.id && zone.radius===6)).toBe(true);
    expect(ABILITIES[HOLY_GROUND]!.effects[0]).not.toHaveProperty('devotionOnFirstHit');

    tickSeconds(sim,1.2);

    expect(target.hp).toBeLessThan(hp0);
    expect(p.paladinDevotion).toBeFalsy();
    expect(p.resourceType).toBe('rage');
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipmentBefore);
  });

  it("Llamado de Valquiria keeps Warrior identity through real flight and lands cleanly",()=>{
    const sim=makeWarrior(203);
    const target=addTarget(sim,10);
    const p=sim.player;
    const equipmentBefore=JSON.stringify(sim.meta(sim.playerId)?.equipment);
    const rage0=p.resource;
    const hp0=target.hp;

    sim.targetEntity(target.id);
    sim.castAbility(VALKYR);

    expect(p.resource).toBe(rage0-35);
    expect(p.valkyrsCalling).toBeTruthy();
    expect(p.jumping).toBe(true);

    tickSeconds(sim,2.2);

    expect(p.valkyrsCalling).toBeFalsy();
    expect(p.jumping).toBe(false);
    expect(p.onGround).toBe(true);
    expect(target.hp).toBeLessThan(hp0);
    expect(Math.hypot(p.pos.x-target.pos.x,p.pos.z-target.pos.z)).toBeLessThan(1);
    expect(p.paladinDevotion).toBeFalsy();
    expect(p.resourceType).toBe('rage');
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipmentBefore);
  });

  it('Égida del Primer Alba channels on Warrior and completes with native cleanup',()=>{
    const sim=makeWarrior(204);
    const p=sim.player;
    p.hp=Math.max(1,p.maxHp-500);
    const hp0=p.hp;
    const rage0=p.resource;
    const equipmentBefore=JSON.stringify(sim.meta(sim.playerId)?.equipment);

    sim.castAbility(AEGIS);

    expect(p.resource).toBe(rage0-60);
    expect(p.channeling).toBe(true);
    expect(p.castingAbility).toBe(AEGIS);
    expect(p.auras.some((a)=>a.kind==='shield_wall' && a.sourceId===p.id)).toBe(true);

    tickSeconds(sim,5.2);

    expect(p.channeling).toBe(false);
    expect(p.castingAbility).toBeNull();
    expect(p.hp).toBeGreaterThan(hp0);
    expect(p.auras.some((a)=>a.kind==='shield_wall' && a.sourceId===p.id)).toBe(false);
    expect(p.auras.some((a)=>a.kind==='buff_speed' && a.sourceId===p.id)).toBe(true);
    expect(p.paladinDevotion).toBeFalsy();
    expect(JSON.stringify(sim.meta(sim.playerId)?.equipment)).toBe(equipmentBefore);
  });

  it('cancelling Égida cleans barrier/state and never leaves a residual channel',()=>{
    const sim=makeWarrior(205);
    const p=sim.player;

    sim.castAbility(AEGIS);
    expect(p.channeling).toBe(true);
    expect(p.auras.some((a)=>a.kind==='shield_wall' && a.sourceId===p.id)).toBe(true);

    cancelCast(ctx(sim),p);

    expect(p.channeling).toBe(false);
    expect(p.castingAbility).toBeNull();
    expect(p.auras.some((a)=>a.kind==='shield_wall' && a.sourceId===p.id)).toBe(false);
    expect(p.paladinDevotion).toBeFalsy();
  });
});
""",
    encoding='utf-8',
)

print('HIGHFLY_SKILL_LAB2_WP_PALADIN_HERITAGE=1')
