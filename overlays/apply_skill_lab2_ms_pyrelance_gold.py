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
# Mage + Shaman 1/7 GOLD — Pyrelance lineage
# BASE Claude: pyroblast/Pyrelance.
# EVO/MUT preserve Mage fire-cast authority, mana, Hot Streak and projectile.
# Shaman DNA is presentation-only in this first integration pass; no VFX-created
# damage. This keeps the authored balance exact while proving the fusion route.
# ---------------------------------------------------------------------------

insert_after("pyroblast", ["hf_ms_crimson_pyrelance_01", "hf_ms_crimson_rain_01"])

rep(
    "src/sim/content/classes.ts",
    """  temporal_mend: {
    id: 'temporal_mend',""",
    """  hf_ms_crimson_pyrelance_01: {
    id: 'hf_ms_crimson_pyrelance_01',
    specs: ['fire'],
    name: 'Lanza Pírica Carmesí',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 5,
    cost: 125,
    castTime: 6.0,
    cooldown: 0,
    range: 30,
    school: 'fire',
    requiresTarget: true,
    projectileFx: 'heavyBolt',
    effects: [
      { type: 'directDamage', min: 179, max: 236 },
      { type: 'dot', total: 50, duration: 12, interval: 2 },
    ],
    description:
      'EVO de Lanza Pírica. Conserva exactamente el proyectil pesado, Mana, cast, impacto y DoT de Pyrelance; hélices, densidad carmesí e impacto reforzado son presentación HIGHFLY.',
  },
  hf_ms_crimson_rain_01: {
    id: 'hf_ms_crimson_rain_01',
    specs: ['fire'],
    name: 'Lluvia Carmesí',
    class: 'mage',
    hiddenFromPlayer: true,
    learnLevel: 5,
    cost: 125,
    castTime: 6.0,
    cooldown: 0,
    range: 30,
    school: 'fire',
    requiresTarget: true,
    projectileFx: 'heavyBolt',
    effects: [
      { type: 'directDamage', min: 179, max: 236 },
      { type: 'dot', total: 50, duration: 12, interval: 2 },
    ],
    description:
      'MUT PRIME Mage+Shaman. La fractura magmática y las réplicas carmesí son coreografía premium; Pyrelance sigue siendo la única autoridad SIM hasta que un rider secundario sea balanceado explícitamente.',
  },
  temporal_mend: {
    id: 'temporal_mend',""",
)

# Preserve the real Fire Mage Hot Streak hooks for EVO and mutation.
# Earlier HIGHFLY overlays may already have extended these arrays, so this must
# be additive/idempotent rather than anchored to Claude's original adjacency.
def append_const_array_items(path: str, const_name: str, items: list[str]) -> None:
    text = read(path)
    marker = f"export const {const_name}: readonly string[] = "
    start = text.find(marker)
    if start < 0:
        raise SystemExit(f"{path}: missing array constant {const_name}")
    array_start = text.find("[", start)
    array_end = text.find("];", array_start)
    if array_start < 0 or array_end < 0:
        raise SystemExit(f"{path}: malformed array constant {const_name}")
    body = text[array_start + 1:array_end]
    missing = [item for item in items if f"'{item}'" not in body]
    if not missing:
        return
    insertion = "".join(f"\n  '{item}'," for item in missing)
    body = body.rstrip() + insertion + "\n"
    write(path, text[:array_start + 1] + body + text[array_end:])

append_const_array_items(
    "src/sim/combat/fire_mage.ts",
    "HOT_STREAK_BUILDERS",
    ["hf_ms_crimson_pyrelance_01", "hf_ms_crimson_rain_01"],
)
append_const_array_items(
    "src/sim/combat/fire_mage.ts",
    "HOT_STREAK_SPENDERS",
    ["hf_ms_crimson_pyrelance_01", "hf_ms_crimson_rain_01"],
)

# Mage body/staff/casting always wins; Shaman contributes presentation DNA only.
rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_wp_unbreakable_dawn_01: { animationRoute:'raised_guard', visualHitMoments:[], vfxRoute:'hf_wp_unbreakable_dawn_01', sfxRoute:'aegis_first_dawn' },""",
    """  hf_wp_unbreakable_dawn_01: { animationRoute:'raised_guard', visualHitMoments:[], vfxRoute:'hf_wp_unbreakable_dawn_01', sfxRoute:'aegis_first_dawn' },
  hf_ms_crimson_pyrelance_01: {
    animationRoute: 'pyroblast',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_crimson_pyrelance_01',
    sfxRoute: 'pyroblast',
  },
  hf_ms_crimson_rain_01: {
    animationRoute: 'pyroblast',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.58 },
      { event: 'impact', normalizedTime: 0.79 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_ms_crimson_rain_01',
    sfxRoute: 'pyroblast',
  },""",
)

write(
    "src/highfly/skill_lab2_ms_pyrelance_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_MS_PYRELANCE_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_ms_crimson_pyrelance_01:{c:'#d44b2f',p:'fire',pw:1.75,sp:56,rg:1.25,vr:1,sm:1,li:1.1,lg:4.5,wu:2.5,a:'bolt'},
  hf_ms_crimson_rain_01:{c:'#b92b24',p:'fire',pw:2.0,sp:72,rg:1.45,vr:1,sm:1,li:1.35,lg:5.0,wu:2.5,fin:1,a:'bolt'},
};

export const HF_MS_PYRELANCE_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_ms_crimson_pyrelance_01:{
    archetype:'bolt',palette:'fire',power:1.75,windup:2.5,windupStyle:'ascend',hot:0.55,
    bolt:{speed:20,style:'rock',headScale:2.15,coils:true,jagged:false,forkEvery:0},
    dot:{drip:'fall'},linger:4.5,motifs:['fissure'],motifAt:'target',decal:'crack',
    impact:{flipbook:true,ring:2.2,vRing:true,sparks:56,debris:true,smoke:true,light:1.1},
    rim:'#ff6c45',accent:'#ffd0a0'
  },
  hf_ms_crimson_rain_01:{
    archetype:'bolt',palette:'fire',power:2.0,windup:2.5,windupStyle:'ascend',hot:0.62,
    bolt:{speed:20,style:'rock',headScale:2.35,coils:true,jagged:false,forkEvery:0},
    chargeStreams:3,dot:{drip:'fall'},linger:5,motifs:['fissure','pillars'],motifAt:'target',
    decal:'crack',shaft:true,
    impact:{flipbook:true,ring:2.5,vRing:true,sparks:72,debris:true,smoke:true,light:1.35},
    rim:'#ff4936',accent:'#ffd1a8',screenFx:true,finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_WP_7OF7_VFX_FULL_SPEC,
  HF_WP_7OF7_VFX_SPEC,
} from '../highfly/skill_lab2_wp_7of7_vfx';""",
    """import {
  HF_WP_7OF7_VFX_FULL_SPEC,
  HF_WP_7OF7_VFX_SPEC,
} from '../highfly/skill_lab2_wp_7of7_vfx';
import {
  HF_MS_PYRELANCE_VFX_FULL_SPEC,
  HF_MS_PYRELANCE_VFX_SPEC,
} from '../highfly/skill_lab2_ms_pyrelance_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_WP_7OF7_VFX_SPEC[abilityId]) return HF_WP_7OF7_VFX_SPEC[abilityId];""",
    """  if (HF_WP_7OF7_VFX_SPEC[abilityId]) return HF_WP_7OF7_VFX_SPEC[abilityId];
  if (HF_MS_PYRELANCE_VFX_SPEC[abilityId]) return HF_MS_PYRELANCE_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_WP_7OF7_VFX_FULL_SPEC[abilityId]) return HF_WP_7OF7_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_WP_7OF7_VFX_FULL_SPEC[abilityId]) return HF_WP_7OF7_VFX_FULL_SPEC[abilityId];
  if (HF_MS_PYRELANCE_VFX_FULL_SPEC[abilityId]) return HF_MS_PYRELANCE_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_ms_pyrelance_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { HOT_STREAK_BUILDERS, HOT_STREAK_SPENDERS } from '../src/sim/combat/fire_mage';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='pyroblast';
const EVO='hf_ms_crimson_pyrelance_01';
const MUT='hf_ms_crimson_rain_01';

function authority(id:string) {
  const d=ABILITIES[id]!;
  return {
    class:d.class,specs:d.specs,cost:d.cost,castTime:d.castTime,cooldown:d.cooldown,
    range:d.range,school:d.school,requiresTarget:d.requiresTarget,projectileFx:d.projectileFx,
    effects:d.effects,
  };
}

describe('HIGHFLY Mage Shaman 1/7 GOLD — Pyrelance',()=>{
  it('preserves Pyrelance SIM authority in EVO/MUT',()=>{
    expect(authority(EVO)).toEqual(authority(BASE));
    expect(authority(MUT)).toEqual(authority(BASE));
    expect(CLASSES.mage.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
    expect(ABILITIES[EVO]!.hiddenFromPlayer).toBe(true);
    expect(ABILITIES[MUT]!.hiddenFromPlayer).toBe(true);
  });

  it('keeps both HIGHFLY stages inside the real Hot Streak builder/spender engine',()=>{
    expect(HOT_STREAK_BUILDERS).toEqual(expect.arrayContaining([EVO,MUT]));
    expect(HOT_STREAK_SPENDERS).toEqual(expect.arrayContaining([EVO,MUT]));
  });

  it('keeps Mage casting/body authority while Shaman DNA lives in presentation',()=>{
    expect(highflyPresentationRoute(EVO,'animation')).toBe('pyroblast');
    expect(highflyPresentationRoute(MUT,'animation')).toBe('pyroblast');
    expect(highflyPresentationRoute(EVO,'sfx')).toBe('pyroblast');
    expect(highflyPresentationRoute(MUT,'sfx')).toBe('pyroblast');
  });

  it('pins premium crimson/magma VFX without adding SIM hits',()=>{
    expect(abilityVfxFullSpec(EVO)).toMatchObject({archetype:'bolt',palette:'fire'});
    expect(abilityVfxFullSpec(MUT)).toMatchObject({archetype:'bolt',palette:'fire',finisher:true,screenFx:true});
    expect(ABILITIES[MUT]!.effects).toEqual(ABILITIES[BASE]!.effects);
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_MS_PYRELANCE_GOLD=1")
