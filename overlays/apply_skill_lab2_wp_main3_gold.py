from pathlib import Path

ROOT=Path('.')

def read(path: str) -> str:
    return (ROOT/path).read_text(encoding='utf-8')

def write(path: str, text: str) -> None:
    p=ROOT/path
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(text,encoding='utf-8')

def rep(path: str, old: str, new: str) -> None:
    text=read(path)
    n=text.count(old)
    if n!=1:
        raise SystemExit(f'{path}: expected 1 anchor, found {n}: {old[:180]!r}')
    write(path,text.replace(old,new,1))

# Warrior body/weapon authority; Paladin DNA is presentation-only here.
rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_rw_point_no_return_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_point_no_return_01',
    sfxRoute: 'umbral_anchor',
  },""",
    """  hf_rw_point_no_return_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_point_no_return_01',
    sfxRoute: 'umbral_anchor',
  },
  hf_demolishing_leap_01: {
    animationRoute: 'heroic_leap',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_demolishing_leap_01',
    sfxRoute: 'heroic_leap',
  },
  hf_ascending_cataclysm_01: {
    animationRoute: 'heroic_leap',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ascending_cataclysm_01',
    sfxRoute: 'heroic_leap',
  },
  hf_cutting_whirlwind_01: {
    animationRoute: 'whirlwind',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_cutting_whirlwind_01',
    sfxRoute: 'whirlwind',
  },
  hf_colossus_tempest_01: {
    animationRoute: 'whirlwind',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_colossus_tempest_01',
    sfxRoute: 'whirlwind',
  },
  hf_seismic_fault_01: {
    animationRoute: 'faultline',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_seismic_fault_01',
    sfxRoute: 'faultline',
  },
  hf_world_fracture_01: {
    animationRoute: 'faultline',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_world_fracture_01',
    sfxRoute: 'faultline',
  },""",
)

write(
    "src/highfly/skill_lab2_wp_main3_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_WP_MAIN3_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_demolishing_leap_01: { c:'#b08b64', p:'steel', pw:1.35, sp:34, rg:1.2, sm:1, li:0.58, lg:1.0, wu:0.08, a:'strike' },
  hf_ascending_cataclysm_01: { c:'#ffd86a', p:'holy', pw:1.72, sp:46, rg:1.5, vr:1, sm:1, bl:1, li:1.18, lg:1.35, wu:0.12, fin:1, a:'strike' },
  hf_cutting_whirlwind_01: { c:'#c5d0dc', p:'steel', pw:1.28, sp:30, rg:1.1, sm:1, li:0.52, lg:0.95, wu:0.04, spin:1, a:'nova' },
  hf_colossus_tempest_01: { c:'#ffe58d', p:'holy', pw:1.6, sp:44, rg:1.35, vr:1, sm:1, bl:1, li:1.0, lg:1.3, wu:0.08, spin:1, fin:1, a:'nova' },
  hf_seismic_fault_01: { c:'#8d745e', p:'steel', pw:1.24, sp:28, rg:1.05, sm:1, li:0.44, lg:1.1, wu:0.07, a:'strike' },
  hf_world_fracture_01: { c:'#ffd66d', p:'holy', pw:1.55, sp:42, rg:1.3, vr:1, sm:1, bl:1, li:0.96, lg:1.4, wu:0.1, fin:1, a:'strike' },
};

export const HF_WP_MAIN3_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_demolishing_leap_01: {
    archetype:'strike', palette:'physical', power:1.35, windup:0.08, windupStyle:'weapon',
    motifs:['fissure'], motifAt:'caster', motifR:2.0,
    strike:{ swings:1, arc:'vertical', groundSlam:true },
    impact:{ ring:1.1, vRing:0.8, sparks:34, smoke:true, debris:true, light:0.58 },
    decal:'crack', linger:1.1, rim:'#cbb18d', tint:'#5b4634', accent:'#efe2cc',
  },
  hf_ascending_cataclysm_01: {
    archetype:'strike', palette:'holy', power:1.72, chargeStreams:2, windup:0.12, windupStyle:'ascend',
    motifs:['fissure','pillars'], motifAt:'caster', motifR:2.5,
    strike:{ swings:1, arc:'vertical', groundSlam:true },
    impact:{ ring:1.35, vRing:1.05, sparks:46, smoke:true, debris:true, light:1.18 },
    shaft:true, decal:'rune', linger:1.5, rim:'#fff0a8', tint:'#d59f2a', accent:'#ffffff', screenFx:true, finisher:true,
  },
  hf_cutting_whirlwind_01: {
    archetype:'nova', palette:'physical', power:1.28, windup:0.04, windupStyle:'stance',
    motifs:['bladestorm'], motifAt:'caster', motifR:2.0,
    nova:{ radius:8 },
    spin:{ rate:1.15 },
    impact:{ ring:1.0, vRing:false, sparks:30, smoke:true, trail:'sweep', light:0.52 },
    linger:0.95, rim:'#d8e0e8', accent:'#ffffff',
  },
  hf_colossus_tempest_01: {
    archetype:'nova', palette:'holy', power:1.6, chargeStreams:2, windup:0.08, windupStyle:'runes',
    motifs:['bladestorm','cross'], motifAt:'caster', motifR:2.35,
    nova:{ radius:8 },
    spin:{ rate:1.3 },
    impact:{ ring:1.25, vRing:0.85, sparks:44, smoke:true, trail:'sweep', light:1.0 },
    shaft:true, decal:'rune', linger:1.35, rim:'#fff2a8', tint:'#e6bb45', accent:'#ffffff', screenFx:true, finisher:true,
  },
  hf_seismic_fault_01: {
    archetype:'strike', palette:'physical', power:1.24, windup:0.07, windupStyle:'weapon',
    motifs:['fissure'], motifAt:'caster', motifR:2.2,
    strike:{ swings:1, arc:'vertical', groundSlam:true },
    impact:{ ring:false, vRing:0.7, sparks:28, smoke:true, debris:true, light:0.44 },
    decal:'crack', linger:1.15, rim:'#ad9278', tint:'#584535', accent:'#e2d2c2',
  },
  hf_world_fracture_01: {
    archetype:'strike', palette:'holy', power:1.55, chargeStreams:2, windup:0.1, windupStyle:'runes',
    motifs:['fissure','pillars'], motifAt:'caster', motifR:2.6,
    strike:{ swings:1, arc:'vertical', groundSlam:true },
    impact:{ ring:false, vRing:1.0, sparks:42, smoke:true, debris:true, light:0.96 },
    shaft:true, decal:'rune', linger:1.45, rim:'#ffe98f', tint:'#c99827', accent:'#ffffff', screenFx:true, finisher:true,
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_RW_R3_VFX_FULL_SPEC,
  HF_RW_R3_VFX_SPEC,
} from '../highfly/skill_lab2_rw_r3_vfx';""",
    """import {
  HF_RW_R3_VFX_FULL_SPEC,
  HF_RW_R3_VFX_SPEC,
} from '../highfly/skill_lab2_rw_r3_vfx';
import {
  HF_WP_MAIN3_VFX_FULL_SPEC,
  HF_WP_MAIN3_VFX_SPEC,
} from '../highfly/skill_lab2_wp_main3_vfx';""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_RW_R3_VFX_SPEC[abilityId]) return HF_RW_R3_VFX_SPEC[abilityId];""",
    """  if (HF_RW_R3_VFX_SPEC[abilityId]) return HF_RW_R3_VFX_SPEC[abilityId];
  if (HF_WP_MAIN3_VFX_SPEC[abilityId]) return HF_WP_MAIN3_VFX_SPEC[abilityId];""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_RW_R3_VFX_FULL_SPEC[abilityId]) return HF_RW_R3_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_RW_R3_VFX_FULL_SPEC[abilityId]) return HF_RW_R3_VFX_FULL_SPEC[abilityId];
  if (HF_WP_MAIN3_VFX_FULL_SPEC[abilityId]) return HF_WP_MAIN3_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_wp_main3_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';
import { ABILITIES } from '../src/sim/data';

describe('HIGHFLY Skill Lab 2.0 Warrior+Paladin MAIN3 GOLD presentation',()=>{
  it('routes all six endpoints through Warrior body animations and native SFX',()=>{
    for(const id of ['hf_demolishing_leap_01','hf_ascending_cataclysm_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('heroic_leap');
      expect(highflyPresentationRoute(id,'sfx')).toBe('heroic_leap');
    }
    for(const id of ['hf_cutting_whirlwind_01','hf_colossus_tempest_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('whirlwind');
      expect(highflyPresentationRoute(id,'sfx')).toBe('whirlwind');
    }
    for(const id of ['hf_seismic_fault_01','hf_world_fracture_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('faultline');
      expect(highflyPresentationRoute(id,'sfx')).toBe('faultline');
    }
  });

  it('pins GOLD mutation motifs without adding SIM effects',()=>{
    expect(abilityVfxFullSpec('hf_ascending_cataclysm_01')).toMatchObject({
      palette:'holy', finisher:true, screenFx:true, decal:'rune',
    });
    expect(abilityVfxFullSpec('hf_ascending_cataclysm_01')?.motifs).toEqual(
      expect.arrayContaining(['fissure','pillars']),
    );
    expect(abilityVfxFullSpec('hf_colossus_tempest_01')?.motifs).toEqual(
      expect.arrayContaining(['bladestorm','cross']),
    );
    expect(abilityVfxFullSpec('hf_world_fracture_01')?.motifs).toEqual(
      expect.arrayContaining(['fissure','pillars']),
    );

    expect(ABILITIES.hf_ascending_cataclysm_01!.effects).toEqual(
      ABILITIES.heroic_leap!.effects,
    );
    expect(ABILITIES.hf_colossus_tempest_01!.effects).toEqual(
      ABILITIES.whirlwind!.effects,
    );
    expect(ABILITIES.hf_world_fracture_01!.effects).toEqual(
      ABILITIES.hf_seismic_fault_01!.effects,
    );
  });
});
""",
)

print('HIGHFLY_SKILL_LAB2_WP_MAIN3_GOLD=1')
