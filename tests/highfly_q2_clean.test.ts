import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import {
  applyHighflyElementalFinisher,
  configureHighflyWeaponElement,
  highflyElementalFinisherReady,
  highflyWeaponElement,
} from '../src/sim/combat/highfly_q2_elemental_basic';
import { BUILTIN_WORLD, MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { Sim } from '../src/sim/sim';
import { placePlayerInOpenField } from './helpers/open_field';

function fixture() {
  const sim = new Sim({
    seed: 4242,
    playerClass: 'warrior',
    autoEquip: true,
    world: { ...BUILTIN_WORLD, camps: [], npcs: {}, groundObjects: [] },
  });
  sim.setPlayerLevel(60);
  placePlayerInOpenField(sim);
  const p = sim.player;
  const target = createMob(sim.nextId++, MOBS.forest_wolf, 1, {
    x: p.pos.x,
    y: p.pos.y,
    z: p.pos.z + 2,
  });
  target.hostile = true;
  target.maxHp = target.hp = 5000;
  sim.addEntity(target);
  p.targetId = target.id;
  return { sim, p, target };
}

describe('HIGHFLY Q2 Claude clean foundation', () => {
  it('keeps ATK4 locked without an active equipped-weapon gem', () => {
    const { p } = fixture();
    expect(highflyWeaponElement(p)).toBe('base');
    expect(highflyElementalFinisherReady(p)).toBe(false);
    configureHighflyWeaponElement(p, p.mainhandItemId, 'base', false);
    expect(highflyElementalFinisherReady(p)).toBe(false);
  });

  it.each([
    ['fire', 'dot'],
    ['frost', 'slow'],
    ['lightning', 'stun'],
  ] as const)('%s enables ATK4 and adds only its elemental rider', (mode, kind) => {
    const { sim, p, target } = fixture();
    configureHighflyWeaponElement(p, p.mainhandItemId, mode, true);
    expect(highflyWeaponElement(p)).toBe(mode);
    expect(highflyElementalFinisherReady(p)).toBe(true);
    const hp = target.hp;
    applyHighflyElementalFinisher(sim.ctx, p, target, mode);
    expect(target.hp).toBe(hp);
    expect(target.auras.some((a) => a.kind === kind)).toBe(true);
  });

  it('air enables ATK4 and uses displacement without rewriting damage', () => {
    const { sim, p, target } = fixture();
    configureHighflyWeaponElement(p, p.mainhandItemId, 'air', true);
    const before = { x: target.pos.x, z: target.pos.z, hp: target.hp };
    applyHighflyElementalFinisher(sim.ctx, p, target, 'air');
    expect(target.hp).toBe(before.hp);
    expect(Math.hypot(target.pos.x - before.x, target.pos.z - before.z)).toBeGreaterThan(0);
  });

  it('rejects a gem bound to a weapon that is not currently equipped', () => {
    const { p } = fixture();
    configureHighflyWeaponElement(p, 'not-the-equipped-weapon', 'fire', true);
    expect(highflyWeaponElement(p)).toBe('base');
    expect(highflyElementalFinisherReady(p)).toBe(false);
  });

  it('contains no Q-body or elemental-skill variant runtime', () => {
    const runtime = readFileSync('src/highfly/q2_runtime.ts', 'utf8');
    const overlay = readFileSync('../overlays/apply_highfly_q2_clean.py', 'utf8');
    expect(runtime).not.toMatch(/qmale|qfemale|hf_aff_/i);
    expect(overlay).not.toContain('apply_highfly_character_q0');
    expect(overlay).not.toContain('apply_highfly_q1_combat');
    expect(overlay).not.toContain('affinity_lab_runtime');
  });

  it('reserves exactly ten native skill seats in two non-overlapping crescents', () => {
    const css = readFileSync('src/styles/hf_q2_clean.css', 'utf8');
    const points: Array<{ slot: number; right: number; bottom: number }> = [];
    for (let slot = 1; slot <= 10; slot++) {
      const re = new RegExp(
        '\\[data-hotbar-slot="' + slot + '"\\] \\{ right: (\\d+)px !important; bottom: (\\d+)px !important; \\}',
      );
      const match = css.match(re);
      expect(match, 'missing S' + slot).not.toBeNull();
      points.push({ slot, right: Number(match?.[1]), bottom: Number(match?.[2]) });
    }
    expect(new Set(points.map((p) => p.right + ':' + p.bottom)).size).toBe(10);
    for (let i = 0; i < points.length; i++) {
      for (let j = i + 1; j < points.length; j++) {
        const dx = points[i].right - points[j].right;
        const dy = points[i].bottom - points[j].bottom;
        expect(Math.hypot(dx, dy)).toBeGreaterThanOrEqual(44);
      }
    }
  });
});
