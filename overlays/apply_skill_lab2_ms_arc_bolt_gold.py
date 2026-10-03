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

# Mage + Shaman 6/7 GOLD — Arc Bolt heritage.
# Mage remains the visible class/resource authority. The Shaman builder is
# copied exactly, including all ranks. Thunder is bridged as an internal aura;
# the decorative bounce/sky branches never create extra SIM hits.
BASE="hf_ms_arc_bolt_01"
EVO="hf_ms_overcharged_bolt_01"
MUT="hf_ms_judgment_sky_01"

insert_after("hf_ms_storms_end_01",[BASE,EVO,MUT])

rep(
    "src/sim/content/classes.ts",
    """  fingers_of_frost: {
    id: 'fingers_of_frost',""",
    """  hf_ms_arc_bolt_01: {
    id: 'hf_ms_arc_bolt_01',
    name: 'Rayo Arcano',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 15,
    castTime: 1.5,
    cooldown: 0,
    range: 30,
    school: 'nature',
    requiresTarget: true,
    projectileFx: 'lightning',
    effects: [{ type: 'directDamage', min: 15, max: 17 }],
    ranks: [
      { rank: 2, level: 8, cost: 25, castTime: 2.0, effects: [{ type: 'directDamage', min: 26, max: 30 }] },
      { rank: 3, level: 14, cost: 40, castTime: 2.5, effects: [{ type: 'directDamage', min: 45, max: 51 }] },
      { rank: 4, level: 20, cost: 60, castTime: 3.0, effects: [{ type: 'directDamage', min: 75, max: 85 }] },
    ],
    description:
      'Heritage Shaman adaptada a Mage. Reutiliza Arc Bolt con Mana y construye Thunder interno sin importar una barra Shaman.',
  },
  hf_ms_overcharged_bolt_01: {
    id: 'hf_ms_overcharged_bolt_01',
    name: 'Rayo Sobrecargado',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 15,
    castTime: 1.5,
    cooldown: 0,
    range: 30,
    school: 'nature',
    requiresTarget: true,
    projectileFx: 'lightning',
    effects: [{ type: 'directDamage', min: 15, max: 17 }],
    ranks: [
      { rank: 2, level: 8, cost: 25, castTime: 2.0, effects: [{ type: 'directDamage', min: 26, max: 30 }] },
      { rank: 3, level: 14, cost: 40, castTime: 2.5, effects: [{ type: 'directDamage', min: 45, max: 51 }] },
      { rank: 4, level: 20, cost: 60, castTime: 3.0, effects: [{ type: 'directDamage', min: 75, max: 85 }] },
    ],
    description:
      'EVO de Rayo Arcano. El arco se ensancha y puede mostrar un rebote visual sin daño adicional; el hit real sigue siendo único.',
  },
  hf_ms_judgment_sky_01: {
    id: 'hf_ms_judgment_sky_01',
    name: 'Juicio del Cielo',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 15,
    castTime: 1.5,
    cooldown: 0,
    range: 30,
    school: 'nature',
    requiresTarget: true,
    projectileFx: 'lightning',
    effects: [{ type: 'directDamage', min: 15, max: 17 }],
    ranks: [
      { rank: 2, level: 8, cost: 25, castTime: 2.0, effects: [{ type: 'directDamage', min: 26, max: 30 }] },
      { rank: 3, level: 14, cost: 40, castTime: 2.5, effects: [{ type: 'directDamage', min: 45, max: 51 }] },
      { rank: 4, level: 20, cost: 60, castTime: 3.0, effects: [{ type: 'directDamage', min: 75, max: 85 }] },
    ],
    description:
      'MUT PRIME Mage+Shaman. El primer rayo fija el blanco y una columna celeste cae desde arriba; ramas cercanas son VFX salvo autorización SIM futura.',
  },
  fingers_of_frost: {
    id: 'fingers_of_frost',""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_storms_end_01: {
    animationRoute: 'earthquake',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.75 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_storms_end_01',
    sfxRoute: 'earthquake',
  },""",
    """  hf_ms_storms_end_01: {
    animationRoute: 'earthquake',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.75 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_storms_end_01',
    sfxRoute: 'earthquake',
  },
  hf_ms_arc_bolt_01: {
    animationRoute: 'lightning_bolt',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_arc_bolt_01',
    sfxRoute: 'lightning_bolt',
  },
  hf_ms_overcharged_bolt_01: {
    animationRoute: 'lightning_bolt',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_overcharged_bolt_01',
    sfxRoute: 'lightning_bolt',
  },
  hf_ms_judgment_sky_01: {
    animationRoute: 'lightning_bolt',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_judgment_sky_01',
    sfxRoute: 'lightning_bolt',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_arc_bolt_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_ARC_BOLT_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_arc_bolt_01:{c:'#7ec8ff',p:'storm',pw:1.05,sp:24,vr:1,li:1.0,lg:1.8,a:'bolt'},
  hf_ms_overcharged_bolt_01:{c:'#62b8ff',p:'storm',pw:1.45,sp:38,vr:1,li:1.35,lg:2.3,a:'bolt'},
  hf_ms_judgment_sky_01:{c:'#b6e6ff',p:'storm',pw:1.85,sp:56,vr:1,li:1.8,lg:3.0,fin:1,a:'bolt'},
};

export const HF_MS_ARC_BOLT_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_arc_bolt_01:{
    archetype:'bolt',palette:'storm',power:1.05,bolt:{speed:30,headScale:0.8,coils:true,jagged:true,forkEvery:0,leader:true},
    linger:1.8,impact:{flipbook:false,ring:1.1,vRing:true,sparks:24,debris:false,smoke:false,light:1.0},
    rim:'#7ec8ff'
  },
  hf_ms_overcharged_bolt_01:{
    archetype:'bolt',palette:'storm',power:1.45,bolt:{speed:32,headScale:1.0,coils:true,jagged:true,forkEvery:0.12,leader:true},
    chargeStreams:2,linger:2.3,impact:{flipbook:true,ring:1.4,vRing:true,sparks:38,debris:false,smoke:false,light:1.35},
    rim:'#62b8ff',accent:'#d8f3ff'
  },
  hf_ms_judgment_sky_01:{
    archetype:'bolt',palette:'storm',power:1.85,bolt:{speed:34,headScale:1.15,coils:true,jagged:true,forkEvery:0.08,leader:true},
    chargeStreams:3,shaft:true,motifs:['pillars'],motifAt:'target',linger:3,
    impact:{flipbook:true,ring:1.8,vRing:true,sparks:56,debris:false,smoke:true,light:1.8},
    rim:'#b6e6ff',accent:'#ffffff',screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_FAULTWAKE_VFX_FULL_SPEC,
  HF_MS_FAULTWAKE_VFX_SPEC,
} from '../highfly/skill_lab2_ms_faultwake_vfx';""",
    """import {
  HF_MS_FAULTWAKE_VFX_FULL_SPEC,
  HF_MS_FAULTWAKE_VFX_SPEC,
} from '../highfly/skill_lab2_ms_faultwake_vfx';
import {
  HF_MS_ARC_BOLT_VFX_FULL_SPEC,
  HF_MS_ARC_BOLT_VFX_SPEC,
} from '../highfly/skill_lab2_ms_arc_bolt_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_FAULTWAKE_VFX_SPEC[abilityId]) return HF_MS_FAULTWAKE_VFX_SPEC[abilityId];""",
    """  if (HF_MS_FAULTWAKE_VFX_SPEC[abilityId]) return HF_MS_FAULTWAKE_VFX_SPEC[abilityId];
  if (HF_MS_ARC_BOLT_VFX_SPEC[abilityId]) return HF_MS_ARC_BOLT_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_FAULTWAKE_VFX_FULL_SPEC[abilityId]) return HF_MS_FAULTWAKE_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_FAULTWAKE_VFX_FULL_SPEC[abilityId]) return HF_MS_FAULTWAKE_VFX_FULL_SPEC[abilityId];
  if (HF_MS_ARC_BOLT_VFX_FULL_SPEC[abilityId]) return HF_MS_ARC_BOLT_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_arc_bolt_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='hf_ms_arc_bolt_01';
const EVO='hf_ms_overcharged_bolt_01';
const MUT='hf_ms_judgment_sky_01';
const SHAMAN='lightning_bolt';

function core(id:string) {
  const d=ABILITIES[id]!;
  return {
    cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,range:d.range,school:d.school,
    requiresTarget:d.requiresTarget,projectileFx:d.projectileFx,effects:d.effects,ranks:d.ranks,
  };
}

describe('HIGHFLY Mage Shaman 6/7 GOLD — Arc Bolt',()=>{
  it('ports the exact Shaman Arc Bolt authority and ranks onto Mage',()=>{
    expect(core(BASE)).toEqual(core(SHAMAN));
    expect(core(EVO)).toEqual(core(SHAMAN));
    expect(core(MUT)).toEqual(core(SHAMAN));
    expect([BASE,EVO,MUT].every((id)=>ABILITIES[id]!.class==='mage')).toBe(true);
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
  });

  it('keeps a single real damage packet while presentation may branch',()=>{
    expect(ABILITIES[MUT]!.effects).toEqual([{type:'directDamage',min:15,max:17}]);
    expect(ABILITIES[MUT]!.ranks).toEqual(ABILITIES[SHAMAN]!.ranks);
  });

  it('keeps Mage body with Lightning Bolt presentation route',()=>{
    expect(highflyPresentationRoute(BASE,'animation')).toBe('lightning_bolt');
    expect(highflyPresentationRoute(EVO,'animation')).toBe('lightning_bolt');
    expect(highflyPresentationRoute(MUT,'animation')).toBe('lightning_bolt');
    expect(abilityVfxFullSpec(MUT)).toMatchObject({archetype:'bolt',palette:'storm',screenFx:true,finisher:true});
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_ARC_BOLT_GOLD=1")
