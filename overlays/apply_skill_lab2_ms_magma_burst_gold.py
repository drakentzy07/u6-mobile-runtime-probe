from pathlib import Path

ROOT=Path(".")

def read(path:str)->str:
    return (ROOT/path).read_text(encoding="utf-8")

def write(path:str,text:str)->None:
    p=ROOT/path
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding="utf-8")

def rep(path:str,old:str,new:str)->None:
    text=read(path)
    n=text.count(old)
    if n!=1:
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:180]!r}")
    write(path,text.replace(old,new,1))

def insert_after(ability_id:str, additions:list[str])->None:
    text=read("src/sim/content/classes.ts")
    anchor=f"      '{ability_id}',"
    if text.count(anchor)!=1:
        raise SystemExit(f"expected one roster anchor for {ability_id}")
    block=anchor+"".join(f"\n      '{x}'," for x in additions)
    write("src/sim/content/classes.ts",text.replace(anchor,block,1))

# Mage + Shaman 7/7 GOLD — Magma Burst heritage.
# Visible authority remains Mage + Mana. A hidden Cinder Jolt adapter carries
# only the prerequisite DoT needed by the real Shaman Magma Surge contract.
CINDER="hf_ms_cinder_jolt_01"
BASE="hf_ms_magma_burst_01"
EVO="hf_ms_volcanic_core_01"
MUT="hf_ms_primordial_eruption_01"

insert_after("hf_ms_judgment_sky_01",[CINDER,BASE,EVO,MUT])

rep(
    "src/sim/content/classes.ts",
    """  fingers_of_frost: {
    id: 'fingers_of_frost',""",
    """  hf_ms_cinder_jolt_01: {
    id: 'hf_ms_cinder_jolt_01',
    name: 'Cinder Jolt',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 4,
    cost: 35,
    castTime: 0,
    cooldown: 6,
    range: 20,
    school: 'fire',
    requiresTarget: true,
    effects: [
      { type: 'directDamage', min: 25, max: 25 },
      { type: 'dot', total: 28, duration: 12, interval: 3 },
    ],
    ranks: [
      {
        rank: 2,
        level: 16,
        cost: 55,
        effects: [
          { type: 'directDamage', min: 42, max: 42 },
          { type: 'dot', total: 48, duration: 12, interval: 3 },
        ],
      },
    ],
    description:
      'Soporte Heritage oculto para el contrato Magma Surge; no aparece como nueva skill equipada ni crea una barra Shaman.',
  },
  hf_ms_magma_burst_01: {
    id: 'hf_ms_magma_burst_01',
    name: 'Explosión Magmática',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 12,
    cost: 45,
    castTime: 2.0,
    cooldown: 8,
    range: 30,
    school: 'fire',
    requiresTarget: true,
    projectileFx: 'heavyBolt',
    effects: [{ type: 'directDamage', min: 63, max: 71 }],
    ranks: [
      { rank: 2, level: 20, cost: 70, effects: [{ type: 'directDamage', min: 105, max: 119 }] },
    ],
    description:
      'Heritage Shaman adaptada a Mage. Conserva Magma Burst, crítico garantizado con Cinder Jolt propio y Magma Surge interno.',
  },
  hf_ms_volcanic_core_01: {
    id: 'hf_ms_volcanic_core_01',
    name: 'Núcleo Volcánico',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 12,
    cost: 45,
    castTime: 2.0,
    cooldown: 8,
    range: 30,
    school: 'fire',
    requiresTarget: true,
    projectileFx: 'heavyBolt',
    effects: [{ type: 'directDamage', min: 63, max: 71 }],
    ranks: [
      { rank: 2, level: 20, cost: 70, effects: [{ type: 'directDamage', min: 105, max: 119 }] },
    ],
    description:
      'EVO de Explosión Magmática. Núcleo más denso, magma viscoso, debris y choque térmico; un único hit SIM.',
  },
  hf_ms_primordial_eruption_01: {
    id: 'hf_ms_primordial_eruption_01',
    name: 'Erupción Primordial',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 12,
    cost: 45,
    castTime: 2.0,
    cooldown: 8,
    range: 30,
    school: 'fire',
    requiresTarget: true,
    projectileFx: 'heavyBolt',
    effects: [{ type: 'directDamage', min: 63, max: 71 }],
    ranks: [
      { rank: 2, level: 20, cost: 70, effects: [{ type: 'directDamage', min: 105, max: 119 }] },
    ],
    description:
      'MUT PRIME Mage+Shaman. Impacto, ascenso magmático y lluvia corta de roca son coreografía; el daño sigue autorizado por un solo impacto.',
  },
  fingers_of_frost: {
    id: 'fingers_of_frost',""",
)

# Extend the native Magma Burst/Cinder Jolt engine instead of reimplementing it.
rep(
    "src/sim/combat/shaman_thundercall_kit.ts",
    """export const MAGMA_BURST_ABILITY_ID = 'lava_burst';
export const MAGMA_SURGE_CHANCE = 0.2;""",
    """export const MAGMA_BURST_ABILITY_ID = 'lava_burst';
export const HIGHFLY_MS_MAGMA_BURST_IDS: readonly string[] = [
  'hf_ms_magma_burst_01',
  'hf_ms_volcanic_core_01',
  'hf_ms_primordial_eruption_01',
];
const MAGMA_BURST_IDS: ReadonlySet<string> = new Set([
  MAGMA_BURST_ABILITY_ID,
  ...HIGHFLY_MS_MAGMA_BURST_IDS,
]);
export const MAGMA_SURGE_CHANCE = 0.2;""",
)

rep(
    "src/sim/combat/shaman_thundercall_kit.ts",
    """const CINDER_JOLT_DOT_ID = 'flame_shock';
const OVERLOAD_ABILITIES:""",
    """const CINDER_JOLT_DOT_ID = 'flame_shock';
const HIGHFLY_MS_CINDER_JOLT_DOT_ID = 'hf_ms_cinder_jolt_01';
const CINDER_JOLT_DOT_IDS: ReadonlySet<string> = new Set([
  CINDER_JOLT_DOT_ID,
  HIGHFLY_MS_CINDER_JOLT_DOT_ID,
]);
const OVERLOAD_ABILITIES:""",
)

rep(
    "src/sim/combat/shaman_thundercall_kit.ts",
    """  if (!meta || ctx.playerMods(meta).spec !== 'elemental') return null;
  return meta;""",
    """  if (!meta) return null;
  if (meta.cls !== 'mage' && ctx.playerMods(meta).spec !== 'elemental') return null;
  return meta;""",
)

rep(
    "src/sim/combat/shaman_thundercall_kit.ts",
    """function knows(ctx: SimContext, player: Entity, abilityId: string): boolean {
  const meta = thundercallMeta(ctx, player);
  return meta !== null && meta.known.some((known) => known.def.id === abilityId);
}""",
    """function knows(ctx: SimContext, player: Entity, abilityId: string): boolean {
  const meta = thundercallMeta(ctx, player);
  return meta !== null && meta.known.some((known) => known.def.id === abilityId);
}

function magmaBurstForPlayer(ctx: SimContext, player: Entity): string | null {
  const meta = thundercallMeta(ctx, player);
  if (!meta) return null;
  const known = [MAGMA_BURST_ABILITY_ID, ...HIGHFLY_MS_MAGMA_BURST_IDS].filter((abilityId) =>
    meta.known.some((entry) => entry.def.id === abilityId),
  );
  if (known.length === 0) return null;
  return known.find((abilityId) => player.cooldowns.has(abilityId)) ?? known[0] ?? null;
}""",
)

rep(
    "src/sim/combat/shaman_thundercall_kit.ts",
    """  if (abilityId !== MAGMA_BURST_ABILITY_ID || thundercallMeta(ctx, player) === null) return false;
  return target.auras.some(
    (aura) => aura.id === CINDER_JOLT_DOT_ID && aura.kind === 'dot' && aura.sourceId === player.id,
  );""",
    """  if (!MAGMA_BURST_IDS.has(abilityId) || thundercallMeta(ctx, player) === null) return false;
  return target.auras.some(
    (aura) =>
      CINDER_JOLT_DOT_IDS.has(aura.id) &&
      aura.kind === 'dot' &&
      aura.sourceId === player.id,
  );""",
)

rep(
    "src/sim/combat/shaman_thundercall_kit.ts",
    """  if (dot.id !== CINDER_JOLT_DOT_ID || !source || source.dead || landed <= 0) return;
  if (source.castingAbility === MAGMA_BURST_ABILITY_ID) return;
  if (!knows(ctx, source, MAGMA_BURST_ABILITY_ID)) return;
  if (!ctx.rng.chance(MAGMA_SURGE_CHANCE)) return;
  source.cooldowns.delete(MAGMA_BURST_ABILITY_ID);
  ctx.applyAura(source, {
    id: MAGMA_SURGE_ID,
    name: 'Magma Burst',
    kind: 'next_cast_instant',
    value: 1,
    remaining: MAGMA_SURGE_DURATION,
    duration: MAGMA_SURGE_DURATION,
    sourceId: source.id,
    school: 'fire',
    empowerAbilities: [MAGMA_BURST_ABILITY_ID],
  });""",
    """  if (!CINDER_JOLT_DOT_IDS.has(dot.id) || !source || source.dead || landed <= 0) return;
  if (source.castingAbility && MAGMA_BURST_IDS.has(source.castingAbility)) return;
  const magmaBurstId = magmaBurstForPlayer(ctx, source);
  if (!magmaBurstId) return;
  if (!ctx.rng.chance(MAGMA_SURGE_CHANCE)) return;
  source.cooldowns.delete(magmaBurstId);
  ctx.applyAura(source, {
    id: MAGMA_SURGE_ID,
    name: 'Magma Burst',
    kind: 'next_cast_instant',
    value: 1,
    remaining: MAGMA_SURGE_DURATION,
    duration: MAGMA_SURGE_DURATION,
    sourceId: source.id,
    school: 'fire',
    empowerAbilities: [magmaBurstId],
  });""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_judgment_sky_01: {
    animationRoute: 'lightning_bolt',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_judgment_sky_01',
    sfxRoute: 'lightning_bolt',
  },""",
    """  hf_ms_judgment_sky_01: {
    animationRoute: 'lightning_bolt',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_judgment_sky_01',
    sfxRoute: 'lightning_bolt',
  },
  hf_ms_magma_burst_01: {
    animationRoute: 'lava_burst',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_magma_burst_01',
    sfxRoute: 'lava_burst',
  },
  hf_ms_volcanic_core_01: {
    animationRoute: 'lava_burst',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_volcanic_core_01',
    sfxRoute: 'lava_burst',
  },
  hf_ms_primordial_eruption_01: {
    animationRoute: 'lava_burst',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_primordial_eruption_01',
    sfxRoute: 'lava_burst',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_magma_burst_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_MAGMA_BURST_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_magma_burst_01:{c:'#ff6d32',p:'fire',pw:1.35,sp:34,rg:1.4,sm:1,li:1.1,lg:2.8,wu:1.8,a:'bolt'},
  hf_ms_volcanic_core_01:{c:'#ef4a22',p:'fire',pw:1.7,sp:52,rg:1.8,sm:1,li:1.45,lg:3.4,wu:1.8,a:'bolt'},
  hf_ms_primordial_eruption_01:{c:'#d9361f',p:'fire',pw:2.05,sp:72,rg:2.4,sm:1,li:1.85,lg:4.2,wu:1.8,fin:1,a:'bolt'},
};

export const HF_MS_MAGMA_BURST_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_magma_burst_01:{
    archetype:'bolt',palette:'fire',power:1.35,windup:1.8,windupStyle:'orb',
    bolt:{speed:20,style:'rock',headScale:1.5,coils:false,jagged:false,forkEvery:0},
    linger:2.8,impact:{flipbook:true,ring:1.4,vRing:true,sparks:34,debris:true,smoke:true,light:1.1},
    rim:'#ff6d32',accent:'#ffc28e'
  },
  hf_ms_volcanic_core_01:{
    archetype:'bolt',palette:'fire',power:1.7,windup:1.8,windupStyle:'vortex',
    bolt:{speed:20,style:'rock',headScale:1.8,coils:true,jagged:false,forkEvery:0},
    motifs:['fissure'],motifAt:'target',decal:'scorch',linger:3.4,
    impact:{flipbook:true,ring:1.8,vRing:true,sparks:52,debris:true,smoke:true,light:1.45},
    rim:'#ef4a22',accent:'#ffd09e'
  },
  hf_ms_primordial_eruption_01:{
    archetype:'bolt',palette:'fire',power:2.05,windup:1.8,windupStyle:'vortex',
    bolt:{speed:20,style:'rock',headScale:2.0,coils:true,jagged:false,forkEvery:0},
    motifs:['fissure','pillars'],motifAt:'target',decal:'crack',shaft:true,chargeStreams:3,linger:4.2,
    impact:{flipbook:true,ring:2.4,vRing:true,sparks:72,debris:true,smoke:true,light:1.85},
    rim:'#d9361f',accent:'#ffe0b8',screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_ARC_BOLT_VFX_FULL_SPEC,
  HF_MS_ARC_BOLT_VFX_SPEC,
} from '../highfly/skill_lab2_ms_arc_bolt_vfx';""",
    """import {
  HF_MS_ARC_BOLT_VFX_FULL_SPEC,
  HF_MS_ARC_BOLT_VFX_SPEC,
} from '../highfly/skill_lab2_ms_arc_bolt_vfx';
import {
  HF_MS_MAGMA_BURST_VFX_FULL_SPEC,
  HF_MS_MAGMA_BURST_VFX_SPEC,
} from '../highfly/skill_lab2_ms_magma_burst_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_ARC_BOLT_VFX_SPEC[abilityId]) return HF_MS_ARC_BOLT_VFX_SPEC[abilityId];""",
    """  if (HF_MS_ARC_BOLT_VFX_SPEC[abilityId]) return HF_MS_ARC_BOLT_VFX_SPEC[abilityId];
  if (HF_MS_MAGMA_BURST_VFX_SPEC[abilityId]) return HF_MS_MAGMA_BURST_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_ARC_BOLT_VFX_FULL_SPEC[abilityId]) return HF_MS_ARC_BOLT_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_ARC_BOLT_VFX_FULL_SPEC[abilityId]) return HF_MS_ARC_BOLT_VFX_FULL_SPEC[abilityId];
  if (HF_MS_MAGMA_BURST_VFX_FULL_SPEC[abilityId]) return HF_MS_MAGMA_BURST_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_magma_burst_gold.test.ts",
    """import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  magmaBurstGuaranteedCrit,
  MAGMA_SURGE_CHANCE,
  thundercallOnDotTick,
} from '../src/sim/combat/shaman_thundercall_kit';
import { MAGMA_SURGE_ID } from '../src/sim/combat/shaman_thundercall';
import { ABILITIES, CLASSES, MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import type { Aura, Entity } from '../src/sim/types';
import { EMPTY_TEST_WORLD } from './sim_shared';

const CINDER='hf_ms_cinder_jolt_01';
const BASE='hf_ms_magma_burst_01';
const EVO='hf_ms_volcanic_core_01';
const MUT='hf_ms_primordial_eruption_01';

function core(id:string) {
  const d=ABILITIES[id]!;
  return {cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,range:d.range,school:d.school,
    requiresTarget:d.requiresTarget,projectileFx:d.projectileFx,effects:d.effects,ranks:d.ranks};
}

function grantHidden(sim:Sim,id:string):void {
  const meta=sim.meta(sim.playerId)!;
  if(meta.known.some((known)=>known.def.id===id)) return;
  const def=ABILITIES[id]!;
  meta.known.push({
    def,rank:1,cost:def.cost,castTime:def.castTime,cooldown:def.cooldown,effects:def.effects,
    threatFlat:def.threat?.flat ?? 0,threatMult:def.threat?.mult ?? 1,bonusCharges:0,
  });
}

function mage(seed:number):{sim:Sim,p:Entity,target:Entity} {
  const sim=new Sim({seed,playerClass:'mage',autoEquip:true,world:EMPTY_TEST_WORLD});
  sim.setPlayerLevel(20); sim.setSpec('fire'); sim.tick();
  for(const id of [CINDER,BASE,EVO,MUT]) grantHidden(sim,id);
  const p=sim.player; p.resource=p.maxResource; p.hitBonus=1;
  const target=createMob((sim as unknown as {nextId:number}).nextId++,MOBS.training_dummy,20,{
    x:p.pos.x,y:p.pos.y,z:p.pos.z+6,
  });
  target.hostile=true;target.maxHp=target.hp=100_000;target.weapon.min=0;target.weapon.max=0;target.weapon.speed=1000;target.swingTimer=1000;target.moveSpeed=0;
  sim.addEntity(target);sim.targetEntity(target.id);
  return {sim,p,target};
}

function cinder(p:Entity):Aura {
  return {id:CINDER,name:'Cinder Jolt',kind:'dot',value:12,remaining:12,duration:12,tickInterval:3,tickTimer:3,sourceId:p.id,school:'fire'};
}

afterEach(()=>vi.restoreAllMocks());

describe('HIGHFLY Mage Shaman 7/7 GOLD — Magma Burst',()=>{
  it('ports Magma Burst and Cinder Jolt authority onto Mage without a Shaman bar',()=>{
    expect(core(BASE)).toEqual(core('lava_burst'));
    expect(core(EVO)).toEqual(core('lava_burst'));
    expect(core(MUT)).toEqual(core('lava_burst'));
    expect(core(CINDER)).toEqual(core('flame_shock'));
    expect([CINDER,BASE,EVO,MUT].every((id)=>ABILITIES[id]!.class==='mage')).toBe(true);
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([CINDER,BASE,EVO,MUT]));
  });

  it('guarantees crit only from the caster own Cinder Jolt for all lineage stages',()=>{
    const {sim,p,target}=mage(401);
    target.auras.push({...cinder(p),sourceId:999999});
    expect(magmaBurstGuaranteedCrit(sim.ctx,p,EVO,target)).toBe(false);
    target.auras.push(cinder(p));
    for(const id of [BASE,EVO,MUT]) expect(magmaBurstGuaranteedCrit(sim.ctx,p,id,target)).toBe(true);
  });

  it('Magma Surge resets only the relevant lineage skill and scopes instant to it',()=>{
    const {sim,p}=mage(402);
    p.cooldowns.set(EVO,8);
    vi.spyOn(sim.rng,'chance').mockImplementation((chance:number)=>chance===MAGMA_SURGE_CHANCE);
    thundercallOnDotTick(sim.ctx,p,cinder(p),12);
    expect(p.cooldowns.has(EVO)).toBe(false);
    expect(p.cooldowns.has(BASE)).toBe(false);
    expect(p.cooldowns.has(MUT)).toBe(false);
    const surge=p.auras.find((a)=>a.id===MAGMA_SURGE_ID);
    expect(surge?.kind).toBe('next_cast_instant');
    expect(surge?.remaining).toBe(10);
    expect(surge?.empowerAbilities).toEqual([EVO]);
  });

  it('suppresses Surge while any Magma Burst lineage cast is already in flight',()=>{
    const {sim,p}=mage(403);
    p.castingAbility=MUT;
    const chance=vi.spyOn(sim.rng,'chance');
    thundercallOnDotTick(sim.ctx,p,cinder(p),12);
    expect(chance).not.toHaveBeenCalled();
    expect(p.auras.some((a)=>a.id===MAGMA_SURGE_ID)).toBe(false);
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_MAGMA_BURST_GOLD=1")
