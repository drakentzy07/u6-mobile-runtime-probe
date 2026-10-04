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

  it('does not use setSpec to launch affinity variants', () => {
    const runtime = readFileSync('src/highfly/affinity_lab_runtime.ts', 'utf8');
    expect(runtime).not.toContain('setSpec(');
    expect(runtime).not.toContain('setSpec (');
  });

  it('keeps SIM authority for elemental riders', () => {
    const leap = readFileSync('src/sim/combat/heroic_leap.ts', 'utf8');
    expect(leap).toContain('highflyAffinityLeapRider(ctx, entity, target, flight.abilityId)');
    expect(leap).toContain("kind: 'dot'");
    expect(leap).toContain("kind: 'slow'");
    expect(leap).toContain("kind: 'stun'");
  });
});
