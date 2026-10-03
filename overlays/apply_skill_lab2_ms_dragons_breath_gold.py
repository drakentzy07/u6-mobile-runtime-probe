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

# Mage + Shaman 4/7 GOLD — Dragon's Breath lineage.
# Preserve all four empower stages, 2.4s authoritative charge, cone geometry,
# incapacitate durations, maximum-stage guaranteed crit and Hot Streak hook.
EVO = "hf_ms_dragon_breath_01"
MUT = "hf_ms_dragon_king_breath_01"

insert_after("dragons_breath", [EVO, MUT])

rep(
    "src/sim/content/classes.ts",
    """  fingers_of_frost: {
    id: 'fingers_of_frost',""",
    """  hf_ms_dragon_breath_01: {
    id: 'hf_ms_dragon_breath_01',
    name: 'Aliento del Dragón',
    class: 'mage',
    learnLevel: 14,
    specs: ['fire'],
    hiddenFromPlayer: true,
    cost: 90,
    castTime: 2.4,
    empowerStages: 4,
    cooldown: 20,
    range: 0,
    school: 'fire',
    requiresTarget: false,
    projectile: false,
    effects: [
      {
        type: 'empoweredCone',
        angle: 90,
        fx: 'fireCone',
        guaranteedCritLevel: 4,
        hotStreakOnce: true,
        stages: [
          { range: 6, angle: 55, min: 32, max: 40, incapacitateDuration: 1 },
          { range: 8, angle: 65, min: 48, max: 60, incapacitateDuration: 1.5 },
          { range: 10, angle: 78, min: 68, max: 82, incapacitateDuration: 2 },
          { range: 12, angle: 90, min: 90, max: 110, incapacitateDuration: 3 },
        ],
      },
    ],
    description:
      'EVO de Aliento Dracónico. Conserva las cuatro etapas reales; el cono gana densidad y presión visual sin alterar alcance, daño, CC ni crítico del máximo.',
  },
  hf_ms_dragon_king_breath_01: {
    id: 'hf_ms_dragon_king_breath_01',
    name: 'Aliento del Rey Dragón',
    class: 'mage',
    learnLevel: 14,
    specs: ['fire'],
    hiddenFromPlayer: true,
    cost: 90,
    castTime: 2.4,
    empowerStages: 4,
    cooldown: 20,
    range: 0,
    school: 'fire',
    requiresTarget: false,
    projectile: false,
    effects: [
      {
        type: 'empoweredCone',
        angle: 90,
        fx: 'fireCone',
        guaranteedCritLevel: 4,
        hotStreakOnce: true,
        stages: [
          { range: 6, angle: 55, min: 32, max: 40, incapacitateDuration: 1 },
          { range: 8, angle: 65, min: 48, max: 60, incapacitateDuration: 1.5 },
          { range: 10, angle: 78, min: 68, max: 82, incapacitateDuration: 2 },
          { range: 12, angle: 90, min: 90, max: 110, incapacitateDuration: 3 },
        ],
      },
    ],
    description:
      'MUT PRIME Mage+Shaman. En carga máxima aparece una cabeza de dragón elemental como manifestación VFX; no es summon ni agrega impactos.',
  },
  fingers_of_frost: {
    id: 'fingers_of_frost',""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_thousand_celestial_darts_01: {
    animationRoute: 'arcane_missiles',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.33 },
      { event: 'impact', normalizedTime: 0.66 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_thousand_celestial_darts_01',
    sfxRoute: 'arcane_missiles',
  },""",
    """  hf_ms_thousand_celestial_darts_01: {
    animationRoute: 'arcane_missiles',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.33 },
      { event: 'impact', normalizedTime: 0.66 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_thousand_celestial_darts_01',
    sfxRoute: 'arcane_missiles',
  },
  hf_ms_dragon_breath_01: {
    animationRoute: 'dragons_breath',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_dragon_breath_01',
    sfxRoute: 'dragons_breath',
  },
  hf_ms_dragon_king_breath_01: {
    animationRoute: 'dragons_breath',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_dragon_king_breath_01',
    sfxRoute: 'dragons_breath',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_dragons_breath_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_DRAGONS_BREATH_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_dragon_breath_01:{c:'#ff7b32',p:'fire',pw:1.75,sp:54,rg:2.2,sm:1,li:1.45,lg:3.2,wu:2.4,a:'nova'},
  hf_ms_dragon_king_breath_01:{c:'#ef4a28',p:'fire',pw:2.1,sp:76,rg:2.8,sm:1,li:1.9,lg:4.0,wu:2.4,fin:1,a:'nova'},
};

export const HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_dragon_breath_01:{
    archetype:'nova',palette:'fire',power:1.75,windup:2.4,windupStyle:'ascend',
    chargeStreams:4,motifs:['pillars'],motifAt:'caster',linger:3.2,
    impact:{flipbook:true,ring:2.2,vRing:false,sparks:54,debris:false,smoke:true,light:1.45},
    rim:'#ff7b32',accent:'#ffd0a0'
  },
  hf_ms_dragon_king_breath_01:{
    archetype:'nova',palette:'fire',power:2.1,windup:2.4,windupStyle:'vortex',
    chargeStreams:5,motifs:['pillars','orbitals'],motifAt:'caster',shaft:true,linger:4,
    spirit:{model:'hawk',path:'lunge',at:'caster',scale:1.25,dur:2.0,tint:'#ff9a55',dim:0.22},
    impact:{flipbook:true,ring:2.8,vRing:true,sparks:76,debris:true,smoke:true,light:1.9},
    rim:'#ef4a28',accent:'#fff0c8',screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_AETHER_DARTS_VFX_FULL_SPEC,
  HF_MS_AETHER_DARTS_VFX_SPEC,
} from '../highfly/skill_lab2_ms_aether_darts_vfx';""",
    """import {
  HF_MS_AETHER_DARTS_VFX_FULL_SPEC,
  HF_MS_AETHER_DARTS_VFX_SPEC,
} from '../highfly/skill_lab2_ms_aether_darts_vfx';
import {
  HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC,
  HF_MS_DRAGONS_BREATH_VFX_SPEC,
} from '../highfly/skill_lab2_ms_dragons_breath_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_AETHER_DARTS_VFX_SPEC[abilityId]) return HF_MS_AETHER_DARTS_VFX_SPEC[abilityId];""",
    """  if (HF_MS_AETHER_DARTS_VFX_SPEC[abilityId]) return HF_MS_AETHER_DARTS_VFX_SPEC[abilityId];
  if (HF_MS_DRAGONS_BREATH_VFX_SPEC[abilityId]) return HF_MS_DRAGONS_BREATH_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_AETHER_DARTS_VFX_FULL_SPEC[abilityId]) return HF_MS_AETHER_DARTS_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_AETHER_DARTS_VFX_FULL_SPEC[abilityId]) return HF_MS_AETHER_DARTS_VFX_FULL_SPEC[abilityId];
  if (HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC[abilityId]) return HF_MS_DRAGONS_BREATH_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_dragons_breath_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='dragons_breath';
const EVO='hf_ms_dragon_breath_01';
const MUT='hf_ms_dragon_king_breath_01';

function authority(id:string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class,specs:d.specs,cost:d.cost,castTime:d.castTime,empowerStages:d.empowerStages,
    cooldown:d.cooldown,range:d.range,school:d.school,requiresTarget:d.requiresTarget,
    projectile:d.projectile,effects:d.effects,
  };
}

describe('HIGHFLY Mage Shaman 4/7 GOLD — Dragon Breath',()=>{
  it('preserves all four native empower stages in EVO/MUT',()=>{
    expect(authority(EVO)).toEqual(authority(BASE));
    expect(authority(MUT)).toEqual(authority(BASE));
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
    expect(ABILITIES[EVO]!.hiddenFromPlayer).toBe(true);
    expect(ABILITIES[MUT]!.hiddenFromPlayer).toBe(true);
  });

  it('pins maximum-stage crit/Hot Streak and CC contract',()=>{
    const eff=ABILITIES[MUT]!.effects[0] as any;
    expect(eff.type).toBe('empoweredCone');
    expect(eff.guaranteedCritLevel).toBe(4);
    expect(eff.hotStreakOnce).toBe(true);
    expect(eff.stages).toHaveLength(4);
    expect(eff.stages[3]).toEqual({range:12,angle:90,min:90,max:110,incapacitateDuration:3});
  });

  it('keeps Mage breath animation/SFX authority',()=>{
    expect(highflyPresentationRoute(EVO,'animation')).toBe('dragons_breath');
    expect(highflyPresentationRoute(MUT,'animation')).toBe('dragons_breath');
    expect(highflyPresentationRoute(EVO,'sfx')).toBe('dragons_breath');
    expect(highflyPresentationRoute(MUT,'sfx')).toBe('dragons_breath');
  });

  it('keeps dragon manifestation presentation-only',()=>{
    expect(abilityVfxFullSpec(EVO)).toMatchObject({archetype:'nova',palette:'fire'});
    expect(abilityVfxFullSpec(MUT)).toMatchObject({archetype:'nova',palette:'fire',screenFx:true,finisher:true});
    expect(ABILITIES[MUT]!.effects).toEqual(ABILITIES[BASE]!.effects);
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_DRAGONS_BREATH_GOLD=1")
