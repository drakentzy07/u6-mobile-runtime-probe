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

# ---------------------------------------------------------------------------
# HIGHFLY Skill Lab 2.0 — Rogue PACK R2
#
#  1) Remate -> Remate Cruel -> Ultimo Susurro
#  2) Desvanecer -> Desvanecer Sombrio -> Vacio Absoluto
#  3) Paso Sombrio -> Paso Umbrio -> Paso del Abismo
#
# The BASE mechanics remain Claude-owned. EVO/MUT are explicit lab endpoints
# whose sim identity is mapped back to the original engine semantics where
# Claude keys behavior on the base ability id.
# ---------------------------------------------------------------------------

# Add the six R2 endpoints to the Rogue kit, immediately after their BASE rows.
rep(
    "src/sim/content/classes.ts",
    """      'sinister_strike',
      'eviscerate',
      'garrote',""",
    """      'sinister_strike',
      'eviscerate',
      'hf_cruel_finish_01',
      'hf_last_whisper_01',
      'garrote',""",
)

rep(
    "src/sim/content/classes.ts",
    """      'rupture',
      'vanish',
      'instant_poison',""",
    """      'rupture',
      'vanish',
      'hf_shadow_vanish_01',
      'hf_absolute_void_01',
      'instant_poison',""",
)

# Shadowstep is talent-granted in Claude, so HIGHFLY lab endpoints are added
# to the class roster without changing the original talent row.
rep(
    "src/sim/content/classes.ts",
    """      'venom_dart',
    ],""",
    """      'venom_dart',
      'hf_umbral_step_01',
      'hf_abyss_step_01',
    ],""",
)

# Six ability definitions. Remate copies the complete combo/rank/action-
# replacement contract. Vanish copies combat-stealth/off-GCD semantics.
# Shadowstep copies any-target directed blink semantics.
rep(
    "src/sim/content/classes.ts",
    """  backstab: {
    id: 'backstab',""",
    """  hf_cruel_finish_01: {
    id: 'hf_cruel_finish_01',
    name: 'Remate Cruel',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 35,
    castTime: 0,
    cooldown: 0,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    spendsCombo: true,
    actionReplacement: [
      { abilityId: 'venomrend', auraKind: 'venom_ritual', minStacks: 6 },
      { abilityId: 'knockout_blow', auraKind: 'redline', minStacks: 1 },
    ],
    effects: [{ type: 'finisherDamage', base: 4, perCombo: 7, variance: 4 }],
    ranks: [
      { rank: 2, level: 12, cost: 35, effects: [{ type: 'finisherDamage', base: 8, perCombo: 12, variance: 6 }] },
      { rank: 3, level: 18, cost: 35, effects: [{ type: 'finisherDamage', base: 14, perCombo: 18, variance: 9 }] },
    ],
    description:
      'Evolucion de Remate. Conserva el mismo finisher por combo points y la misma autoridad de dano; profundiza la lectura visual de ejecucion.',
  },
  hf_last_whisper_01: {
    id: 'hf_last_whisper_01',
    name: 'Ultimo Susurro',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 1,
    cost: 35,
    castTime: 0,
    cooldown: 0,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    spendsCombo: true,
    actionReplacement: [
      { abilityId: 'venomrend', auraKind: 'venom_ritual', minStacks: 6 },
      { abilityId: 'knockout_blow', auraKind: 'redline', minStacks: 1 },
    ],
    effects: [{ type: 'finisherDamage', base: 4, perCombo: 7, variance: 4 }],
    ranks: [
      { rank: 2, level: 12, cost: 35, effects: [{ type: 'finisherDamage', base: 8, perCombo: 12, variance: 6 }] },
      { rank: 3, level: 18, cost: 35, effects: [{ type: 'finisherDamage', base: 14, perCombo: 18, variance: 9 }] },
    ],
    description:
      'Mutacion de Remate Cruel. El Rogue ejecuta el mismo finisher autoritativo mientras una sombra Warlock retiene visualmente al objetivo y el ultimo scar absorbe la escena.',
  },
  backstab: {
    id: 'backstab',""",
)

rep(
    "src/sim/content/classes.ts",
    """  instant_poison: {
    id: 'instant_poison',""",
    """  hf_shadow_vanish_01: {
    id: 'hf_shadow_vanish_01',
    tooltipOmitEffectLines: true,
    name: 'Desvanecer Sombrio',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 0,
    castTime: 0,
    cooldown: 300,
    range: 0,
    school: 'physical',
    requiresTarget: false,
    offGcd: true,
    effects: [{ type: 'selfBuff', kind: 'stealth', value: 0.5, duration: 10 }],
    description:
      'Evolucion de Desvanecer. Mantiene Duskveil de combate y el escape real de targeting con una entrada de humo y afterimage mas limpia.',
  },
  hf_absolute_void_01: {
    id: 'hf_absolute_void_01',
    tooltipOmitEffectLines: true,
    name: 'Vacio Absoluto',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 18,
    cost: 0,
    castTime: 0,
    cooldown: 300,
    range: 0,
    school: 'physical',
    requiresTarget: false,
    offGcd: true,
    effects: [{ type: 'selfBuff', kind: 'stealth', value: 0.5, duration: 10 }],
    description:
      'Mutacion de Desvanecer Sombrio. Conserva el mismo escape Duskveil y deja un colapso umbral puramente visual en el punto abandonado.',
  },
  instant_poison: {
    id: 'instant_poison',""",
)

rep(
    "src/sim/content/classes.ts",
    """  stealth: {
    id: 'stealth',""",
    """  hf_umbral_step_01: {
    id: 'hf_umbral_step_01',
    name: 'Paso Umbrio',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 20,
    castTime: 0,
    cooldown: 24,
    range: 24,
    school: 'physical',
    requiresTarget: true,
    targetType: 'any',
    effects: [{ type: 'blinkForward', distance: 24 }],
    description:
      'Evolucion de Paso Sombrio. Conserva el teleport dirigido a aliado o enemigo sin romper Duskveil y refuerza la lectura de salida y entrada.',
  },
  hf_abyss_step_01: {
    id: 'hf_abyss_step_01',
    name: 'Paso del Abismo',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 10,
    cost: 20,
    castTime: 0,
    cooldown: 24,
    range: 24,
    school: 'physical',
    requiresTarget: true,
    targetType: 'any',
    effects: [{ type: 'blinkForward', distance: 24 }],
    description:
      'Mutacion de Paso Umbrio. Conserva el blink autoritativo y conecta origen/destino con una fisura umbral de presentacion.',
  },
  stealth: {
    id: 'stealth',""",
)

# ---------------------------------------------------------------------------
# Engine equivalence: only where Claude intentionally keys mechanics on the
# original id. This keeps the custom endpoints from silently losing core Rogue
# behavior while leaving BASE untouched.
# ---------------------------------------------------------------------------

# Dirt Nap engine: Redline opens for EVO/MUT exactly as for BASE.
rep(
    "src/sim/combat/rogue_engines.ts",
    """  if (spentCombo < 4) return;
  if (abilityId !== 'eviscerate') return;""",
    """  if (spentCombo < 4) return;
  if (!['eviscerate', 'hf_cruel_finish_01', 'hf_last_whisper_01'].includes(abilityId)) return;""",
)

# Smokefade special set-bonus opener recognition.
rep(
    "src/sim/combat/rogue_engines.ts",
    """    const fromSmokefade = p.auras.some((aura) => aura.id === 'vanish' && aura.kind === 'stealth');""",
    """    const fromSmokefade = p.auras.some(
      (aura) =>
        ['vanish', 'hf_shadow_vanish_01', 'hf_absolute_void_01'].includes(aura.id) &&
        aura.kind === 'stealth',
    );""",
)

# Escape-stealth targeting semantics.
rep(
    "src/sim/threat.ts",
    """export function hasEscapeStealth(target: Entity): boolean {
  return target.auras.some((a) => a.id === 'vanish' && a.kind === 'stealth');
}""",
    """export function hasEscapeStealth(target: Entity): boolean {
  return target.auras.some(
    (a) =>
      ['vanish', 'hf_shadow_vanish_01', 'hf_absolute_void_01'].includes(a.id) &&
      a.kind === 'stealth',
  );
}""",
)

# Combat/threat drop and directed target-relative blink.
rep(
    "src/sim/combat/effect_dispatch.ts",
    """function dropsCombatOnStealth(ability: AbilityDef): boolean {
  return ability.id === 'vanish';
}""",
    """function dropsCombatOnStealth(ability: AbilityDef): boolean {
  return ['vanish', 'hf_shadow_vanish_01', 'hf_absolute_void_01'].includes(ability.id);
}""",
)

rep(
    "src/sim/combat/effect_dispatch.ts",
    """        if (ability.id === 'shadowstep' && target && !target.dead) {""",
    """        if (
          ['shadowstep', 'hf_umbral_step_01', 'hf_abyss_step_01'].includes(ability.id) &&
          target &&
          !target.dead
        ) {""",
)

# Kill Chain refreshes the active HIGHFLY Smokefade endpoint as well as BASE.
rep(
    "src/sim/combat/damage.ts",
    """        if (killMods.onKillVanishReset > 0) creditEntity.cooldowns.delete('vanish');""",
    """        if (killMods.onKillVanishReset > 0) {
          creditEntity.cooldowns.delete('vanish');
          creditEntity.cooldowns.delete('hf_shadow_vanish_01');
          creditEntity.cooldowns.delete('hf_absolute_void_01');
        }""",
)

# ---------------------------------------------------------------------------
# Presentation routes. Body/weapon/SFX remain Rogue BASE routes.
# ---------------------------------------------------------------------------
rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_eclipse_mortal_01: {
    animationRoute: 'ambush',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.55 },
      { event: 'impact', normalizedTime: 0.78 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_eclipse_mortal_01',
    sfxRoute: 'ambush',
  },""",
    """  hf_eclipse_mortal_01: {
    animationRoute: 'ambush',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.55 },
      { event: 'impact', normalizedTime: 0.78 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_eclipse_mortal_01',
    sfxRoute: 'ambush',
  },
  hf_cruel_finish_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_cruel_finish_01',
    sfxRoute: 'eviscerate',
  },
  hf_last_whisper_01: {
    animationRoute: 'eviscerate',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.72 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_last_whisper_01',
    sfxRoute: 'eviscerate',
  },
  hf_shadow_vanish_01: {
    animationRoute: 'vanish',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_shadow_vanish_01',
    sfxRoute: 'vanish',
  },
  hf_absolute_void_01: {
    animationRoute: 'vanish',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_absolute_void_01',
    sfxRoute: 'vanish',
  },
  hf_umbral_step_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_umbral_step_01',
    sfxRoute: 'shadowstep',
  },
  hf_abyss_step_01: {
    animationRoute: 'shadowstep',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_abyss_step_01',
    sfxRoute: 'shadowstep',
  },""",
)

write(
    "src/highfly/skill_lab2_rogue_pack_r2_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_ROGUE_R2_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_cruel_finish_01: { c:'#b62947', p:'blood', pw:1.58, sp:42, rg:1.1, vr:1, bl:1, sm:0, li:0.62, lg:1.0, wu:0.16, fin:1, a:'strike' },
  hf_last_whisper_01: { c:'#6d3bd1', p:'shadow', pw:1.82, sp:52, rg:1.2, vr:1, bl:1, sm:1, li:1.12, lg:1.2, wu:0.18, fin:1, a:'strike' },
  hf_shadow_vanish_01: { c:'#75628f', p:'shadow', pw:1.08, sp:0, rg:1.1, vr:0, bl:0, sm:1, li:0.34, lg:1.8, wu:0, a:'buff' },
  hf_absolute_void_01: { c:'#54219f', p:'shadow', pw:1.36, sp:4, rg:1.2, vr:0, bl:0, sm:1, li:0.72, lg:2.0, wu:0.08, a:'buff' },
  hf_umbral_step_01: { c:'#755bb1', p:'shadow', pw:1.05, sp:12, rg:0, vr:0, bl:0, sm:1, li:0.82, lg:0.9, wu:0, a:'dash' },
  hf_abyss_step_01: { c:'#5a24bb', p:'shadow', pw:1.34, sp:18, rg:0, vr:0, bl:0, sm:1, li:1.08, lg:1.1, wu:0.06, a:'dash' },
};

export const HF_ROGUE_R2_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_cruel_finish_01: {
    archetype:'strike', palette:'blood', power:1.58, finisher:true,
    windup:0.16, windupStyle:'stance',
    strike:{ swings:3, arc:'uppercut', bleed:true },
    impact:{ debris:true, sparks:42, trail:'x', blood:true, light:0.62, focused:true },
    linger:0.9, rim:'#e58ba0', accent:'#fff0f3',
  },
  hf_last_whisper_01: {
    archetype:'strike', palette:'shadow', power:1.82, finisher:true,
    windup:0.18, windupStyle:'vortex',
    motifs:['chains','implosion'], motifAt:'target', motifR:2.0,
    strike:{ swings:3, arc:'sweep', bleed:true },
    impact:{ ring:false, vRing:1.05, sparks:52, smoke:true, trail:'x', light:1.12, focused:true },
    decal:'portal', linger:1.15, rim:'#d9cbff', tint:'#4f238f', accent:'#f8f2ff', screenFx:true,
  },
  hf_shadow_vanish_01: {
    archetype:'buff', palette:'shadow', power:1.08, self:true,
    buff:{ style:'veil', shellDur:0.55, orbit:'none', o:{ tickEvery:4.2 } },
    motifs:['implosion'], windupStyle:'none', linger:2.0, rim:'#463958',
    impact:{ flipbook:false, ring:1.15, vRing:false, sparks:0, debris:false, smoke:true, light:0.34 },
  },
  hf_absolute_void_01: {
    archetype:'buff', palette:'shadow', power:1.36, self:true,
    chargeStreams:2, windup:0.08, windupStyle:'vortex',
    buff:{ style:'veil', shellDur:0.7, orbit:'none', o:{ tickEvery:4.0 } },
    motifs:['implosion'], motifAt:'caster', motifR:1.8,
    decal:'portal', linger:2.2, rim:'#a680df', tint:'#391168', accent:'#decaff', screenFx:true,
    impact:{ flipbook:false, ring:false, vRing:false, sparks:4, debris:false, smoke:true, light:0.72 },
  },
  hf_umbral_step_01: {
    archetype:'dash', palette:'shadow', power:1.05, spirit:null, windupStyle:'none',
    linger:0.95, rim:'#9278c4',
    impact:{ smoke:true, sparks:12, ring:false, vRing:false, flipbook:false, debris:false, light:0.82, liteAudio:true },
  },
  hf_abyss_step_01: {
    archetype:'dash', palette:'shadow', power:1.34, spirit:null,
    windup:0.06, windupStyle:'vortex',
    motifs:['fissure','implosion'], motifAt:'target', motifR:1.7,
    decal:'portal', linger:1.15, rim:'#c1a4f2', tint:'#431682', accent:'#eee4ff',
    impact:{ smoke:true, sparks:18, ring:false, vRing:false, flipbook:false, debris:false, light:1.08, liteAudio:true },
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_ECLIPSE_MORTAL_VFX_FULL_SPEC,
  HF_ECLIPSE_MORTAL_VFX_SPEC,
} from '../highfly/skill_lab2_rogue_mutation_vfx';""",
    """import {
  HF_ECLIPSE_MORTAL_VFX_FULL_SPEC,
  HF_ECLIPSE_MORTAL_VFX_SPEC,
} from '../highfly/skill_lab2_rogue_mutation_vfx';
import {
  HF_ROGUE_R2_VFX_FULL_SPEC,
  HF_ROGUE_R2_VFX_SPEC,
} from '../highfly/skill_lab2_rogue_pack_r2_vfx';""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (abilityId === 'hf_eclipse_mortal_01') return HF_ECLIPSE_MORTAL_VFX_SPEC;""",
    """  if (abilityId === 'hf_eclipse_mortal_01') return HF_ECLIPSE_MORTAL_VFX_SPEC;
  if (HF_ROGUE_R2_VFX_SPEC[abilityId]) return HF_ROGUE_R2_VFX_SPEC[abilityId];""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (abilityId === 'hf_eclipse_mortal_01') return HF_ECLIPSE_MORTAL_VFX_FULL_SPEC;""",
    """  if (abilityId === 'hf_eclipse_mortal_01') return HF_ECLIPSE_MORTAL_VFX_FULL_SPEC;
  if (HF_ROGUE_R2_VFX_FULL_SPEC[abilityId]) return HF_ROGUE_R2_VFX_FULL_SPEC[abilityId];""",
)

# ---------------------------------------------------------------------------
# Tests: individual lineage contracts + pack integration.
# ---------------------------------------------------------------------------
write(
    "tests/highfly_skill_lab2_rogue_pack_r2.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

describe('HIGHFLY Skill Lab 2.0 Rogue PACK R2', () => {
  it('R2A Remate preserves one combo spend and one finisher damage authority', () => {
    for (const id of ['eviscerate','hf_cruel_finish_01','hf_last_whisper_01']) {
      const def = ABILITIES[id];
      expect(def.requiresTarget).toBe(true);
      expect(def.spendsCombo).toBe(true);
      expect(def.effects.filter((e) => e.type === 'finisherDamage')).toHaveLength(1);
    }
    expect(highflyPresentationRoute('hf_cruel_finish_01','animation')).toBe('eviscerate');
    expect(highflyPresentationRoute('hf_last_whisper_01','animation')).toBe('eviscerate');
    expect(abilityVfxFullSpec('hf_last_whisper_01')?.motifs).toEqual(
      expect.arrayContaining(['implosion']),
    );
  });

  it('R2B Desvanecer preserves combat stealth/off-GCD with no damage effect', () => {
    for (const id of ['vanish','hf_shadow_vanish_01','hf_absolute_void_01']) {
      const def = ABILITIES[id];
      expect(def.offGcd).toBe(true);
      expect(def.requiresTarget).toBe(false);
      expect(def.effects).toHaveLength(1);
      expect(def.effects[0]).toMatchObject({ type:'selfBuff', kind:'stealth', duration:10 });
      expect(def.effects.some((e) => ['directDamage','weaponStrike','finisherDamage'].includes(e.type))).toBe(false);
    }
    expect(abilityVfxFullSpec('hf_absolute_void_01')?.decal).toBe('portal');
  });

  it('R2C Paso Sombrio preserves any-target 24m blink and zero damage authority', () => {
    for (const id of ['shadowstep','hf_umbral_step_01','hf_abyss_step_01']) {
      const def = ABILITIES[id];
      expect(def.requiresTarget).toBe(true);
      expect(def.targetType).toBe('any');
      expect(def.range).toBe(24);
      expect(def.effects).toContainEqual({ type:'blinkForward', distance:24 });
      expect(def.effects.some((e) => ['directDamage','weaponStrike','finisherDamage'].includes(e.type))).toBe(false);
    }
    expect(abilityVfxFullSpec('hf_abyss_step_01')?.motifs).toEqual(
      expect.arrayContaining(['fissure','implosion']),
    );
  });

  it('integrates all six endpoints into Rogue without replacing the GOLD lineage', () => {
    expect(CLASSES.rogue.abilities).toEqual(expect.arrayContaining([
      'ambush','hf_shadow_hunt_01','hf_eclipse_mortal_01',
      'eviscerate','hf_cruel_finish_01','hf_last_whisper_01',
      'vanish','hf_shadow_vanish_01','hf_absolute_void_01',
      'hf_umbral_step_01','hf_abyss_step_01',
    ]));
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_ROGUE_PACK_R2=1")
