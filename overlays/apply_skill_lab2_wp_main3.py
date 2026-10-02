from pathlib import Path

ROOT=Path('.')
CLASSES=ROOT/'src/sim/content/classes.ts'
TEST=ROOT/'tests/highfly_skill_lab2_wp_main3.test.ts'

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
# HIGHFLY Skill Lab 2.0 — Warrior+Paladin MAIN3
#
# GOLD contract from HIGHFLY_28_MUTACIONES_PRIME_GOLD_v2_ADAPTERS:
# - Heroic Leap: same point/landing/cancel authority.
# - Whirlwind: preserve exactly one aoe_echo grant with 2 charges.
# - Faultline: preserve frontal AoE + 3s stun but decouple HIGHFLY acquisition
#   from visible Protection spec.
# - BASE Claude abilities remain untouched.
# ---------------------------------------------------------------------------

insert_roster_after('faultline', ['hf_seismic_fault_01','hf_world_fracture_01'])
insert_roster_after('heroic_leap', ['hf_demolishing_leap_01','hf_ascending_cataclysm_01'])
insert_roster_after('whirlwind', ['hf_cutting_whirlwind_01','hf_colossus_tempest_01'])

rep(
    CLASSES,
    """  rallying_cry: {
    id: 'rallying_cry',""",
    """  hf_demolishing_leap_01: {
    id: 'hf_demolishing_leap_01',
    name: 'Salto Demoledor',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 6,
    cost: 0,
    castTime: 0,
    cooldown: 30,
    range: 30,
    school: 'physical',
    requiresTarget: false,
    targetMode: 'position',
    effects: [{ type: 'repositionToAim', landingAoe: { min: 24, max: 32, radius: 6 } }],
    description:
      'EVO HIGHFLY de Salto Heroico. Conserva punto de caída, alcance, cooldown y landing AoE; crater/fisuras/shockwave son presentación.',
  },
  hf_ascending_cataclysm_01: {
    id: 'hf_ascending_cataclysm_01',
    name: 'Cataclismo Ascendente',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 6,
    cost: 0,
    castTime: 0,
    cooldown: 30,
    range: 30,
    school: 'physical',
    requiresTarget: false,
    targetMode: 'position',
    effects: [{ type: 'repositionToAim', landingAoe: { min: 24, max: 32, radius: 6 } }],
    description:
      'MUTACIÓN HIGHFLY. Mantiene la autoridad de Salto Heroico; la radiancia Paladin asciende sólo desde las fisuras del impacto Warrior.',
  },
  rallying_cry: {
    id: 'rallying_cry',""",
)

rep(
    CLASSES,
    """  berserker_rage: {
    id: 'berserker_rage',""",
    """  hf_cutting_whirlwind_01: {
    id: 'hf_cutting_whirlwind_01',
    name: 'Torbellino Cortante',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 0,
    castTime: 0,
    cooldown: 10,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    effects: [
      { type: 'aoeDamage', min: 30, max: 42, radius: 8 },
      {
        type: 'selfBuff',
        kind: 'aoe_echo',
        value: 0,
        duration: 12,
        charges: 2,
        auraId: 'bladed_echo',
        auraName: 'Bladed Echo',
      },
    ],
    description:
      'EVO HIGHFLY de Torbellino. Conserva el AoE y arma exactamente 2 cargas de Bladed Echo una sola vez.',
  },
  hf_colossus_tempest_01: {
    id: 'hf_colossus_tempest_01',
    name: 'Tempestad del Coloso',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 0,
    castTime: 0,
    cooldown: 10,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    effects: [
      { type: 'aoeDamage', min: 30, max: 42, radius: 8 },
      {
        type: 'selfBuff',
        kind: 'aoe_echo',
        value: 0,
        duration: 12,
        charges: 2,
        auraId: 'bladed_echo',
        auraName: 'Bladed Echo',
      },
    ],
    description:
      'MUTACIÓN HIGHFLY. La corona radiante Paladin es presentación; el AoE y las 2 cargas de Bladed Echo siguen siendo la única autoridad.',
  },
  berserker_rage: {
    id: 'berserker_rage',""",
)

rep(
    CLASSES,
    """  defiant_bellow: {
    id: 'defiant_bellow',""",
    """  hf_seismic_fault_01: {
    id: 'hf_seismic_fault_01',
    name: 'Falla Sísmica',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 14,
    cost: 15,
    castTime: 0,
    cooldown: 30,
    range: 0,
    school: 'physical',
    requiresTarget: false,
    effects: [
      {
        type: 'aoeDamage',
        min: 15,
        max: 20,
        radius: 8,
        frontal: true,
        stunSec: 3,
      },
    ],
    description:
      'EVO HIGHFLY de Falla. Conserva frontal AoE, radio y stun de 3 sec; la adquisición HIGHFLY no exige spec Protection visible.',
  },
  hf_world_fracture_01: {
    id: 'hf_world_fracture_01',
    name: 'Fractura del Mundo',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 14,
    cost: 15,
    castTime: 0,
    cooldown: 30,
    range: 0,
    school: 'physical',
    requiresTarget: false,
    effects: [
      {
        type: 'aoeDamage',
        min: 15,
        max: 20,
        radius: 8,
        frontal: true,
        stunSec: 3,
      },
    ],
    description:
      'MUTACIÓN HIGHFLY. Las runas de juicio siguen la geometría Warrior; frontal AoE y stun permanecen autoridad Claude.',
  },
  defiant_bellow: {
    id: 'defiant_bellow',""",
)

TEST.write_text(
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';

const HERO = ['heroic_leap','hf_demolishing_leap_01','hf_ascending_cataclysm_01'] as const;
const WHIRL = ['whirlwind','hf_cutting_whirlwind_01','hf_colossus_tempest_01'] as const;
const FAULT = ['faultline','hf_seismic_fault_01','hf_world_fracture_01'] as const;

function core(id: string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class, learnLevel:d.learnLevel, cost:d.cost, castTime:d.castTime,
    cooldown:d.cooldown, range:d.range, school:d.school,
    requiresTarget:d.requiresTarget, targetMode:d.targetMode, effects:d.effects,
  };
}

describe('HIGHFLY Skill Lab 2.0 Warrior+Paladin MAIN3',()=>{
  it('Salto Heroico BASE EVO MUT preserve exact movement and landing authority',()=>{
    expect(core(HERO[1])).toEqual(core(HERO[0]));
    expect(core(HERO[2])).toEqual(core(HERO[0]));
    for(const id of HERO.slice(1)) {
      expect(ABILITIES[id]!.effects).toEqual([
        {type:'repositionToAim',landingAoe:{min:24,max:32,radius:6}},
      ]);
    }
  });

  it('Torbellino BASE EVO MUT arm exactly one 2-charge Bladed Echo grant',()=>{
    for(const id of WHIRL) {
      const d=ABILITIES[id]!;
      const echoes=d.effects.filter((e)=>e.type==='selfBuff' && e.kind==='aoe_echo');
      expect(echoes).toHaveLength(1);
      expect(echoes[0]).toMatchObject({
        kind:'aoe_echo',charges:2,duration:12,auraId:'bladed_echo',
      });
      expect(d.effects.filter((e)=>e.type==='aoeDamage')).toHaveLength(1);
    }
    expect(core(WHIRL[1])).toEqual({...core(WHIRL[0])});
    expect(core(WHIRL[2])).toEqual({...core(WHIRL[0])});
  });

  it('Falla HIGHFLY decouples acquisition from Protection while preserving frontal stun authority',()=>{
    expect(ABILITIES.faultline!.specs).toEqual(['prot']);
    for(const id of FAULT.slice(1)) {
      const d=ABILITIES[id]!;
      expect(d.specs).toBeUndefined();
      expect(d.effects).toEqual([
        {type:'aoeDamage',min:15,max:20,radius:8,frontal:true,stunSec:3},
      ]);
      expect({...core(id)}).toEqual({...core('faultline')});
    }
  });

  it('Whirlwind HIGHFLY endpoints are spec-decoupled without altering native BASE',()=>{
    expect(ABILITIES.whirlwind!.specs).toEqual(['fury']);
    expect(ABILITIES.hf_cutting_whirlwind_01!.specs).toBeUndefined();
    expect(ABILITIES.hf_colossus_tempest_01!.specs).toBeUndefined();
  });

  it('registers all six endpoints in Warrior only',()=>{
    const ids=[
      'hf_demolishing_leap_01','hf_ascending_cataclysm_01',
      'hf_cutting_whirlwind_01','hf_colossus_tempest_01',
      'hf_seismic_fault_01','hf_world_fracture_01',
    ];
    const kit=new Set(CLASSES.warrior.abilities);
    for(const id of ids) {
      expect(kit.has(id)).toBe(true);
      expect(ABILITIES[id]!.hiddenFromPlayer).toBe(true);
      for(const [cls,def] of Object.entries(CLASSES)) if(cls!=='warrior') {
        expect(def.abilities.includes(id)).toBe(false);
      }
    }
  });
});
""",
    encoding='utf-8',
)

print('HIGHFLY_SKILL_LAB2_WP_MAIN3=1')
