from pathlib import Path

ROOT = Path(".")

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def write(path: str, text: str) -> None:
    p = ROOT / path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text, encoding="utf-8")

def rep(path: str, old: str, new: str) -> None:
    text = read(path)
    n = text.count(old)
    if n != 1:
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:180]!r}")
    write(path, text.replace(old, new, 1))

def insert_after(ability_id: str, additions: list[str]) -> None:
    text = read("src/sim/content/classes.ts")
    anchor = f"      '{ability_id}',"
    if text.count(anchor) != 1:
        raise SystemExit(f"expected one roster anchor for {ability_id}")
    block = anchor + "".join(f"\n      '{x}'," for x in additions)
    write("src/sim/content/classes.ts", text.replace(anchor, block, 1))

# ---------------------------------------------------------------------------
# Mage + Shaman 2/7 GOLD — Meteor / Skystone lineage
# BASE Claude: meteor/Skystone.
# EVO/MUT preserve the delayed ground AoE, Ignite fraction, Mana, cooldown,
# radius and ground targeting exactly. Extra fragments, magma cracks and the
# second elemental wave are presentation-only in this safe integration pass.
# ---------------------------------------------------------------------------

EVO = "hf_ms_fallen_star_01"
MUT = "hf_ms_celestial_extinction_01"

insert_after("meteor", [EVO, MUT])

rep(
    "src/sim/content/classes.ts",
    """  presence_of_mind: {
    id: 'presence_of_mind',""",
    """  hf_ms_fallen_star_01: {
    id: 'hf_ms_fallen_star_01',
    name: 'Estrella Caída',
    class: 'mage',
    learnLevel: 16,
    specs: ['fire'],
    hiddenFromPlayer: true,
    cost: 120,
    castTime: 0,
    cooldown: 45,
    range: 30,
    school: 'fire',
    requiresTarget: false,
    targetMode: 'position',
    effects: [
      {
        type: 'groundAoE',
        min: 90,
        max: 120,
        radius: 8,
        duration: 2.5,
        interval: 2,
        igniteFrac: 0.4,
        delayed: true,
      },
    ],
    description:
      'EVO de Skystone. Conserva exactamente la caída diferida, el área, el impacto y el Ignite de Meteor; telegraph, sombra, cráter y fragmentos son presentación HIGHFLY.',
  },
  hf_ms_celestial_extinction_01: {
    id: 'hf_ms_celestial_extinction_01',
    name: 'Extinción Celeste',
    class: 'mage',
    learnLevel: 16,
    specs: ['fire'],
    hiddenFromPlayer: true,
    cost: 120,
    castTime: 0,
    cooldown: 45,
    range: 30,
    school: 'fire',
    requiresTarget: false,
    targetMode: 'position',
    effects: [
      {
        type: 'groundAoE',
        min: 90,
        max: 120,
        radius: 8,
        duration: 2.5,
        interval: 2,
        igniteFrac: 0.4,
        delayed: true,
      },
    ],
    description:
      'MUT PRIME Mage+Shaman. El meteorito despierta magma y tormenta en el cráter; la onda elemental secundaria es coreografía visual hasta que exista un rider SIM balanceado explícitamente.',
  },
  presence_of_mind: {
    id: 'presence_of_mind',""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_crimson_rain_01: {
    animationRoute: 'pyroblast',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.58 },
      { event: 'impact', normalizedTime: 0.79 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_crimson_rain_01',
    sfxRoute: 'pyroblast',
  },""",
    """  hf_ms_crimson_rain_01: {
    animationRoute: 'pyroblast',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.58 },
      { event: 'impact', normalizedTime: 0.79 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_crimson_rain_01',
    sfxRoute: 'pyroblast',
  },
  hf_ms_fallen_star_01: {
    animationRoute: 'meteor',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_fallen_star_01',
    sfxRoute: 'meteor',
  },
  hf_ms_celestial_extinction_01: {
    animationRoute: 'meteor',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.72 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_celestial_extinction_01',
    sfxRoute: 'meteor',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_meteor_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_METEOR_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_fallen_star_01:{c:'#ff632b',p:'fire',pw:1.85,sp:72,rg:2.8,db:1,sm:1,li:1.8,lg:4.2,wu:2.2,fin:1,a:'burst'},
  hf_ms_celestial_extinction_01:{c:'#e23a24',p:'fire',pw:2.15,sp:88,rg:3.2,db:1,sm:1,li:2.1,lg:5.0,wu:2.2,fin:1,a:'burst'},
};

export const HF_MS_METEOR_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_fallen_star_01:{
    archetype:'burst',palette:'fire',power:1.85,burst:{style:'skybeam'},shaft:1.8,
    windup:2.2,windupStyle:'orb',motifs:['fissure'],motifAt:'target',decal:'scorch',
    hot:0.28,linger:4.2,rim:'#ff632b',accent:'#ffd0a0',
    impact:{flipbook:true,ring:2.8,vRing:true,sparks:72,debris:true,smoke:true,light:1.8,sample:'imp_meteor'},
    finisher:true
  },
  hf_ms_celestial_extinction_01:{
    archetype:'burst',palette:'fire',power:2.15,burst:{style:'skybeam'},shaft:2.15,
    windup:2.2,windupStyle:'orb',motifs:['fissure','pillars'],motifAt:'target',decal:'crack',
    hot:0.42,linger:5,rim:'#e23a24',accent:'#ffe0b5',
    chargeStreams:3,
    impact:{flipbook:true,ring:3.2,vRing:true,sparks:88,debris:true,smoke:true,light:2.1,sample:'imp_meteor'},
    screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_PYRELANCE_VFX_FULL_SPEC,
  HF_MS_PYRELANCE_VFX_SPEC,
} from '../highfly/skill_lab2_ms_pyrelance_vfx';""",
    """import {
  HF_MS_PYRELANCE_VFX_FULL_SPEC,
  HF_MS_PYRELANCE_VFX_SPEC,
} from '../highfly/skill_lab2_ms_pyrelance_vfx';
import {
  HF_MS_METEOR_VFX_FULL_SPEC,
  HF_MS_METEOR_VFX_SPEC,
} from '../highfly/skill_lab2_ms_meteor_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_PYRELANCE_VFX_SPEC[abilityId]) return HF_MS_PYRELANCE_VFX_SPEC[abilityId];""",
    """  if (HF_MS_PYRELANCE_VFX_SPEC[abilityId]) return HF_MS_PYRELANCE_VFX_SPEC[abilityId];
  if (HF_MS_METEOR_VFX_SPEC[abilityId]) return HF_MS_METEOR_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_PYRELANCE_VFX_FULL_SPEC[abilityId]) return HF_MS_PYRELANCE_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_PYRELANCE_VFX_FULL_SPEC[abilityId]) return HF_MS_PYRELANCE_VFX_FULL_SPEC[abilityId];
  if (HF_MS_METEOR_VFX_FULL_SPEC[abilityId]) return HF_MS_METEOR_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_meteor_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='meteor';
const EVO='hf_ms_fallen_star_01';
const MUT='hf_ms_celestial_extinction_01';

function authority(id:string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class,specs:d.specs,cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,
    range:d.range,school:d.school,requiresTarget:d.requiresTarget,targetMode:d.targetMode,
    effects:d.effects,
  };
}

describe('HIGHFLY Mage Shaman 2/7 GOLD — Meteor',()=>{
  it('preserves Skystone SIM authority in EVO/MUT',()=>{
    expect(authority(EVO)).toEqual(authority(BASE));
    expect(authority(MUT)).toEqual(authority(BASE));
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
    expect(ABILITIES[EVO]!.hiddenFromPlayer).toBe(true);
    expect(ABILITIES[MUT]!.hiddenFromPlayer).toBe(true);
  });

  it('keeps native delayed ground AoE + Ignite authority',()=>{
    expect(ABILITIES[EVO]!.effects).toEqual([
      {type:'groundAoE',min:90,max:120,radius:8,duration:2.5,interval:2,igniteFrac:0.4,delayed:true},
    ]);
    expect(ABILITIES[MUT]!.effects).toEqual(ABILITIES[BASE]!.effects);
  });

  it('keeps Mage meteor animation/SFX route',()=>{
    expect(highflyPresentationRoute(EVO,'animation')).toBe('meteor');
    expect(highflyPresentationRoute(MUT,'animation')).toBe('meteor');
    expect(highflyPresentationRoute(EVO,'sfx')).toBe('meteor');
    expect(highflyPresentationRoute(MUT,'sfx')).toBe('meteor');
  });

  it('adds premium crater/storm presentation without extra SIM pulses',()=>{
    expect(abilityVfxFullSpec(EVO)).toMatchObject({archetype:'burst',palette:'fire',finisher:true});
    expect(abilityVfxFullSpec(MUT)).toMatchObject({archetype:'burst',palette:'fire',screenFx:true,finisher:true});
    expect(ABILITIES[MUT]!.effects).toEqual(ABILITIES[BASE]!.effects);
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_METEOR_GOLD=1")
