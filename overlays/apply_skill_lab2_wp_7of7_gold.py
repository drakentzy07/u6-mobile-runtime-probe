from pathlib import Path

ROOT=Path('.')
CLASSES=ROOT/'src/sim/content/classes.ts'

def read(path: str) -> str:
    return (ROOT/path).read_text(encoding='utf-8')

def write(path: str, text: str) -> None:
    p=ROOT/path
    p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(text,encoding='utf-8')

def rep(path: str, old: str, new: str) -> None:
    text=read(path)
    n=text.count(old)
    if n!=1:
        raise SystemExit(f'{path}: expected 1 anchor, found {n}: {old[:180]!r}')
    write(path,text.replace(old,new,1))

def insert_after(ability_id: str, additions: list[str]) -> None:
    text=read('src/sim/content/classes.ts')
    anchor=f"      '{ability_id}',"
    if text.count(anchor)!=1:
        raise SystemExit(f'expected one roster anchor for {ability_id}')
    block=anchor+''.join(f"\n      '{x}'," for x in additions)
    write('src/sim/content/classes.ts',text.replace(anchor,block,1))

# ---------------------------------------------------------------------------
# Warrior + Paladin 7/7 GOLD closure
# Execute gets presentation only; its SIM parity lives in wp_execute overlay.
# Heritage EVO/MUT keep the exact adapted BASE authority. No extra hits.
# ---------------------------------------------------------------------------

insert_after('hf_wp_consecration_01',['hf_wp_radiant_sanctuary_01','hf_wp_dawn_domain_01'])
insert_after('hf_wp_valkyrs_calling_01',['hf_wp_valkyr_descent_01','hf_wp_divine_descent_01'])
insert_after('hf_wp_aegis_first_dawn_01',['hf_wp_dawn_aegis_01','hf_wp_unbreakable_dawn_01'])

rep(
    'src/sim/content/classes.ts',
    """  hf_wp_valkyrs_calling_01: {
    id: 'hf_wp_valkyrs_calling_01',""",
    """  hf_wp_radiant_sanctuary_01: {
    id: 'hf_wp_radiant_sanctuary_01',
    name: 'Santuario Radiante',
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
    effects: [{ type:'groundAoE', min:9, max:12, radius:6, duration:9, interval:1 }],
    description:'EVO Heritage de Tierra Consagrada. Mismo groundAoE autoritativo; runas, perímetro y expansión luminosa son presentación.',
  },
  hf_wp_dawn_domain_01: {
    id: 'hf_wp_dawn_domain_01',
    name: 'Dominio del Alba',
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
    effects: [{ type:'groundAoE', min:9, max:12, radius:6, duration:9, interval:1 }],
    description:'MUT Heritage. Apertura, dominio estable y amanecer final son presentación; los ticks siguen siendo exactamente los del groundAoE adaptado.',
  },
  hf_wp_valkyrs_calling_01: {
    id: 'hf_wp_valkyrs_calling_01',""",
)

rep(
    'src/sim/content/classes.ts',
    """  hf_wp_aegis_first_dawn_01: {
    id: 'hf_wp_aegis_first_dawn_01',""",
    """  hf_wp_valkyr_descent_01: {
    id: 'hf_wp_valkyr_descent_01',
    name: 'Descenso de Valquiria',
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
    effects: [{ type:'valkyrsCalling', min:150, max:180, radius:8, softCap:5 }],
    description:'EVO Heritage. Reutiliza vuelo y landing Valkyr reales con cuerpo Warrior; la silueta alada es visual.',
  },
  hf_wp_divine_descent_01: {
    id: 'hf_wp_divine_descent_01',
    name: 'Descenso Divino',
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
    effects: [{ type:'valkyrsCalling', min:150, max:180, radius:8, softCap:5 }],
    description:'MUT Heritage. Warrior y manifestación caen como una silueta; el landing AoE real sigue siendo la única autoridad.',
  },
  hf_wp_aegis_first_dawn_01: {
    id: 'hf_wp_aegis_first_dawn_01',""",
)

rep(
    'src/sim/content/classes.ts',
    """  slam: {
    id: 'slam',""",
    """  hf_wp_dawn_aegis_01: {
    id: 'hf_wp_dawn_aegis_01',
    name: 'Égida del Alba',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 60,
    castTime: 0,
    cooldown: 180,
    range: 0,
    school: 'holy',
    requiresTarget: false,
    channel: { duration:5, ticks:5 },
    effects: [{ type:'paladinAegis', radius:10, tickMin:35, tickMax:45, finalMin:120, finalMax:150, damageReduction:0.5, speedMult:1.3, speedDuration:4 }],
    description:'EVO Heritage. Misma Égida autoritativa con Warrior guard; placas, pulsos y reconstrucción son presentación.',
  },
  hf_wp_unbreakable_dawn_01: {
    id: 'hf_wp_unbreakable_dawn_01',
    name: 'Amanecer Inquebrantable',
    class: 'warrior',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 60,
    castTime: 0,
    cooldown: 180,
    range: 0,
    school: 'holy',
    requiresTarget: false,
    channel: { duration:5, ticks:5 },
    effects: [{ type:'paladinAegis', radius:10, tickMin:35, tickMax:45, finalMin:120, finalMax:150, damageReduction:0.5, speedMult:1.3, speedDuration:4 }],
    description:'MUT Heritage. Cúpula frontal y dawn burst son visuales; heal/DR/final heal/speed y cancel cleanup siguen siendo Claude.',
  },
  slam: {
    id: 'slam',""",
)

# Presentation routes: Warrior body always wins.
rep(
    'src/highfly/presentation_adapter.ts',
    """  hf_world_fracture_01: {
    animationRoute: 'faultline',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_world_fracture_01',
    sfxRoute: 'faultline',
  },""",
    """  hf_world_fracture_01: {
    animationRoute: 'faultline',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_world_fracture_01',
    sfxRoute: 'faultline',
  },
  hf_bloody_verdict_01: { animationRoute:'execute', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_bloody_verdict_01', sfxRoute:'execute' },
  hf_kings_end_01: { animationRoute:'execute', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_kings_end_01', sfxRoute:'execute' },
  hf_wp_consecration_01: { animationRoute:'faultline', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_wp_consecration_01', sfxRoute:'consecration' },
  hf_wp_radiant_sanctuary_01: { animationRoute:'faultline', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_wp_radiant_sanctuary_01', sfxRoute:'consecration' },
  hf_wp_dawn_domain_01: { animationRoute:'faultline', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_wp_dawn_domain_01', sfxRoute:'consecration' },
  hf_wp_valkyrs_calling_01: { animationRoute:'heroic_leap', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_wp_valkyrs_calling_01', sfxRoute:'valkyrs_calling' },
  hf_wp_valkyr_descent_01: { animationRoute:'heroic_leap', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_wp_valkyr_descent_01', sfxRoute:'valkyrs_calling' },
  hf_wp_divine_descent_01: { animationRoute:'heroic_leap', visualHitMoments:[{event:'impact',normalizedTime:1}], vfxRoute:'hf_wp_divine_descent_01', sfxRoute:'valkyrs_calling' },
  hf_wp_aegis_first_dawn_01: { animationRoute:'raised_guard', visualHitMoments:[], vfxRoute:'hf_wp_aegis_first_dawn_01', sfxRoute:'aegis_first_dawn' },
  hf_wp_dawn_aegis_01: { animationRoute:'raised_guard', visualHitMoments:[], vfxRoute:'hf_wp_dawn_aegis_01', sfxRoute:'aegis_first_dawn' },
  hf_wp_unbreakable_dawn_01: { animationRoute:'raised_guard', visualHitMoments:[], vfxRoute:'hf_wp_unbreakable_dawn_01', sfxRoute:'aegis_first_dawn' },""",
)

write(
  'src/highfly/skill_lab2_wp_7of7_vfx.ts',
  """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_WP_7OF7_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_bloody_verdict_01:{c:'#b94532',p:'steel',pw:1.35,sp:26,rg:1.0,sm:1,li:0.65,lg:0.9,wu:0.1,a:'strike'},
  hf_kings_end_01:{c:'#ffd76e',p:'holy',pw:1.65,sp:40,rg:1.2,vr:1,sm:1,li:1.05,lg:1.25,wu:0.14,fin:1,a:'strike'},
  hf_wp_consecration_01:{c:'#e0b94d',p:'holy',pw:1.05,sp:18,rg:1.1,li:0.45,lg:1.1,a:'nova'},
  hf_wp_radiant_sanctuary_01:{c:'#f1ce68',p:'holy',pw:1.35,sp:28,rg:1.3,vr:1,li:0.75,lg:1.4,a:'nova'},
  hf_wp_dawn_domain_01:{c:'#fff09d',p:'holy',pw:1.65,sp:42,rg:1.5,vr:1,bl:1,li:1.1,lg:1.8,fin:1,a:'nova'},
  hf_wp_valkyrs_calling_01:{c:'#e3c56a',p:'holy',pw:1.1,sp:20,rg:1.0,li:0.5,lg:1.0,a:'dash'},
  hf_wp_valkyr_descent_01:{c:'#f2d67b',p:'holy',pw:1.4,sp:30,rg:1.2,vr:1,li:0.82,lg:1.3,a:'dash'},
  hf_wp_divine_descent_01:{c:'#fff0ad',p:'holy',pw:1.75,sp:46,rg:1.45,vr:1,bl:1,li:1.18,lg:1.6,fin:1,a:'dash'},
  hf_wp_aegis_first_dawn_01:{c:'#d9be63',p:'holy',pw:1.0,sp:12,rg:1.0,li:0.45,lg:1.0,a:'buff'},
  hf_wp_dawn_aegis_01:{c:'#efd777',p:'holy',pw:1.3,sp:22,rg:1.25,vr:1,li:0.72,lg:1.35,a:'buff'},
  hf_wp_unbreakable_dawn_01:{c:'#fff1a9',p:'holy',pw:1.6,sp:34,rg:1.5,vr:1,bl:1,li:1.05,lg:1.65,fin:1,a:'buff'},
};

export const HF_WP_7OF7_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_bloody_verdict_01:{archetype:'strike',palette:'physical',power:1.35,windup:0.1,windupStyle:'weapon',motifs:['cross'],motifAt:'target',motifR:1.4,strike:{swings:2,arc:'vertical'},impact:{ring:false,vRing:0.6,sparks:26,smoke:true,light:0.65,focused:true},linger:0.9,rim:'#d48b78',accent:'#ffffff'},
  hf_kings_end_01:{archetype:'strike',palette:'holy',power:1.65,windup:0.14,windupStyle:'runes',motifs:['cross','implosion'],motifAt:'target',motifR:1.8,strike:{swings:2,arc:'vertical'},impact:{ring:false,vRing:1,sparks:40,smoke:true,light:1.05,focused:true},decal:'rune',linger:1.25,rim:'#ffe998',accent:'#ffffff',screenFx:true,finisher:true},
  hf_wp_consecration_01:{archetype:'nova',palette:'holy',power:1.05,motifs:['cross'],motifAt:'caster',motifR:2,nova:{radius:6},impact:{ring:1,vRing:false,sparks:18,light:0.45},decal:'rune',linger:1.1,rim:'#e8d17d'},
  hf_wp_radiant_sanctuary_01:{archetype:'nova',palette:'holy',power:1.35,chargeStreams:2,motifs:['cross','pillars'],motifAt:'caster',motifR:2.3,nova:{radius:6},impact:{ring:1.2,vRing:0.8,sparks:28,light:0.75},shaft:true,decal:'rune',linger:1.4,rim:'#ffe99a',accent:'#ffffff'},
  hf_wp_dawn_domain_01:{archetype:'nova',palette:'holy',power:1.65,chargeStreams:3,motifs:['cross','pillars'],motifAt:'caster',motifR:2.6,nova:{radius:6},impact:{ring:1.4,vRing:1,sparks:42,light:1.1},shaft:true,decal:'rune',linger:1.8,rim:'#fff2b5',accent:'#ffffff',screenFx:true,finisher:true},
  hf_wp_valkyrs_calling_01:{archetype:'dash',palette:'holy',power:1.1,motifs:['cross'],motifAt:'caster',motifR:1.2,impact:{ring:1,vRing:false,sparks:20,smoke:true,light:0.5},linger:1,rim:'#e9d28a'},
  hf_wp_valkyr_descent_01:{archetype:'dash',palette:'holy',power:1.4,chargeStreams:2,motifs:['cross','pillars'],motifAt:'caster',motifR:1.8,impact:{ring:1.2,vRing:0.8,sparks:30,smoke:true,light:0.82},shaft:true,decal:'rune',linger:1.3,rim:'#ffe99d',accent:'#ffffff'},
  hf_wp_divine_descent_01:{archetype:'dash',palette:'holy',power:1.75,chargeStreams:3,motifs:['cross','pillars'],motifAt:'caster',motifR:2.2,impact:{ring:1.4,vRing:1,sparks:46,smoke:true,light:1.18},shaft:true,decal:'rune',linger:1.6,rim:'#fff2bd',accent:'#ffffff',screenFx:true,finisher:true},
  hf_wp_aegis_first_dawn_01:{archetype:'buff',palette:'holy',power:1.0,motifs:['cross'],motifAt:'caster',motifR:1.6,impact:{ring:1,vRing:false,sparks:12,light:0.45},linger:1,rim:'#e4ce83'},
  hf_wp_dawn_aegis_01:{archetype:'buff',palette:'holy',power:1.3,chargeStreams:2,motifs:['cross','pillars'],motifAt:'caster',motifR:2,impact:{ring:1.2,vRing:0.7,sparks:22,light:0.72},shaft:true,linger:1.35,rim:'#ffe89a',accent:'#ffffff'},
  hf_wp_unbreakable_dawn_01:{archetype:'buff',palette:'holy',power:1.6,chargeStreams:3,motifs:['cross','pillars'],motifAt:'caster',motifR:2.4,impact:{ring:1.4,vRing:1,sparks:34,light:1.05},shaft:true,decal:'rune',linger:1.65,rim:'#fff1b2',accent:'#ffffff',screenFx:true,finisher:true},
};
"""
)

rep(
  'src/render/ability_vfx_registry.ts',
  """import {
  HF_WP_MAIN3_VFX_FULL_SPEC,
  HF_WP_MAIN3_VFX_SPEC,
} from '../highfly/skill_lab2_wp_main3_vfx';""",
  """import {
  HF_WP_MAIN3_VFX_FULL_SPEC,
  HF_WP_MAIN3_VFX_SPEC,
} from '../highfly/skill_lab2_wp_main3_vfx';
import {
  HF_WP_7OF7_VFX_FULL_SPEC,
  HF_WP_7OF7_VFX_SPEC,
} from '../highfly/skill_lab2_wp_7of7_vfx';"""
)

rep(
  'src/render/ability_vfx_registry.ts',
  """  if (HF_WP_MAIN3_VFX_SPEC[abilityId]) return HF_WP_MAIN3_VFX_SPEC[abilityId];""",
  """  if (HF_WP_MAIN3_VFX_SPEC[abilityId]) return HF_WP_MAIN3_VFX_SPEC[abilityId];
  if (HF_WP_7OF7_VFX_SPEC[abilityId]) return HF_WP_7OF7_VFX_SPEC[abilityId];"""
)

rep(
  'src/render/ability_vfx_registry.ts',
  """  if (HF_WP_MAIN3_VFX_FULL_SPEC[abilityId]) return HF_WP_MAIN3_VFX_FULL_SPEC[abilityId];""",
  """  if (HF_WP_MAIN3_VFX_FULL_SPEC[abilityId]) return HF_WP_MAIN3_VFX_FULL_SPEC[abilityId];
  if (HF_WP_7OF7_VFX_FULL_SPEC[abilityId]) return HF_WP_7OF7_VFX_FULL_SPEC[abilityId];"""
)

write(
  'tests/highfly_skill_lab2_wp_7of7_gold.test.ts',
  """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const LINES=[
  ['hf_wp_consecration_01','hf_wp_radiant_sanctuary_01','hf_wp_dawn_domain_01'],
  ['hf_wp_valkyrs_calling_01','hf_wp_valkyr_descent_01','hf_wp_divine_descent_01'],
  ['hf_wp_aegis_first_dawn_01','hf_wp_dawn_aegis_01','hf_wp_unbreakable_dawn_01'],
] as const;

function authority(id:string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class,cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,range:d.range,
    school:d.school,requiresTarget:d.requiresTarget,channel:d.channel,effects:d.effects,threat:d.threat,
  };
}

describe('HIGHFLY Warrior Paladin 7/7 GOLD closure',()=>{
  it('preserves Heritage BASE authority in EVO/MUT and Warrior identity',()=>{
    for(const [base,evo,mut] of LINES) {
      expect(authority(evo)).toEqual(authority(base));
      expect(authority(mut)).toEqual(authority(base));
      expect(CLASSES.warrior.abilities).toEqual(expect.arrayContaining([base,evo,mut]));
      expect(ABILITIES[evo]!.hiddenFromPlayer).toBe(true);
      expect(ABILITIES[mut]!.hiddenFromPlayer).toBe(true);
    }
  });

  it('keeps Execute presentation on Warrior body and Holy rider visual-only',()=>{
    expect(highflyPresentationRoute('hf_bloody_verdict_01','animation')).toBe('execute');
    expect(highflyPresentationRoute('hf_kings_end_01','animation')).toBe('execute');
    expect(abilityVfxFullSpec('hf_kings_end_01')).toMatchObject({palette:'holy',finisher:true});
    expect(ABILITIES.hf_kings_end_01!.effects).toEqual(ABILITIES.execute!.effects);
  });

  it('routes Heritage animations through Warrior body vocabulary',()=>{
    for(const id of ['hf_wp_consecration_01','hf_wp_radiant_sanctuary_01','hf_wp_dawn_domain_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('faultline');
    }
    for(const id of ['hf_wp_valkyrs_calling_01','hf_wp_valkyr_descent_01','hf_wp_divine_descent_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('heroic_leap');
    }
    for(const id of ['hf_wp_aegis_first_dawn_01','hf_wp_dawn_aegis_01','hf_wp_unbreakable_dawn_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('raised_guard');
    }
  });

  it('pins premium mutation motifs without new SIM effects',()=>{
    expect(abilityVfxFullSpec('hf_wp_dawn_domain_01')).toMatchObject({palette:'holy',finisher:true,screenFx:true});
    expect(abilityVfxFullSpec('hf_wp_divine_descent_01')).toMatchObject({palette:'holy',finisher:true,screenFx:true});
    expect(abilityVfxFullSpec('hf_wp_unbreakable_dawn_01')).toMatchObject({palette:'holy',finisher:true,screenFx:true});
  });
});
"""
)

print('HIGHFLY_SKILL_LAB2_WP_7OF7_GOLD=1')
