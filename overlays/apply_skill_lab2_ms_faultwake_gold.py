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

# Mage + Shaman 5/7 GOLD — Faultwake heritage.
# Mage remains the visible class/resource authority. The copied Shaman BASE
# keeps the exact 6s ground zone, 1.5s ticks, radius and Mana cost. Thunder
# build/vent state is bridged separately as an internal aura only.
BASE="hf_ms_faultwake_01"
EVO="hf_ms_primordial_cataclysm_01"
MUT="hf_ms_storms_end_01"

insert_after("hf_ms_dragon_king_breath_01",[BASE,EVO,MUT])

rep(
    "src/sim/content/classes.ts",
    """  fingers_of_frost: {
    id: 'fingers_of_frost',""",
    """  hf_ms_faultwake_01: {
    id: 'hf_ms_faultwake_01',
    name: 'Falla',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 80,
    castTime: 0,
    cooldown: 12,
    range: 30,
    school: 'nature',
    requiresTarget: false,
    targetMode: 'position',
    effects: [{ type: 'groundAoE', min: 13, max: 17, radius: 8, duration: 6, interval: 1.5 }],
    description:
      'Heritage Shaman adaptada a Mage. Conserva Faultwake real con Mana; Thunder permanece como estado interno sin barra nueva.',
  },
  hf_ms_primordial_cataclysm_01: {
    id: 'hf_ms_primordial_cataclysm_01',
    name: 'Cataclismo Primordial',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 80,
    castTime: 0,
    cooldown: 12,
    range: 30,
    school: 'nature',
    requiresTarget: false,
    targetMode: 'position',
    effects: [{ type: 'groundAoE', min: 13, max: 17, radius: 8, duration: 6, interval: 1.5 }],
    description:
      'EVO de Falla. Conserva el mismo groundAoE y ticks; grietas, roca y polvo son presentación HIGHFLY.',
  },
  hf_ms_storms_end_01: {
    id: 'hf_ms_storms_end_01',
    name: 'Fin de la Tormenta',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 80,
    castTime: 0,
    cooldown: 12,
    range: 30,
    school: 'nature',
    requiresTarget: false,
    targetMode: 'position',
    effects: [{ type: 'groundAoE', min: 13, max: 17, radius: 8, duration: 6, interval: 1.5 }],
    description:
      'MUT PRIME Mage+Shaman. Grietas encadenadas mezclan electricidad y magma antes de la implosión visual final; el SIM mantiene los ticks originales.',
  },
  fingers_of_frost: {
    id: 'fingers_of_frost',""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_dragon_king_breath_01: {
    animationRoute: 'dragons_breath',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_dragon_king_breath_01',
    sfxRoute: 'dragons_breath',
  },""",
    """  hf_ms_dragon_king_breath_01: {
    animationRoute: 'dragons_breath',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_dragon_king_breath_01',
    sfxRoute: 'dragons_breath',
  },
  hf_ms_faultwake_01: {
    animationRoute: 'earthquake',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_faultwake_01',
    sfxRoute: 'earthquake',
  },
  hf_ms_primordial_cataclysm_01: {
    animationRoute: 'earthquake',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_primordial_cataclysm_01',
    sfxRoute: 'earthquake',
  },
  hf_ms_storms_end_01: {
    animationRoute: 'earthquake',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.75 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_storms_end_01',
    sfxRoute: 'earthquake',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_faultwake_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_FAULTWAKE_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_faultwake_01:{c:'#8f7a52',p:'nature',pw:1.15,sp:24,rg:2.1,db:1,sm:1,li:0.8,lg:6,a:'dot'},
  hf_ms_primordial_cataclysm_01:{c:'#b26c38',p:'nature',pw:1.55,sp:42,rg:2.6,db:1,sm:1,li:1.15,lg:6,a:'dot'},
  hf_ms_storms_end_01:{c:'#d95a32',p:'nature',pw:1.9,sp:60,rg:3.0,db:1,sm:1,li:1.55,lg:6,fin:1,a:'dot'},
};

export const HF_MS_FAULTWAKE_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_faultwake_01:{
    archetype:'dot',palette:'nature',power:1.15,motifs:['fissure'],motifAt:'target',decal:'crack',linger:6,
    impact:{flipbook:false,ring:2.1,vRing:false,sparks:24,debris:true,smoke:true,light:0.8},
    rim:'#8f7a52'
  },
  hf_ms_primordial_cataclysm_01:{
    archetype:'dot',palette:'nature',power:1.55,motifs:['fissure','pillars'],motifAt:'target',decal:'crack',linger:6,
    impact:{flipbook:true,ring:2.6,vRing:false,sparks:42,debris:true,smoke:true,light:1.15},
    rim:'#b26c38',accent:'#ffd1a0'
  },
  hf_ms_storms_end_01:{
    archetype:'dot',palette:'nature',power:1.9,motifs:['fissure','pillars'],motifAt:'target',decal:'crack',shaft:true,linger:6,
    chargeStreams:3,
    impact:{flipbook:true,ring:3.0,vRing:true,sparks:60,debris:true,smoke:true,light:1.55},
    rim:'#d95a32',accent:'#d8eeff',screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC,
  HF_MS_DRAGONS_BREATH_VFX_SPEC,
} from '../highfly/skill_lab2_ms_dragons_breath_vfx';""",
    """import {
  HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC,
  HF_MS_DRAGONS_BREATH_VFX_SPEC,
} from '../highfly/skill_lab2_ms_dragons_breath_vfx';
import {
  HF_MS_FAULTWAKE_VFX_FULL_SPEC,
  HF_MS_FAULTWAKE_VFX_SPEC,
} from '../highfly/skill_lab2_ms_faultwake_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_DRAGONS_BREATH_VFX_SPEC[abilityId]) return HF_MS_DRAGONS_BREATH_VFX_SPEC[abilityId];""",
    """  if (HF_MS_DRAGONS_BREATH_VFX_SPEC[abilityId]) return HF_MS_DRAGONS_BREATH_VFX_SPEC[abilityId];
  if (HF_MS_FAULTWAKE_VFX_SPEC[abilityId]) return HF_MS_FAULTWAKE_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC[abilityId]) return HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC[abilityId]) return HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC[abilityId];
  if (HF_MS_FAULTWAKE_VFX_FULL_SPEC[abilityId]) return HF_MS_FAULTWAKE_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_faultwake_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='hf_ms_faultwake_01';
const EVO='hf_ms_primordial_cataclysm_01';
const MUT='hf_ms_storms_end_01';
const SHAMAN='earthquake';

function core(id:string) {
  const d=ABILITIES[id]!;
  return {
    cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,range:d.range,school:d.school,
    requiresTarget:d.requiresTarget,targetMode:d.targetMode,effects:d.effects,
  };
}

describe('HIGHFLY Mage Shaman 5/7 GOLD — Faultwake',()=>{
  it('ports the exact Shaman Faultwake authority onto Mage Mana',()=>{
    expect(core(BASE)).toEqual(core(SHAMAN));
    expect(core(EVO)).toEqual(core(SHAMAN));
    expect(core(MUT)).toEqual(core(SHAMAN));
    expect([BASE,EVO,MUT].every((id)=>ABILITIES[id]!.class==='mage')).toBe(true);
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
  });

  it('preserves 6s / 1.5s / radius 8 ground ticks',()=>{
    expect(ABILITIES[MUT]!.effects).toEqual([{type:'groundAoE',min:13,max:17,radius:8,duration:6,interval:1.5}]);
  });

  it('keeps Mage body while routing Shaman presentation DNA',()=>{
    expect(highflyPresentationRoute(BASE,'animation')).toBe('earthquake');
    expect(highflyPresentationRoute(EVO,'animation')).toBe('earthquake');
    expect(highflyPresentationRoute(MUT,'animation')).toBe('earthquake');
    expect(abilityVfxFullSpec(MUT)).toMatchObject({archetype:'dot',palette:'nature',screenFx:true,finisher:true});
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_FAULTWAKE_GOLD=1")
