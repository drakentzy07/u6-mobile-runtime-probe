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

rep(
    "src/sim/content/classes.ts",
    """      'hf_rw_evil_eye_01',
      'hf_rw_reaping_command_01',
      'hf_rw_umbral_anchor_01',
      'instant_poison',""",
    """      'hf_rw_evil_eye_01',
      'hf_rw_abyss_gaze_01',
      'hf_rw_eye_of_end_01',
      'hf_rw_reaping_command_01',
      'hf_rw_unholy_dominion_01',
      'hf_rw_march_of_dead_01',
      'hf_rw_umbral_anchor_01',
      'hf_rw_umbral_return_01',
      'hf_rw_point_no_return_01',
      'instant_poison',""",
)

rep(
    "src/sim/content/classes.ts",
    """  backstab: {
    id: 'backstab',""",
    """  hf_rw_unholy_dominion_01: {
    id: 'hf_rw_unholy_dominion_01',
    name: 'Dominio Profano',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 45,
    castTime: 0,
    cooldown: 8,
    range: 30,
    school: 'shadow',
    requiresTarget: true,
    projectile: false,
    effects: [
      { type: 'reapingCommand' },
      { type: 'commandUndead', duration: 6, dmgPct: 0.15, hastePct: 0.1 },
    ],
    description:
      'Evolucion Heritage de Mandato de Siega. Reutiliza Dominio Profano: golpe sincronizado y exaltacion real de undead 6 sec con Energy Rogue.',
  },
  hf_rw_march_of_dead_01: {
    id: 'hf_rw_march_of_dead_01',
    name: 'Marcha de los Muertos',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 45,
    castTime: 0,
    cooldown: 8,
    range: 30,
    school: 'shadow',
    requiresTarget: true,
    projectile: false,
    effects: [
      { type: 'reapingCommand' },
      { type: 'commandUndead', duration: 6, dmgPct: 0.15, hastePct: 0.1 },
    ],
    description:
      'Mutacion Heritage de Dominio Profano. Conserva exactamente su autoridad SIM; las oleadas y la marcha final son coreografia premium.',
  },
  hf_rw_abyss_gaze_01: {
    id: 'hf_rw_abyss_gaze_01',
    name: 'Mirada del Abismo',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 15,
    castTime: 0,
    cooldown: 1,
    range: 30,
    school: 'shadow',
    requiresTarget: true,
    projectile: false,
    effects: [{ type: 'afflictionEvilEye' }],
    description:
      'Evolucion Heritage de Ojo Maldito. Conserva una unica primary mark source-owned y Energy Rogue.',
  },
  hf_rw_eye_of_end_01: {
    id: 'hf_rw_eye_of_end_01',
    name: 'Ojo del Fin',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 15,
    castTime: 0,
    cooldown: 1,
    range: 30,
    school: 'shadow',
    requiresTarget: true,
    projectile: false,
    effects: [{ type: 'afflictionEvilEye' }],
    description:
      'Mutacion Heritage. El iris y sus parpadeos son presentacion; la marca source-owned sigue siendo la unica autoridad.',
  },
  hf_rw_umbral_return_01: {
    id: 'hf_rw_umbral_return_01',
    name: 'Retorno Umbrio',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 25,
    castTime: 0,
    cooldown: 45,
    range: 0,
    school: 'shadow',
    requiresTarget: false,
    effects: [{ type: 'warlockUmbralAnchor', duration: 300, maxRange: 40 }],
    description:
      'Evolucion Heritage de Ancla Umbral. Mismo place/recall real y mismo gate de 40m con Energy Rogue.',
  },
  hf_rw_point_no_return_01: {
    id: 'hf_rw_point_no_return_01',
    name: 'Punto de No Retorno',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 25,
    castTime: 0,
    cooldown: 45,
    range: 0,
    school: 'shadow',
    requiresTarget: false,
    effects: [{ type: 'warlockUmbralAnchor', duration: 300, maxRange: 40 }],
    description:
      'Mutacion Heritage. La grieta y el doble afterimage son presentacion; el teleport conserva autoridad Claude.',
  },
  backstab: {
    id: 'backstab',""",
)

rep(
    "src/sim/combat/casting_lifecycle.ts",
    """  if (ability.id === UMBRAL_ANCHOR_ID || ability.id === 'hf_rw_umbral_anchor_01') {""",
    """  if (
    [UMBRAL_ANCHOR_ID, 'hf_rw_umbral_anchor_01', 'hf_rw_umbral_return_01', 'hf_rw_point_no_return_01'].includes(
      ability.id,
    )
  ) {""",
)

rep(
    "src/sim/combat/casting_lifecycle.ts",
    """  if (
    (abilityId === UMBRAL_ANCHOR_ID || abilityId === 'hf_rw_umbral_anchor_01') &&
    !hasUmbralAnchor(p)
  ) return;""",
    """  if (
    [UMBRAL_ANCHOR_ID, 'hf_rw_umbral_anchor_01', 'hf_rw_umbral_return_01', 'hf_rw_point_no_return_01'].includes(
      abilityId,
    ) &&
    !hasUmbralAnchor(p)
  ) return;""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_abyss_step_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_abyss_step_01',
    sfxRoute: 'shadowstep',
  },""",
    """  hf_abyss_step_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_abyss_step_01',
    sfxRoute: 'shadowstep',
  },
  hf_rw_reaping_command_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_reaping_command_01',
    sfxRoute: 'reaping_command',
  },
  hf_rw_unholy_dominion_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_unholy_dominion_01',
    sfxRoute: 'reaping_command',
  },
  hf_rw_march_of_dead_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.55 },
      { event: 'impact', normalizedTime: 0.78 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_rw_march_of_dead_01',
    sfxRoute: 'reaping_command',
  },
  hf_rw_evil_eye_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_evil_eye_01',
    sfxRoute: 'evil_eye',
  },
  hf_rw_abyss_gaze_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_abyss_gaze_01',
    sfxRoute: 'evil_eye',
  },
  hf_rw_eye_of_end_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_eye_of_end_01',
    sfxRoute: 'evil_eye',
  },
  hf_rw_umbral_anchor_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_umbral_anchor_01',
    sfxRoute: 'umbral_anchor',
  },
  hf_rw_umbral_return_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_umbral_return_01',
    sfxRoute: 'umbral_anchor',
  },
  hf_rw_point_no_return_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_rw_point_no_return_01',
    sfxRoute: 'umbral_anchor',
  },""",
)

write(
    "src/highfly/skill_lab2_rw_r3_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_RW_R3_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_rw_reaping_command_01: { c:'#5d377f', p:'shadow', pw:1.18, sp:20, rg:1.15, sm:1, li:0.62, lg:1.0, wu:0.08, a:'strike' },
  hf_rw_unholy_dominion_01: { c:'#7044a8', p:'shadow', pw:1.42, sp:30, rg:1.25, vr:1, sm:1, li:0.86, lg:1.15, wu:0.12, a:'strike' },
  hf_rw_march_of_dead_01: { c:'#8e5be0', p:'shadow', pw:1.78, sp:48, rg:1.4, vr:1, bl:1, sm:1, li:1.2, lg:1.35, wu:0.16, fin:1, a:'strike' },
  hf_rw_evil_eye_01: { c:'#7038a2', p:'shadow', pw:1.04, sp:8, rg:1.1, sm:1, li:0.48, lg:1.35, wu:0.04, a:'strike' },
  hf_rw_abyss_gaze_01: { c:'#8d4ac7', p:'shadow', pw:1.3, sp:16, rg:1.25, vr:1, sm:1, li:0.74, lg:1.6, wu:0.08, a:'strike' },
  hf_rw_eye_of_end_01: { c:'#ad6cff', p:'shadow', pw:1.58, sp:28, rg:1.4, vr:1, bl:1, sm:1, li:1.06, lg:1.9, wu:0.12, fin:1, a:'strike' },
  hf_rw_umbral_anchor_01: { c:'#4f3472', p:'shadow', pw:1.0, sp:8, sm:1, li:0.45, lg:1.5, wu:0.04, a:'dash' },
  hf_rw_umbral_return_01: { c:'#6e45a4', p:'shadow', pw:1.28, sp:14, sm:1, li:0.72, lg:1.75, wu:0.06, a:'dash' },
  hf_rw_point_no_return_01: { c:'#8e5de0', p:'shadow', pw:1.55, sp:24, sm:1, li:1.04, lg:2.0, wu:0.09, fin:1, a:'dash' },
};

export const HF_RW_R3_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_rw_reaping_command_01: {
    archetype:'strike', palette:'shadow', power:1.18, windup:0.08, windupStyle:'stance',
    motifs:['chains'], motifAt:'target', motifR:1.5,
    strike:{ swings:1, arc:'sweep' },
    impact:{ ring:false, vRing:false, sparks:20, smoke:true, light:0.62, focused:true },
    linger:0.9, rim:'#9b7ab8', accent:'#eee4ff',
  },
  hf_rw_unholy_dominion_01: {
    archetype:'strike', palette:'shadow', power:1.42, chargeStreams:2, windup:0.12, windupStyle:'vortex',
    motifs:['chains'], motifAt:'target', motifR:1.9,
    strike:{ swings:2, arc:'sweep' },
    impact:{ ring:false, vRing:0.8, sparks:30, smoke:true, light:0.86, focused:true },
    decal:'portal', linger:1.05, rim:'#b28bd7', tint:'#4b246b', accent:'#f4eaff',
  },
  hf_rw_march_of_dead_01: {
    archetype:'strike', palette:'shadow', power:1.78, chargeStreams:3, windup:0.16, windupStyle:'vortex',
    motifs:['chains','implosion'], motifAt:'target', motifR:2.4,
    strike:{ swings:3, arc:'sweep' },
    impact:{ ring:false, vRing:1.05, sparks:48, smoke:true, trail:'x', light:1.2, focused:true },
    decal:'portal', linger:1.35, rim:'#dcc6ff', tint:'#5a2c88', accent:'#ffffff', screenFx:true, finisher:true,
  },
  hf_rw_evil_eye_01: {
    archetype:'strike', palette:'shadow', power:1.04, windup:0.04, windupStyle:'stance',
    motifs:['implosion'], motifAt:'target', motifR:1.15,
    strike:{ swings:1, arc:'horizontal' },
    impact:{ ring:false, vRing:0.6, sparks:8, smoke:true, light:0.48, focused:true },
    linger:1.1, rim:'#a779c8', accent:'#eadcff',
  },
  hf_rw_abyss_gaze_01: {
    archetype:'strike', palette:'shadow', power:1.3, chargeStreams:2, windup:0.08, windupStyle:'vortex',
    motifs:['implosion'], motifAt:'target', motifR:1.6,
    strike:{ swings:1, arc:'horizontal' },
    impact:{ ring:false, vRing:0.85, sparks:16, smoke:true, light:0.74, focused:true },
    decal:'portal', linger:1.35, rim:'#c19be2', tint:'#512273', accent:'#f3e9ff',
  },
  hf_rw_eye_of_end_01: {
    archetype:'strike', palette:'shadow', power:1.58, chargeStreams:3, windup:0.12, windupStyle:'vortex',
    motifs:['chains','implosion'], motifAt:'target', motifR:2.0,
    strike:{ swings:1, arc:'horizontal' },
    impact:{ ring:false, vRing:1.0, sparks:28, smoke:true, light:1.06, focused:true },
    decal:'portal', linger:1.65, rim:'#e1c9ff', tint:'#632d8d', accent:'#ffffff', screenFx:true, finisher:true,
  },
  hf_rw_umbral_anchor_01: {
    archetype:'dash', palette:'shadow', power:1.0, spirit:null, windupStyle:'none',
    motifs:['fissure'], motifAt:'caster', motifR:1.0,
    linger:1.25, rim:'#7e6995',
    impact:{ ring:false, vRing:false, sparks:8, smoke:true, flipbook:false, debris:false, light:0.45, liteAudio:true },
  },
  hf_rw_umbral_return_01: {
    archetype:'dash', palette:'shadow', power:1.28, spirit:null, windup:0.06, windupStyle:'vortex',
    motifs:['fissure'], motifAt:'caster', motifR:1.35,
    decal:'portal', linger:1.5, rim:'#ae8ed0', tint:'#45205e', accent:'#e9dcff',
    impact:{ ring:false, vRing:false, sparks:14, smoke:true, flipbook:false, debris:false, light:0.72, liteAudio:true },
  },
  hf_rw_point_no_return_01: {
    archetype:'dash', palette:'shadow', power:1.55, spirit:null, windup:0.09, windupStyle:'vortex',
    motifs:['fissure','implosion'], motifAt:'caster', motifR:1.8,
    decal:'portal', linger:1.9, rim:'#d7baff', tint:'#55247a', accent:'#ffffff', screenFx:true, finisher:true,
    impact:{ ring:false, vRing:false, sparks:24, smoke:true, flipbook:false, debris:false, light:1.04, liteAudio:true },
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_ROGUE_R2_VFX_FULL_SPEC,
  HF_ROGUE_R2_VFX_SPEC,
} from '../highfly/skill_lab2_rogue_pack_r2_vfx';""",
    """import {
  HF_ROGUE_R2_VFX_FULL_SPEC,
  HF_ROGUE_R2_VFX_SPEC,
} from '../highfly/skill_lab2_rogue_pack_r2_vfx';
import {
  HF_RW_R3_VFX_FULL_SPEC,
  HF_RW_R3_VFX_SPEC,
} from '../highfly/skill_lab2_rw_r3_vfx';""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_ROGUE_R2_VFX_SPEC[abilityId]) return HF_ROGUE_R2_VFX_SPEC[abilityId];""",
    """  if (HF_ROGUE_R2_VFX_SPEC[abilityId]) return HF_ROGUE_R2_VFX_SPEC[abilityId];
  if (HF_RW_R3_VFX_SPEC[abilityId]) return HF_RW_R3_VFX_SPEC[abilityId];""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_ROGUE_R2_VFX_FULL_SPEC[abilityId]) return HF_ROGUE_R2_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_ROGUE_R2_VFX_FULL_SPEC[abilityId]) return HF_ROGUE_R2_VFX_FULL_SPEC[abilityId];
  if (HF_RW_R3_VFX_FULL_SPEC[abilityId]) return HF_RW_R3_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_rw_r3_gold.test.ts",
    """import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';
import { ABILITIES, CLASSES } from '../src/sim/data';

const LINES = [
  ['hf_rw_reaping_command_01','hf_rw_unholy_dominion_01','hf_rw_march_of_dead_01'],
  ['hf_rw_evil_eye_01','hf_rw_abyss_gaze_01','hf_rw_eye_of_end_01'],
  ['hf_rw_umbral_anchor_01','hf_rw_umbral_return_01','hf_rw_point_no_return_01'],
] as const;

function authority(id: string) {
  const d = ABILITIES[id]!;
  return {
    class:d.class, cost:d.cost, castTime:d.castTime, cooldown:d.cooldown,
    range:d.range, school:d.school, requiresTarget:d.requiresTarget,
    targetType:d.targetType, effects:d.effects,
  };
}

describe('HIGHFLY Skill Lab 2.0 Rogue+Warlock R3 GOLD', () => {
  it('keeps all Heritage endpoints on Rogue and preserves the intended SIM authority', () => {
    for (const [base,evo,mut] of LINES) {
      expect(CLASSES.rogue.abilities).toEqual(expect.arrayContaining([base,evo,mut]));
      expect(ABILITIES[evo]!.hiddenFromPlayer).toBe(true);
      expect(ABILITIES[mut]!.hiddenFromPlayer).toBe(true);
    }

    expect(ABILITIES.hf_rw_reaping_command_01!.effects).toEqual([{type:'reapingCommand'}]);
    expect(ABILITIES.hf_rw_unholy_dominion_01!.effects).toEqual([
      {type:'reapingCommand'},
      {type:'commandUndead',duration:6,dmgPct:0.15,hastePct:0.1},
    ]);
    expect(authority('hf_rw_march_of_dead_01')).toEqual(authority('hf_rw_unholy_dominion_01'));

    for (const [base,evo,mut] of LINES.slice(1)) {
      expect(authority(evo)).toEqual(authority(base));
      expect(authority(mut)).toEqual(authority(base));
    }
  });

  it('never imports a second visible resource into Heritage endpoints', () => {
    for (const [base,evo,mut] of LINES) {
      for (const id of [base,evo,mut]) {
        const def = ABILITIES[id]!;
        expect(def.class).toBe('rogue');
        expect(def.effects.some((e) => e.type === 'gainSoulFragments')).toBe(false);
        expect((def as { soulFragmentCost?: number }).soulFragmentCost).toBeUndefined();
      }
    }
  });

  it('routes body animation through Rogue while preserving Warlock flavored SFX', () => {
    for (const id of ['hf_rw_unholy_dominion_01','hf_rw_march_of_dead_01','hf_rw_abyss_gaze_01','hf_rw_eye_of_end_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('eviscerate');
    }
    for (const id of ['hf_rw_umbral_return_01','hf_rw_point_no_return_01']) {
      expect(highflyPresentationRoute(id,'animation')).toBe('shadowstep');
    }
    expect(highflyPresentationRoute('hf_rw_march_of_dead_01','sfx')).toBe('reaping_command');
    expect(highflyPresentationRoute('hf_rw_eye_of_end_01','sfx')).toBe('evil_eye');
    expect(highflyPresentationRoute('hf_rw_point_no_return_01','sfx')).toBe('umbral_anchor');
  });

  it('pins premium mutation VFX without making presentation authoritative', () => {
    expect(abilityVfxFullSpec('hf_rw_march_of_dead_01')).toMatchObject({
      archetype:'strike', palette:'shadow', finisher:true, screenFx:true,
    });
    expect(abilityVfxFullSpec('hf_rw_eye_of_end_01')?.motifs).toEqual(
      expect.arrayContaining(['implosion']),
    );
    expect(abilityVfxFullSpec('hf_rw_point_no_return_01')?.motifs).toEqual(
      expect.arrayContaining(['fissure','implosion']),
    );
  });

  it('extends both canonical Anchor lifecycle gates to BASE EVO MUT aliases', () => {
    const source = readFileSync('src/sim/combat/casting_lifecycle.ts','utf8');
    for (const id of ['hf_rw_umbral_anchor_01','hf_rw_umbral_return_01','hf_rw_point_no_return_01']) {
      expect(source).toContain(id);
    }
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_RW_R3_GOLD=1")
