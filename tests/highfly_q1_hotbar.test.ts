import { describe, expect, it } from 'vitest';
import { HIGHFLY_AFFINITY_CLASSES } from '../src/highfly/affinity_lab_loadouts';
import {
  AFFINITY_MODES,
  buildAffinityHotbar,
  parseWeaponAffinities,
  weaponAffinity,
} from '../src/highfly/weapon_affinity_core';
import { abilitiesKnownAt } from '../src/sim/content/classes';

const known = (id: string, flags = {}) => ({ def: { id, ...flags } });

describe('Q1 native skill slots and equipped weapon elements', () => {
  it('preserves exact native ids when the weapon element changes', () => {
    const kit = [known('heroic_leap'), known('cleave'), known('heroic_strike')];
    for (const mode of AFFINITY_MODES) {
      expect(buildAffinityHotbar('warrior', kit, mode)).toEqual([
        { type: 'ability', id: 'heroic_leap' },
        { type: 'ability', id: 'cleave' },
        { type: 'ability', id: 'heroic_strike' },
      ]);
    }
  });

  it('uses only available active abilities and never fabricates a missing spec skill', () => {
    const kit = [
      known('heroic_strike'),
      known('cleave'),
      known('whirlwind', { passive: true }),
      known('secret', { hiddenFromPlayer: true }),
      known('hf_aff_heroic_leap_fire_01'),
    ];
    const bar = buildAffinityHotbar('warrior', kit, 'fire');
    expect(bar.map((a) => a.id)).toEqual(['cleave', 'heroic_strike']);
  });

  it('caps native action slots at ten and keeps stealth skills usable through the native gate', () => {
    const kit = [
      known('ambush', { requiresStealth: true }),
      ...Array.from({ length: 12 }, (_, i) => known('native_' + i)),
    ];
    const bar = buildAffinityHotbar('rogue', kit);
    expect(bar).toHaveLength(10);
    expect(bar[0].id).toBe('ambush');
    expect(new Set(bar.map((a) => a.id)).size).toBe(10);
  });

  it('resolves the real level-20 kit for all nine classes without adding synthetic skills', () => {
    for (const cls of HIGHFLY_AFFINITY_CLASSES) {
      const kit = abilitiesKnownAt(cls, 20);
      const bar = buildAffinityHotbar(cls, kit);
      expect(bar.length).toBeGreaterThan(0);
      expect(bar.length).toBeLessThanOrEqual(10);
      for (const action of bar) {
        const original = kit.find((k) => k.def.id === action.id);
        expect(original).toBeDefined();
        expect(original?.def.passive).not.toBe(true);
        expect(action.id.startsWith('hf_aff_')).toBe(false);
      }
    }
  });

  it('binds an element to its exact equipped weapon rather than a global class toggle', () => {
    const map = parseWeaponAffinities('{"sword":"fire","staff":"frost","broken":"bogus"}');
    expect(weaponAffinity(map, 'sword')).toBe('fire');
    expect(weaponAffinity(map, 'staff')).toBe('frost');
    expect(weaponAffinity(map, 'dagger')).toBe('base');
    expect(weaponAffinity(map, null)).toBe('base');
    expect(weaponAffinity(map, 'broken')).toBe('base');
    expect(weaponAffinity(parseWeaponAffinities('not json'), 'sword')).toBe('base');
  });
});
