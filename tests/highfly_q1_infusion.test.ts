import { describe, expect, it } from 'vitest';
import { HIGHFLY_AFFINITY_AUDIT_V1 } from '../src/highfly/affinity_lab_loadouts';
import { updateAuras } from '../src/sim/combat/auras';
import { cancelCast, castAbility, updateCasting } from '../src/sim/combat/casting_lifecycle';
import { dealDamage } from '../src/sim/combat/damage';
import {
  applyWeaponInfusionHit, captureWeaponInfusion, configureWeaponInfusion,
  getWeaponInfusion, WEAPON_INFUSION_RECEPTORS,
  type WeaponInfusionMode,
} from '../src/sim/combat/highfly_weapon_affinity';
import { ABILITIES, BUILTIN_WORLD, MOBS } from '../src/sim/data';
import { createMob } from '../src/sim/entity';
import { advancePendingProjectiles } from '../src/sim/projectile_travel';
import { Sim } from '../src/sim/sim';
import type { PlayerClass } from '../src/sim/types';
import { placePlayerInOpenField } from './helpers/open_field';

function fixture(cls: PlayerClass = 'warrior') {
  const sim = new Sim({ seed: 4242, playerClass: cls, autoEquip: true,
    world: { ...BUILTIN_WORLD, camps: [], npcs: {}, groundObjects: [] } });
  sim.setPlayerLevel(20);
  placePlayerInOpenField(sim);
  const p = sim.player;
  const target = createMob(sim.nextId++, MOBS.forest_wolf, 1,
    { x: p.pos.x, y: p.pos.y, z: p.pos.z + 2 });
  target.maxHp = target.hp = 5000;
  target.hostile = true;
  sim.addEntity(target);
  p.facing = 0;
  p.resource = p.maxResource;
  sim.targetEntity(target.id, p.id);
  sim.drainEvents();
  return { sim, p, target };
}

function nativeHit(mode: WeaponInfusionMode, direct = true) {
  const f = fixture();
  configureWeaponInfusion(f.p, f.p.mainhandItemId, mode);
  captureWeaponInfusion(f.p, 'heroic_leap');
  dealDamage(f.sim.ctx, f.p, f.target, 100, false, 'physical',
    ABILITIES.heroic_leap.name, 'hit', false, undefined, direct);
  return f;
}

describe('HIGHFLY Q1 native weapon infusion', () => {
  it('covers exactly the implemented audit receptors and resolves native labels', () => {
    const audited = Object.values(HIGHFLY_AFFINITY_AUDIT_V1)
      .flatMap((row) => row.receptors.filter((r) => r.implemented).map((r) => r.variants.base));
    expect([...WEAPON_INFUSION_RECEPTORS].sort()).toEqual(audited.sort());
    const { p } = fixture();
    configureWeaponInfusion(p, p.mainhandItemId, 'fire');
    for (const id of WEAPON_INFUSION_RECEPTORS) {
      captureWeaponInfusion(p, id);
      expect(getWeaponInfusion(p, ABILITIES[id].name)).toBe('fire');
    }
    expect(getWeaponInfusion(p, 'hf_aff_heroic_leap_fire_01')).toBe('base');
    expect(getWeaponInfusion(p, 'charge')).toBe('base');
  });

  it('leaves unconfigured and BASE entities unchanged and draws no random numbers', () => {
    const { sim, p, target } = fixture();
    const before = JSON.stringify(p);
    let draws = 0;
    sim.rng.setObserver(() => draws++);
    configureWeaponInfusion(p, p.mainhandItemId, 'base');
    captureWeaponInfusion(p, 'heroic_leap');
    applyWeaponInfusionHit(sim.ctx, p, target, 'heroic_leap', 'hit', 100);
    expect(JSON.stringify(p)).toBe(before);
    expect(target.auras).toEqual([]);
    expect(draws).toBe(0);
  });

  it.each(['fire', 'frost', 'lightning'] as const)(
    '%s keeps native primary damage, school and cast identity intact', (mode) => {
      const canonical = JSON.stringify(ABILITIES.heroic_leap);
      const base = nativeHit('base');
      const infused = nativeHit(mode);
      expect(infused.target.hp).toBe(base.target.hp);
      expect(infused.target.hp).toBe(4900);
      const baseDamage = base.sim.drainEvents().filter((e) => e.type === 'damage');
      const infusedDamage = infused.sim.drainEvents().filter((e) => e.type === 'damage');
      expect(infusedDamage).toEqual(baseDamage);
      expect(infused.target.auras.map((a) => a.kind)).toEqual([
        mode === 'fire' ? 'dot' : mode === 'frost' ? 'slow' : 'stun',
      ]);
      expect(JSON.stringify(ABILITIES.heroic_leap)).toBe(canonical);
    },
  );

  it('captures the accepted inlay without rewriting an in-flight cast on selection changes', () => {
    const { p } = fixture();
    configureWeaponInfusion(p, p.mainhandItemId, 'fire');
    captureWeaponInfusion(p, 'fireball');
    configureWeaponInfusion(p, p.mainhandItemId, 'frost');
    expect(getWeaponInfusion(p)).toBe('frost');
    expect(getWeaponInfusion(p, 'fireball')).toBe('fire');
    captureWeaponInfusion(p, 'fireball');
    expect(getWeaponInfusion(p, 'fireball')).toBe('frost');
    p.mainhandItemId = 'different_weapon';
    expect(getWeaponInfusion(p)).toBe('base');
    expect(getWeaponInfusion(p, 'fireball')).toBe('frost');
  });

  it('rejects foreign weapons, misses, zero loss, dead targets and unadmitted casts', () => {
    const { sim, p, target } = fixture();
    configureWeaponInfusion(p, 'foreign_weapon', 'fire');
    captureWeaponInfusion(p, 'heroic_leap');
    expect(getWeaponInfusion(p)).toBe('base');
    configureWeaponInfusion(p, p.mainhandItemId, 'fire');
    applyWeaponInfusionHit(sim.ctx, p, target, 'heroic_leap', 'hit', 100);
    expect(target.auras).toEqual([]);
    captureWeaponInfusion(p, 'heroic_leap');
    applyWeaponInfusionHit(sim.ctx, p, target, 'heroic_leap', 'miss', 100);
    applyWeaponInfusionHit(sim.ctx, p, target, 'heroic_leap', 'hit', 0);
    target.hp = 0;
    applyWeaponInfusionHit(sim.ctx, p, target, 'heroic_leap', 'hit', 100);
    expect(target.auras).toEqual([]);
  });

  it('never infuses periodic damage or recursively refreshes its own burn', () => {
    expect(nativeHit('fire', false).target.auras).toEqual([]);
    const { sim, p, target } = nativeHit('fire');
    sim.drainEvents();
    for (let tick = 0; tick < 120; tick++) updateAuras(sim.ctx, target);
    const burn = sim.drainEvents().filter((e) => e.type === 'damage' &&
      e.sourceId === p.id && e.ability === 'Weapon Fire Infusion');
    expect(burn.map((e) => e.type === 'damage' ? e.amount : 0)).toEqual([2, 2, 2]);
    expect(target.hp).toBe(4894);
    expect(target.auras).toEqual([]);
  });

  it('adds lightning after the original damage removes breakable control', () => {
    const { sim, p, target } = fixture();
    configureWeaponInfusion(p, p.mainhandItemId, 'lightning');
    captureWeaponInfusion(p, 'heroic_leap');
    target.auras.push({ id: 'old_control', name: 'Old Control', kind: 'incapacitate',
      remaining: 10, duration: 10, value: 0, school: 'physical', sourceId: p.id,
      breaksOnDamage: true });
    dealDamage(sim.ctx, p, target, 100, false, 'physical', ABILITIES.heroic_leap.name, 'hit');
    expect(target.auras.map((a) => a.kind)).toEqual(['stun']);
  });

  it('respects native crowd-control immunity', () => {
    const { sim, p, target } = fixture();
    target.ccImmune = true;
    configureWeaponInfusion(p, p.mainhandItemId, 'lightning');
    captureWeaponInfusion(p, 'heroic_leap');
    applyWeaponInfusionHit(sim.ctx, p, target, 'heroic_leap', 'hit', 100);
    expect(target.auras).toEqual([]);
  });

  it('captures a real zero-cooldown Cinderbolt while preserving native cost and damage', () => {
    function run(mode: WeaponInfusionMode) {
      const { sim, p, target } = fixture('mage');
      configureWeaponInfusion(p, p.mainhandItemId, mode);
      const resource = p.resource;
      castAbility(sim.ctx, 'fireball', p.id);
      expect(p.castingAbility).toBe('fireball');
      const meta = sim.players.get(p.id)!;
      for (let i = 0; i < 200 && p.castingAbility; i++) updateCasting(sim.ctx, p, meta);
      for (let i = 0; i < 200 && sim.ctx.pendingProjectiles.length; i++)
        advancePendingProjectiles(sim.ctx);
      const primary = sim.drainEvents().filter((e) => e.type === 'damage');
      expect(primary.length).toBeGreaterThan(0);
      return { primary, spent: resource - p.resource, cooldowns: [...p.cooldowns],
        mode: getWeaponInfusion(p, 'fireball'), kinds: target.auras.map((a) => a.kind) };
    }
    const base = run('base');
    const infused = run('lightning');
    expect(infused.primary).toEqual(base.primary);
    expect(infused.spent).toBe(base.spent);
    expect(infused.cooldowns).toEqual(base.cooldowns);
    expect(infused.mode).toBe('lightning');
    expect(infused.kinds).toContain('stun');
  });

  it('infuses native Rupture once without altering its canonical bleed', () => {
    const { sim, p, target } = fixture('rogue');
    p.comboPoints = 5;
    configureWeaponInfusion(p, p.mainhandItemId, 'frost');
    castAbility(sim.ctx, 'rupture', p.id);
    const bleed = target.auras.find((a) => a.id === 'rupture');
    expect(bleed).toBeTruthy();
    expect(bleed?.duration).toBe(16);
    expect(target.auras.filter((a) => a.kind === 'slow')).toHaveLength(1);
    const slow = target.auras.find((a) => a.kind === 'slow')!;
    for (let tick = 0; tick < 20; tick++) updateAuras(sim.ctx, target);
    expect(slow.remaining).toBeLessThan(4);
  });

  it.each([false, true])('pins real Cinderbolt admission across inlay changes (weapon swap: %s)',
    (swapWeapon) => {
      const { sim, p, target } = fixture('mage');
      configureWeaponInfusion(p, p.mainhandItemId, 'fire');
      castAbility(sim.ctx, 'fireball', p.id);
      expect(p.castingAbility).toBe('fireball');
      if (swapWeapon) p.mainhandItemId = 'lab_replacement_weapon';
      configureWeaponInfusion(p, p.mainhandItemId, 'frost');
      const meta = sim.players.get(p.id)!;
      for (let i = 0; i < 200 && p.castingAbility; i++) updateCasting(sim.ctx, p, meta);
      for (let i = 0; i < 200 && sim.ctx.pendingProjectiles.length; i++)
        advancePendingProjectiles(sim.ctx);
      expect(getWeaponInfusion(p)).toBe('frost');
      expect(getWeaponInfusion(p, 'fireball')).toBe('fire');
      expect(target.auras.some((a) => a.id === `hf_q1_weapon_fire_${p.id}`)).toBe(true);
      expect(target.auras.some((a) => a.id === `hf_q1_weapon_frost_${p.id}`)).toBe(false);
    },
  );

  it('keeps a BASE cast BASE when first opting in during its windup', () => {
    const { sim, p, target } = fixture('mage');
    castAbility(sim.ctx, 'fireball', p.id);
    configureWeaponInfusion(p, p.mainhandItemId, 'fire');
    const meta = sim.players.get(p.id)!;
    for (let i = 0; i < 200 && p.castingAbility; i++) updateCasting(sim.ctx, p, meta);
    for (let i = 0; i < 200 && sim.ctx.pendingProjectiles.length; i++)
      advancePendingProjectiles(sim.ctx);
    expect(getWeaponInfusion(p, 'fireball')).toBe('base');
    expect(target.auras.some((a) => a.id.startsWith('hf_q1_weapon_'))).toBe(false);
  });

  it('clears cancelled admission so the next cast captures its own inlay', () => {
    const { sim, p, target } = fixture('mage');
    configureWeaponInfusion(p, p.mainhandItemId, 'fire');
    castAbility(sim.ctx, 'fireball', p.id);
    cancelCast(sim.ctx, p);
    p.gcdRemaining = 0;
    configureWeaponInfusion(p, p.mainhandItemId, 'frost');
    castAbility(sim.ctx, 'fireball', p.id);
    const meta = sim.players.get(p.id)!;
    for (let i = 0; i < 200 && p.castingAbility; i++) updateCasting(sim.ctx, p, meta);
    for (let i = 0; i < 200 && sim.ctx.pendingProjectiles.length; i++)
      advancePendingProjectiles(sim.ctx);
    expect(getWeaponInfusion(p, 'fireball')).toBe('frost');
    expect(target.auras.some((a) => a.id === `hf_q1_weapon_frost_${p.id}`)).toBe(true);
  });

  it('replays the same lab inputs deterministically', () => {
    function run() {
      const { sim, target } = nativeHit('fire');
      for (let tick = 0; tick < 120; tick++) updateAuras(sim.ctx, target);
      return { hp: target.hp, auras: target.auras, events: sim.drainEvents() };
    }
    expect(run()).toEqual(run());
  });
});
