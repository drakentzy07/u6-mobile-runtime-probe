import { readFileSync } from 'node:fs';
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

describe('Q1.1 mobile double-crescent geometry', () => {
  it('owns ten unique non-overlapping skill seats in landscape', () => {
    const css = readFileSync('src/styles/hf_affinity_lab.css', 'utf8');
    expect(css).toContain('HIGHFLY mobile landscape — approved DOUBLE CRESCENT');
    expect(css).toContain('@media (orientation: landscape)');

    const points: Array<{ slot: number; right: number; bottom: number }> = [];
    for (let slot = 1; slot <= 10; slot++) {
      const re = new RegExp(
        '\\[data-hotbar-slot="' + slot + '"\\] \\{ right: (\\d+)px !important; bottom: (\\d+)px !important; \\}',
      );
      const match = css.match(re);
      expect(match, 'missing crescent seat for slot ' + slot).not.toBeNull();
      points.push({ slot, right: Number(match?.[1]), bottom: Number(match?.[2]) });
    }
    expect(new Set(points.map((p) => p.right + ':' + p.bottom)).size).toBe(10);
    for (let i = 0; i < points.length; i++) {
      for (let j = i + 1; j < points.length; j++) {
        const dx = points[i].right - points[j].right;
        const dy = points[i].bottom - points[j].bottom;
        expect(Math.hypot(dx, dy), 'overlap slots ' + points[i].slot + '/' + points[j].slot).toBeGreaterThanOrEqual(42);
      }
    }

    // S1..S5 are the inner/right arc, S6..S10 the outer/left arc.
    const innerMean = points.slice(0, 5).reduce((n, p) => n + p.right, 0) / 5;
    const outerMean = points.slice(5).reduce((n, p) => n + p.right, 0) / 5;
    expect(outerMean).toBeGreaterThan(innerMean);
  });
});

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
