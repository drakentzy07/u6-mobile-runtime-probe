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

MUT="hf_hd_boreal_domain_01"

# Reuse the already-approved BASE + EVO from Production Pack 01.
rep(
    "src/sim/content/classes.ts",
    """      'frostjaw_trap',
      'hf_hunter_prison_01',
      'tame_beast',""",
    """      'frostjaw_trap',
      'hf_hunter_prison_01',
      'hf_hd_boreal_domain_01',
      'tame_beast',""",
)

rep(
    "src/sim/content/classes.ts",
    """  hf_hunter_prison_01: {
    id: 'hf_hunter_prison_01',
    name: 'Prisión del Cazador',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 11,
    cost: 0,
    castTime: 0,
    cooldown: 30,
    range: 30,
    school: 'frost',
    requiresTarget: false,
    effects: [
      {
        type: 'frostjawTrap',
        radius: 5,
        armTime: 0.75,
        lifetime: 30,
        rootDuration: 3,
        slowMult: 0.5,
        slowDuration: 4,
        rootAll: true,
      },
    ],
    description:
      'Evolución de Trampa Colmillo Helado: al activarse, encierra con raíces de hielo a todos los enemigos dentro del campo y ralentiza el área.',
  },""",
    """  hf_hunter_prison_01: {
    id: 'hf_hunter_prison_01',
    name: 'Prisión del Cazador',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 11,
    cost: 0,
    castTime: 0,
    cooldown: 30,
    range: 30,
    school: 'frost',
    requiresTarget: false,
    effects: [
      {
        type: 'frostjawTrap',
        radius: 5,
        armTime: 0.75,
        lifetime: 30,
        rootDuration: 3,
        slowMult: 0.5,
        slowDuration: 4,
        rootAll: true,
      },
    ],
    description:
      'Evolución de Trampa Colmillo Helado: al activarse, encierra con raíces de hielo a todos los enemigos dentro del campo y ralentiza el área.',
  },
  hf_hd_boreal_domain_01: {
    id: 'hf_hd_boreal_domain_01',
    name: 'Dominio Boreal',
    class: 'hunter',
    hiddenFromPlayer: true,
    learnLevel: 11,
    cost: 0,
    castTime: 0,
    cooldown: 30,
    range: 30,
    school: 'frost',
    requiresTarget: false,
    effects: [
      {
        type: 'frostjawTrap',
        radius: 5,
        armTime: 0.75,
        lifetime: 30,
        rootDuration: 3,
        slowMult: 0.5,
        slowDuration: 4,
        rootAll: true,
      },
    ],
    description:
      'MUT PRIME Hunter+Druid. La misma prisión Hunter despierta raíces congeladas y halo lunar Druid; sigue existiendo una sola trampa y una sola autoridad de control.',
  },""",
)

rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_ms_primordial_eruption_01: {
    animationRoute: 'lava_burst',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_primordial_eruption_01',
    sfxRoute: 'lava_burst',
  },""",
    """  hf_ms_primordial_eruption_01: {
    animationRoute: 'lava_burst',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_ms_primordial_eruption_01',
    sfxRoute: 'lava_burst',
  },
  hf_hd_boreal_domain_01: {
    animationRoute: 'frostjaw_trap',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_hd_boreal_domain_01',
    sfxRoute: 'frost_nova',
  },""",
)

write(
    "src/highfly/skill_lab2_hd_frostjaw_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_HD_FROSTJAW_VFX_SPEC: Record<string, AbilityVfxSpec> = {
  hf_hd_boreal_domain_01:{
    c:'#8edcff',p:'frost',pw:1.8,sp:58,rg:2.6,vr:1,li:1.55,lg:4.2,wu:0.25,fin:1,a:'cc'
  },
};

export const HF_HD_FROSTJAW_VFX_FULL_SPEC: Record<string, AbilityVfxFullSpec> = {
  hf_hd_boreal_domain_01:{
    archetype:'cc',
    palette:'frost',
    power:1.8,
    windup:0.25,
    windupStyle:'runes',
    motifs:['barrier','vines','crescents'],
    motifAt:'target',
    impact:{ring:2.6,vRing:true,sparks:58,flipbook:true,light:1.55},
    cc:{style:'tendrils'},
    decal:'rune',
    linger:4.2,
    rim:'#d8f6ff',
    tint:'#77cfff',
    accent:'#d8c8ff',
    hot:0.2,
    screenFx:true,
    finisher:true
  },
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import {
  HF_MS_MAGMA_BURST_VFX_FULL_SPEC,
  HF_MS_MAGMA_BURST_VFX_SPEC,
} from '../highfly/skill_lab2_ms_magma_burst_vfx';""",
    """import {
  HF_MS_MAGMA_BURST_VFX_FULL_SPEC,
  HF_MS_MAGMA_BURST_VFX_SPEC,
} from '../highfly/skill_lab2_ms_magma_burst_vfx';
import {
  HF_HD_FROSTJAW_VFX_FULL_SPEC,
  HF_HD_FROSTJAW_VFX_SPEC,
} from '../highfly/skill_lab2_hd_frostjaw_vfx';""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_MAGMA_BURST_VFX_SPEC[abilityId]) return HF_MS_MAGMA_BURST_VFX_SPEC[abilityId];""",
    """  if (HF_MS_MAGMA_BURST_VFX_SPEC[abilityId]) return HF_MS_MAGMA_BURST_VFX_SPEC[abilityId];
  if (HF_HD_FROSTJAW_VFX_SPEC[abilityId]) return HF_HD_FROSTJAW_VFX_SPEC[abilityId];""",
)
rep(
    "src/render/ability_vfx_registry.ts",
    """  if (HF_MS_MAGMA_BURST_VFX_FULL_SPEC[abilityId]) return HF_MS_MAGMA_BURST_VFX_FULL_SPEC[abilityId];""",
    """  if (HF_MS_MAGMA_BURST_VFX_FULL_SPEC[abilityId]) return HF_MS_MAGMA_BURST_VFX_FULL_SPEC[abilityId];
  if (HF_HD_FROSTJAW_VFX_FULL_SPEC[abilityId]) return HF_HD_FROSTJAW_VFX_FULL_SPEC[abilityId];""",
)

write(
    "tests/highfly_skill_lab2_hd_frostjaw_gold.test.ts",
    """import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/data';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';

const BASE='frostjaw_trap';
const EVO='hf_hunter_prison_01';
const MUT='hf_hd_boreal_domain_01';

function trap(id:string){
  return ABILITIES[id]!.effects.find((e:any)=>e.type==='frostjawTrap') as any;
}

describe('HIGHFLY Hunter Druid 1/7 GOLD — Frostjaw',()=>{
  it('reuses the approved Hunter BASE and EVO, then adds one hidden MUT endpoint',()=>{
    expect(CLASSES.hunter.abilities).toEqual(expect.arrayContaining([BASE,EVO,MUT]));
    expect(ABILITIES[EVO]!.hiddenFromPlayer).toBe(true);
    expect(ABILITIES[MUT]!.hiddenFromPlayer).toBe(true);
  });

  it('keeps one trap authority and preserves the approved Prison mechanics',()=>{
    expect(trap(BASE)).toMatchObject({
      radius:4,armTime:0.75,lifetime:30,rootDuration:3,slowMult:0.5,slowDuration:4,
    });
    expect(trap(EVO)).toMatchObject({
      radius:5,armTime:0.75,lifetime:30,rootDuration:3,slowMult:0.5,slowDuration:4,rootAll:true,
    });
    expect(trap(MUT)).toEqual(trap(EVO));
    expect(ABILITIES[MUT]!.effects).toHaveLength(1);
  });

  it('keeps Hunter placement/SFX identity while Druid DNA stays presentation-side',()=>{
    expect(highflyPresentationRoute(MUT,'animation')).toBe('frostjaw_trap');
    expect(highflyPresentationRoute(MUT,'sfx')).toBe('frost_nova');
    expect(abilityVfxFullSpec(MUT)).toMatchObject({
      archetype:'cc',palette:'frost',screenFx:true,finisher:true,
    });
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_HD_FROSTJAW_GOLD=1")
