import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { ABILITIES, CLASSES } from '../src/sim/content/classes';
import { heroicLeapPlacementPreview } from '../src/sim/combat/heroic_leap';
import {
  HIGHFLY_AFFINITY_AUDIT_V1,
  HIGHFLY_AFFINITY_CLASSES,
} from '../src/highfly/affinity_lab_loadouts';

describe('HIGHFLY AFFINITY LAB v1', () => {
  it('keeps all nine native Claude classes', () => {
    expect(HIGHFLY_AFFINITY_CLASSES).toEqual([
      'warrior',
      'paladin',
      'rogue',
      'warlock',
      'mage',
      'shaman',
      'hunter',
      'druid',
      'priest',
    ]);
  });

  it('implements only Tanda 1 while keeping the other six classes audited', () => {
    for (const cls of HIGHFLY_AFFINITY_CLASSES) {
      const receptors = HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors;
      expect(receptors.length).toBeGreaterThanOrEqual(4);
      expect(receptors.length).toBeLessThanOrEqual(5);
    }
    const implemented = HIGHFLY_AFFINITY_CLASSES.flatMap((cls) =>
      HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors
        .filter((receptor) => receptor.implemented)
        .map((receptor) => `${cls}:${receptor.id}`),
    );
    expect(implemented).toEqual([
      'warrior:heroic_leap',
      'warrior:whirlwind',
      'warrior:thunder_clap',
      'warrior:cleave',
      'rogue:eviscerate',
      'rogue:ambush',
      'rogue:sinister_strike',
      'rogue:rupture',
      'mage:fireball',
      'mage:arcane_missiles',
      'mage:frostbolt',
      'mage:frost_nova',
    ]);
  });

  it('keeps Heroic Leap base plus three same-class elemental variants', () => {
    const ids = [
      'hf_aff_heroic_leap_fire_01',
      'hf_aff_heroic_leap_frost_01',
      'hf_aff_heroic_leap_lightning_01',
    ];
    expect(CLASSES.warrior.abilities).toEqual(expect.arrayContaining(ids));
    for (const id of ids) {
      const ability = ABILITIES[id];
      expect(ability).toBeTruthy();
      expect(ability.class).toBe('warrior');
      expect(ability.targetMode).toBe('position');
      expect(ability.requiresTarget).toBe(false);
      expect(ability.range).toBe(ABILITIES.heroic_leap.range);
      expect(ability.cooldown).toBe(ABILITIES.heroic_leap.cooldown);
    }
    for (const cls of HIGHFLY_AFFINITY_CLASSES.filter((cls) => cls !== 'warrior')) {
      for (const id of ids) expect(CLASSES[cls].abilities).not.toContain(id);
    }
  });

  it('reuses the exact Heroic Leap placement sweep for affinity ids', () => {
    const caster = { pos: { x: 3, y: 0, z: 4 }, onGround: true };
    const point = { x: 14, z: 17 };
    const base = heroicLeapPlacementPreview(12345, caster, 'heroic_leap', point);
    expect(
      heroicLeapPlacementPreview(12345, caster, 'hf_aff_heroic_leap_fire_01', point),
    ).toEqual(base);
    expect(
      heroicLeapPlacementPreview(12345, caster, 'hf_aff_heroic_leap_frost_01', point),
    ).toEqual(base);
    expect(
      heroicLeapPlacementPreview(12345, caster, 'hf_aff_heroic_leap_lightning_01', point),
    ).toEqual(base);
  });

  it('keeps Claude no-respec-in-combat rule in production while the LAB exits combat only for explicit tester spec changes', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts', 'utf8');
    const selectSpecStart = runtime.indexOf('function selectSpec(');
    const resetStart = runtime.indexOf('function resetLab(');
    const body = runtime.slice(selectSpecStart, resetStart);
    expect(body).toContain('p.inCombat = false');
    expect(body).toContain('p.autoAttack = false');
    expect(body).toContain('const ok = sim.setSpec(spec)');
  });

  it('never changes spec implicitly while preparing or casting affinity variants', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts', 'utf8');
    const prepareStart = runtime.indexOf('function prepareCast(');
    const castStart = runtime.indexOf('function cast(');
    const selectSpecStart = runtime.indexOf('function selectSpec(');
    const resetStart = runtime.indexOf('function resetLab(');
    expect(prepareStart).toBeGreaterThanOrEqual(0);
    expect(castStart).toBeGreaterThanOrEqual(0);
    expect(selectSpecStart).toBeGreaterThanOrEqual(0);
    expect(resetStart).toBeGreaterThan(selectSpecStart);

    const prepareBody = runtime.slice(prepareStart, castStart);
    const castBody = runtime.slice(castStart, selectSpecStart);
    const explicitSpecUiBody = runtime.slice(selectSpecStart, resetStart);

    expect(prepareBody).not.toContain('setSpec(');
    expect(castBody).not.toContain('setSpec(');
    expect(explicitSpecUiBody).toContain('sim.setSpec(spec)');
  });

  it('uses canonical Claude entity targeting and keeps ranged casters out of melee staging', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts', 'utf8');
    expect(runtime).toContain("meta?.cls === 'mage'");
    expect(runtime).toContain('? 12');
    expect(runtime).toContain('sim.castAbility(id, p.id);');
    expect(runtime).not.toContain('sim.castAbility(id, p.id, target.id)');
  });

  it('stages self-centered AoE receptors inside their authored radius', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts', 'utf8');
    expect(runtime).toContain("receptor.target === 'none'");
    expect(runtime).toContain('? 5');
  });

  it('keeps SIM authority for elemental riders', () => {
    const leap = readFileSync('src/sim/combat/heroic_leap.ts', 'utf8');
    expect(leap).toContain('highflyAffinityLeapRider(ctx, entity, target, flight.abilityId)');
    expect(leap).toContain("kind: 'dot'");
    expect(leap).toContain("kind: 'slow'");
    expect(leap).toContain("kind: 'stun'");
  });
});
