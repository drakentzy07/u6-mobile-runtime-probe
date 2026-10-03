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
# Mage + Shaman 3/7 GOLD — Aether Darts / Arcane Missiles lineage
# BASE Claude: arcane_missiles/Aether Darts.
# EVO/MUT preserve the real 3 sec / 3 tick channel, ranks, Mana and target
# authority exactly. Extra visible darts and lightning networking are visual
# grouping around those same authoritative channel ticks.
# ---------------------------------------------------------------------------

EVO = "hf_ms_aether_storm_01"
MUT = "hf_ms_thousand_celestial_darts_01"

insert_after("arcane_missiles", [EVO, MUT])

rep(
    "src/sim/content/classes.ts",
    """  polymorph: {
    id: 'polymorph',""",
    """  hf_ms_aether_storm_01: {
    id: 'hf_ms_aether_storm_01',
    name: 'Tormenta de Éter',
    class: 'mage',
    learnLevel: 5,
    specs: ['arcane'],
    hiddenFromPlayer: true,
    cost: 50,
    castTime: 0,
    channel: { duration: 3, ticks: 3 },
    cooldown: 0,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    effects: [{ type: 'directDamage', min: 8, max: 8 }],
    ranks: [
      { rank: 2, level: 14, cost: 75, effects: [{ type: 'directDamage', min: 14, max: 14 }] },
      { rank: 3, level: 18, cost: 90, effects: [{ type: 'directDamage', min: 18, max: 18 }] },
      { rank: 4, level: 20, cost: 105, effects: [{ type: 'directDamage', min: 22, max: 22 }] },
    ],
    description:
      'EVO de Dardos Etéreos. Conserva exactamente los tres ticks del canal; los proyectiles visibles adicionales se agrupan dentro de esos eventos y no crean daño extra.',
  },
  hf_ms_thousand_celestial_darts_01: {
    id: 'hf_ms_thousand_celestial_darts_01',
    name: 'Mil Dardos Celestes',
    class: 'mage',
    learnLevel: 5,
    specs: ['arcane'],
    hiddenFromPlayer: true,
    cost: 50,
    castTime: 0,
    channel: { duration: 3, ticks: 3 },
    cooldown: 0,
    range: 30,
    school: 'arcane',
    requiresTarget: true,
    effects: [{ type: 'directDamage', min: 8, max: 8 }],
    ranks: [
      { rank: 2, level: 14, cost: 75, effects: [{ type: 'directDamage', min: 14, max: 14 }] },
      { rank: 3, level: 18, cost: 90, effects: [{ type: 'directDamage', min: 18, max: 18 }] },
      { rank: 4, level: 20, cost: 105, effects: [{ type: 'directDamage', min: 22, max: 22 }] },
    ],
    description:
      'MUT PRIME Mage+Shaman. Los dardos orbitan y una red de rayos los conecta antes del remate; el SIM sigue autorizando sólo los tres ticks reales del canal.',
  },
  polymorph: {
    id: 'polymorph',""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_celestial_extinction_01: {
    animationRoute: 'meteor',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.72 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_celestial_extinction_01',
    sfxRoute: 'meteor',
  },""",
    """  hf_ms_celestial_extinction_01: {
    animationRoute: 'meteor',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.72 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_celestial_extinction_01',
    sfxRoute: 'meteor',
  },
  hf_ms_aether_storm_01: {
    animationRoute: 'arcane_missiles',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.33 },
      { event: 'impact', normalizedTime: 0.66 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_aether_storm_01',
    sfxRoute: 'arcane_missiles',
  },
  hf_ms_thousand_celestial_darts_01: {
    animationRoute: 'arcane_missiles',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.33 },
      { event: 'impact', normalizedTime: 0.66 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_thousand_celestial_darts_01',
    sfxRoute: 'arcane_missiles',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_aether_darts_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_AETHER_DARTS_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_aether_storm_01:{c:'#b776ff',p:'arcane',pw:1.35,sp:28,vr:1,li:1.25,lg:3.2,wu:0.25,a:'bolt'},
  hf_ms_thousand_celestial_darts_01:{c:'#a95cff',p:'arcane',pw:1.65,sp:42,vr:1,li:1.55,lg:3.8,wu:0.25,fin:1,a:'bolt'},
};

export const HF_MS_AETHER_DARTS_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_aether_storm_01:{
    archetype:'bolt',palette:'arcane',power:1.35,windup:0.25,windupStyle:'orb',
    bolt:{speed:24,style:'shard',headScale:0.9,coils:true,jagged:false,forkEvery:0},
    chargeStreams:3,motifs:['orbitals'],motifAt:'caster',linger:3.2,
    impact:{flipbook:true,ring:1.4,vRing:true,sparks:28,debris:false,smoke:false,light:1.25},
    rim:'#c99bff',accent:'#f1d9ff'
  },
  hf_ms_thousand_celestial_darts_01:{
    archetype:'bolt',palette:'arcane',power:1.65,windup:0.25,windupStyle:'vortex',
    bolt:{speed:26,style:'shard',headScale:1.0,coils:true,jagged:true,forkEvery:1},
    chargeStreams:5,motifs:['orbitals','pillars'],motifAt:'caster',linger:3.8,shaft:true,
    impact:{flipbook:true,ring:1.8,vRing:true,sparks:42,debris:false,smoke:true,light:1.55},
    rim:'#b86cff',accent:'#d8f2ff',screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_METEOR_VFX_FULL_SPEC,
  HF_MS_METEOR_VFX_SPEC,
} from '../highfly/skill_lab2_ms_meteor_vfx';""",
    """import {
  HF_MS_METEOR_VFX_FULL_SPEC,
  HF_MS_METEOR_VFX_SPEC,
} from '../highfly/skill_lab2_ms_meteor_vfx';
import {
  HF_MS_AETHER_DARTS_VFX_FULL_SPEC,
  HF_MS_AETHER_DARTS_VFX_SPEC,
} from '../highfly/skill_lab2_ms_aether_darts_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_METEOR_VFX_SPEC[abilityId]) return HF_MS_METEOR_VFX_SPEC[abilityId];""",
    """  if (HF_MS_METEOR_VFX_SPEC[abilityId]) return HF_MS_METEOR_VFX_SPEC[abilityId];
  if (HF_MS_AETHER_DARTS_VFX_SPEC[abilityId]) return HF_MS_AETHER_DARTS_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_METEOR_VFX_FULL_SPEC[abilityId]) return HF_MS_METEOR_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_METEOR_VFX_FULL_SPEC[abilityId]) return HF_MS_METEOR_VFX_FULL_SPEC[abilityId];
  if (HF_MS_AETHER_DARTS_VFX_FULL_SPEC[abilityId]) return HF_MS_AETHER_DARTS_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_aether_darts_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='arcane_missiles';
const EVO='hf_ms_aether_storm_01';
const MUT='hf_ms_thousand_celestial_darts_01';

function authority(id:string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class,specs:d.specs,cost:d.cost,castTime:d.castTime,channel:d.channel,
    cooldown:d.cooldown,range:d.range,school:d.school,requiresTarget:d.requiresTarget,
    effects:d.effects,ranks:d.ranks,
  };
}

describe('HIGHFLY Mage Shaman 3/7 GOLD — Aether Darts',()=>{
  it('preserves Aether Darts SIM authority and all ranks in EVO/MUT',()=>{
    expect(authority(EVO)).toEqual(authority(BASE));
    expect(authority(MUT)).toEqual(authority(BASE));
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
    expect(ABILITIES[EVO]!.hiddenFromPlayer).toBe(true);
    expect(ABILITIES[MUT]!.hiddenFromPlayer).toBe(true);
  });

  it('pins the real three-tick channel contract',()=>{
    expect(ABILITIES[EVO]!.channel).toEqual({duration:3,ticks:3});
    expect(ABILITIES[MUT]!.channel).toEqual({duration:3,ticks:3});
    expect(ABILITIES[MUT]!.effects).toEqual(ABILITIES[BASE]!.effects);
  });

  it('keeps Mage channel animation and SFX authority',()=>{
    expect(highflyPresentationRoute(EVO,'animation')).toBe('arcane_missiles');
    expect(highflyPresentationRoute(MUT,'animation')).toBe('arcane_missiles');
    expect(highflyPresentationRoute(EVO,'sfx')).toBe('arcane_missiles');
    expect(highflyPresentationRoute(MUT,'sfx')).toBe('arcane_missiles');
  });

  it('adds premium orbit/network presentation without adding SIM ticks',()=>{
    expect(abilityVfxFullSpec(EVO)).toMatchObject({archetype:'bolt',palette:'arcane'});
    expect(abilityVfxFullSpec(MUT)).toMatchObject({archetype:'bolt',palette:'arcane',screenFx:true,finisher:true});
    expect(ABILITIES[MUT]!.channel?.ticks).toBe(3);
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_AETHER_DARTS_GOLD=1")
