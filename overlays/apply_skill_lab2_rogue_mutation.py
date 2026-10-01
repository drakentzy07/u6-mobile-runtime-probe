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
        raise SystemExit(f"{path}: expected 1 anchor, found {n}: {old[:160]!r}")
    write(path, text.replace(old, new, 1))

# ---------------------------------------------------------------------------
# SKILL LAB 2.0 / RUN1
# Rogue lineage: Emboscada -> Cacería Sombría -> Eclipse Mortal.
# Mutation stays on Claude authoritative sim. VFX/animation are presentation.
# ---------------------------------------------------------------------------

rep(
    "src/sim/content/classes.ts",
    """      'ambush',
      'hf_shadow_hunt_01',
      'rupture',""",
    """      'ambush',
      'hf_shadow_hunt_01',
      'hf_eclipse_mortal_01',
      'rupture',""",
)

rep(
    "src/sim/content/classes.ts",
    """  stealth: {
    id: 'stealth',""",
    """  hf_eclipse_mortal_01: {
    id: 'hf_eclipse_mortal_01',
    name: 'Eclipse Mortal',
    class: 'rogue',
    hiddenFromPlayer: true,
    learnLevel: 4,
    cost: 60,
    castTime: 0,
    cooldown: 0,
    range: 0,
    school: 'physical',
    requiresTarget: true,
    awardsCombo: 2,
    requiresStealth: true,
    effects: [
      {
        type: 'weaponStrike',
        bonus: 34,
        requiresBehind: true,
        weaponMult: 2.75,
      },
      {
        type: 'debuffTargetSource',
        kind: 'internal_cd',
        value: 0,
        duration: 6,
        auraId: 'hf_eclipse_mark',
        auraName: 'Marca del Eclipse',
      },
      { type: 'directDamage', min: 18, max: 22 },
      { type: 'directDamage', min: 32, max: 38 },
    ],
    description:
      'Mutación de Cacería Sombría: conserva la apertura real desde Duskveil y por la espalda, aplica Marca del Eclipse y resuelve tres eventos autoritativos: golpe, eco y ejecución.',
    specNotes: {
      subtlety:
        'La Marca del Eclipse queda como estado real de 6 sec para futuras lecturas de Herencia Warlock. Los ecos visuales no son summons ni autoridad de daño.',
    },
  },
  stealth: {
    id: 'stealth',""",
)

# Reuse the true stealth opener path: BASE, EVO and MUTATION all consume the
# same stealth snapshot and therefore keep Rogue identity intact.
rep(
    "src/sim/combat/rogue_stealth_opener.ts",
    """  'hf_shadow_hunt_01',
]);""",
    """  'hf_shadow_hunt_01',
  'hf_eclipse_mortal_01',
]);""",
)

# Presentation routes to the proven Ambush body/SFX while owning its visual identity.
rep(
    "src/highfly/presentation_adapter.ts",
    """  hf_shadow_hunt_01: {
    animationRoute: 'ambush',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_shadow_hunt_01',
    sfxRoute: 'ambush',
  },""",
    """  hf_shadow_hunt_01: {
    animationRoute: 'ambush',
    visualHitMoments: [{ event: 'impact', normalizedTime: 1 }],
    vfxRoute: 'hf_shadow_hunt_01',
    sfxRoute: 'ambush',
  },
  hf_eclipse_mortal_01: {
    animationRoute: 'ambush',
    visualHitMoments: [
      { event: 'impact', normalizedTime: 0.55 },
      { event: 'impact', normalizedTime: 0.78 },
      { event: 'impact', normalizedTime: 1 },
    ],
    vfxRoute: 'hf_eclipse_mortal_01',
    sfxRoute: 'ambush',
  },""",
)

write(
    "src/highfly/skill_lab2_rogue_mutation_vfx.ts",
    """import type { AbilityVfxFullSpec, AbilityVfxSpec } from '../render/ability_vfx_core';

export const HF_ECLIPSE_MORTAL_VFX_SPEC: AbilityVfxSpec = {
  c: '#7d49ff',
  p: 'shadow',
  pw: 1.72,
  sp: 54,
  rg: 1.65,
  vr: 1,
  bl: 1,
  sm: 1,
  li: 1.28,
  lg: 1.45,
  wu: 0.2,
  fin: 1,
  a: 'strike',
};

export const HF_ECLIPSE_MORTAL_VFX_FULL_SPEC: AbilityVfxFullSpec = {
  archetype: 'strike',
  palette: 'shadow',
  power: 1.78,
  chargeStreams: 2,
  windup: 0.14,
  windupStyle: 'vortex',
  motifs: ['chains', 'implosion'],
  motifAt: 'target',
  motifR: 2.4,
  strike: { swings: 3, arc: 'sweep', bleed: true },
  impact: {
    ring: false,
    vRing: 1.1,
    sparks: 58,
    smoke: true,
    light: 1.34,
    trail: 'x',
    flipbook: true,
    focused: true,
  },
  decal: 'portal',
  linger: 1.35,
  rim: '#d8c7ff',
  tint: '#6030d6',
  accent: '#f7f0ff',
  hot: 0.24,
  screenFx: true,
  finisher: true,
};
""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """import { highflyPresentationRoute } from '../highfly/presentation_adapter';""",
    """import {
  HF_ECLIPSE_MORTAL_VFX_FULL_SPEC,
  HF_ECLIPSE_MORTAL_VFX_SPEC,
} from '../highfly/skill_lab2_rogue_mutation_vfx';
import { highflyPresentationRoute } from '../highfly/presentation_adapter';""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (abilityId === 'hf_shadow_hunt_01') return HF_SHADOW_HUNT_VFX_SPEC;""",
    """  if (abilityId === 'hf_shadow_hunt_01') return HF_SHADOW_HUNT_VFX_SPEC;
  if (abilityId === 'hf_eclipse_mortal_01') return HF_ECLIPSE_MORTAL_VFX_SPEC;""",
)

rep(
    "src/render/ability_vfx_registry.ts",
    """  if (abilityId === 'hf_shadow_hunt_01') return HF_SHADOW_HUNT_VFX_FULL_SPEC;""",
    """  if (abilityId === 'hf_shadow_hunt_01') return HF_SHADOW_HUNT_VFX_FULL_SPEC;
  if (abilityId === 'hf_eclipse_mortal_01') return HF_ECLIPSE_MORTAL_VFX_FULL_SPEC;""",
)

write(
    "tests/highfly_skill_lab2_rogue_mutation.test.ts",
    """import { describe, expect, it } from 'vitest';
import { highflyPresentationRoute } from '../src/highfly/presentation_adapter';
import { abilityVfxFullSpec } from '../src/render/ability_vfx_registry';
import { ABILITIES, CLASSES } from '../src/sim/data';

describe('HIGHFLY Skill Lab 2.0 RUN1 - Rogue Eclipse Mortal', () => {
  it('keeps the complete Rogue lineage on the real class kit', () => {
    expect(CLASSES.rogue.abilities).toEqual(
      expect.arrayContaining(['ambush', 'hf_shadow_hunt_01', 'hf_eclipse_mortal_01']),
    );
  });

  it('preserves Ambush identity through BASE, EVO and MUTATION', () => {
    const base = ABILITIES.ambush;
    const evo = ABILITIES.hf_shadow_hunt_01;
    const mutation = ABILITIES.hf_eclipse_mortal_01;
    for (const def of [base, evo, mutation]) {
      expect(def.requiresStealth).toBe(true);
      expect(def.requiresTarget).toBe(true);
      expect(def.effects.some((e) => e.type === 'weaponStrike' && e.requiresBehind)).toBe(true);
    }
    expect(evo.awardsCombo).toBe(2);
    expect(mutation.awardsCombo).toBe(2);
  });

  it('owns exactly three authoritative damage events and one real Eclipse mark', () => {
    const mutation = ABILITIES.hf_eclipse_mortal_01;
    const damage = mutation.effects.filter(
      (e) => e.type === 'weaponStrike' || e.type === 'directDamage',
    );
    expect(damage).toHaveLength(3);
    expect(mutation.effects).toContainEqual(
      expect.objectContaining({
        type: 'debuffTargetSource',
        auraId: 'hf_eclipse_mark',
        auraName: 'Marca del Eclipse',
        duration: 6,
      }),
    );
  });

  it('keeps presentation separate from damage authority', () => {
    expect(highflyPresentationRoute('hf_eclipse_mortal_01', 'animation')).toBe('ambush');
    expect(highflyPresentationRoute('hf_eclipse_mortal_01', 'vfx')).toBe('hf_eclipse_mortal_01');
    const vfx = abilityVfxFullSpec('hf_eclipse_mortal_01');
    expect(vfx).toBeTruthy();
    expect(vfx?.strike?.swings).toBe(3);
    expect(vfx?.motifs).toEqual(expect.arrayContaining(['implosion']));
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_RUN1_ROGUE_ECLIPSE=1")


# RUN1B premium choreography: use Claude's existing pooled sequencer for
# shadow-echo ribbons and staggered slash beats. No summoned entities and no
# presentation-owned damage.
rep(
    "src/render/ability_vfx/sequencer.ts",
    "    const caster = host.anchorOf(slot.casterId, 0.58);\n    if (caster) {\n      host.burstAt(",
    "    const caster = host.anchorOf(slot.casterId, 0.58);\n    if (caster) {\n      if (slot.abilityId === 'hf_eclipse_mortal_01' && slot.tier === 0) {\n        const victim = host.anchorOf(slot.targetId, 0.55);\n        if (victim) {\n          const dx = victim.x - caster.x;\n          const dz = victim.z - caster.z;\n          const len = Math.hypot(dx, dz) || 1;\n          const rx = dz / len;\n          const rz = -dx / len;\n          for (const offset of [-0.58, 0, 0.58]) {\n            host.pathRibbon(slot.color, 0.34, 0.34, (pts) => {\n              for (let i = 0; i < 10; i++) {\n                const u = i / 9;\n                const sway = Math.sin(u * Math.PI) * offset;\n                pts[i].set(\n                  caster.x + dx * u + rx * sway,\n                  caster.y + (victim.y - caster.y) * u + Math.sin(u * Math.PI) * 0.12,\n                  caster.z + dz * u + rz * sway,\n                );\n              }\n              return 10;\n            });\n          }\n          host.burstAt(caster.x, caster.y, caster.z, slot.color, 14, 0.9, 'smoke');\n          host.countPrimitive(slot.abilityId, 4);\n        }\n      }\n      host.burstAt(",
)

rep(
    "src/render/ability_vfx/sequencer.ts",
    "type BeatKind = 'burst' | 'pillar' | 'orbital';",
    "type BeatKind = 'burst' | 'pillar' | 'orbital' | 'slash';",
)

rep(
    "src/render/ability_vfx/sequencer.ts",
    """      case 'orbital':
        // one orb slam (gallery orbitals): the strike arc plus its spark pop
        host.boltPoints(""",
    """      case 'slash': {
        host.slashStyled(
          { x: beat.x, y: beat.y, z: beat.z },
          beat.color,
          beat.a < 0.5 ? 'horizontal' : 'sweep',
          beat.b,
        );
        host.burstAt(beat.x, beat.y, beat.z, beat.accent, 12, 0.9, 'sparks');
        host.countPrimitive(beat.abilityId, 2);
        break;
      }
      case 'orbital':
        // one orb slam (gallery orbitals): the strike arc plus its spark pop
        host.boltPoints(""",
)

rep(
    "src/render/ability_vfx/sequencer.ts",
    """          host.slashStyled(at, c, spec.strike?.arc ?? 'horizontal', SPECTACLE.strikeArc);
          host.countPrimitive(slot.abilityId, 1);
          if (spec.strike?.bleed) {""",
    """          host.slashStyled(at, c, spec.strike?.arc ?? 'horizontal', SPECTACLE.strikeArc);
          host.countPrimitive(slot.abilityId, 1);
          if (slot.abilityId === 'hf_eclipse_mortal_01' && slot.tier === 0) {
            this.scheduleBeat(
              0.1,
              'slash',
              slot.abilityId,
              at.x,
              at.y,
              at.z,
              slot.color,
              slot.accent,
              0,
              SPECTACLE.strikeArc * 0.92,
            );
            this.scheduleBeat(
              0.2,
              'slash',
              slot.abilityId,
              at.x,
              at.y,
              at.z,
              slot.color,
              slot.accent,
              1,
              SPECTACLE.strikeArc * 1.06,
            );
          }
          if (spec.strike?.bleed) {""",
)

rep(
    "src/render/ability_vfx/sequencer.ts",
    """        if ((spec.strike?.swings ?? 1) > 1) {
          slot.swing2At = slot.t + 0.22;
          slot.swing2Done = false;""",
    """        if ((spec.strike?.swings ?? 1) > 1 && slot.abilityId !== 'hf_eclipse_mortal_01') {
          slot.swing2At = slot.t + 0.22;
          slot.swing2Done = false;""",
)

write(
    "tests/highfly_skill_lab2_rogue_premium.test.ts",
    """import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { HF_ECLIPSE_MORTAL_VFX_FULL_SPEC } from '../src/highfly/skill_lab2_rogue_mutation_vfx';

describe('HIGHFLY Skill Lab 2.0 RUN1B - Eclipse Mortal premium choreography', () => {
  it('pins a focused three-cut shadow finisher without AoE-ring noise', () => {
    expect(HF_ECLIPSE_MORTAL_VFX_FULL_SPEC).toMatchObject({
      archetype: 'strike',
      palette: 'shadow',
      chargeStreams: 2,
      windupStyle: 'vortex',
      strike: { swings: 3, arc: 'sweep', bleed: true },
      impact: { ring: false, trail: 'x', smoke: true, focused: true },
      finisher: true,
      screenFx: true,
    });
    expect(HF_ECLIPSE_MORTAL_VFX_FULL_SPEC.motifs).toEqual(
      expect.arrayContaining(['chains', 'implosion']),
    );
  });

  it('uses pooled ribbons and delayed slashes instead of shadow NPCs or damage colliders', () => {
    const source = readFileSync('src/render/ability_vfx/sequencer.ts', 'utf8');
    expect(source).toContain("slot.abilityId === 'hf_eclipse_mortal_01'");
    expect(source).toContain("for (const offset of [-0.58, 0, 0.58])");
    expect(source).toContain("host.pathRibbon(slot.color, 0.34, 0.34");
    expect(source).toContain("'slash'");
    expect(source).not.toContain('hf_eclipse_shadow_npc');
    expect(source).not.toContain('hf_eclipse_damage_collider');
  });
});
""",
)

print("HIGHFLY_SKILL_LAB2_RUN1B_ROGUE_PREMIUM=1")
