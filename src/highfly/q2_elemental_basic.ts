import type { SimContext } from '../sim_context';
import type { Entity } from '../types';

export type HighflyWeaponElement = 'base' | 'fire' | 'frost' | 'lightning' | 'air';

type ElementState = { weaponId: string; mode: HighflyWeaponElement; active: boolean };
type ElementEntity = Entity & { highflyQ2WeaponElement?: ElementState };

export const HIGHFLY_WEAPON_ELEMENTS: readonly HighflyWeaponElement[] = [
  'base',
  'fire',
  'frost',
  'lightning',
  'air',
];

export function configureHighflyWeaponElement(
  entity: Entity,
  weaponId: string | null | undefined,
  mode: HighflyWeaponElement,
  active = true,
): void {
  const actor = entity as ElementEntity;
  if (
    entity.kind !== 'player' ||
    !weaponId ||
    entity.mainhandItemId !== weaponId ||
    !HIGHFLY_WEAPON_ELEMENTS.includes(mode)
  ) {
    actor.highflyQ2WeaponElement = undefined;
    return;
  }
  actor.highflyQ2WeaponElement = { weaponId, mode, active: active && mode !== 'base' };
}

export function highflyWeaponElement(entity: Entity): HighflyWeaponElement {
  const state = (entity as ElementEntity).highflyQ2WeaponElement;
  if (!state?.active || state.weaponId !== entity.mainhandItemId) return 'base';
  return state.mode;
}

export function highflyElementalFinisherReady(entity: Entity): boolean {
  return entity.kind === 'player' && !!entity.mainhandItemId && highflyWeaponElement(entity) !== 'base';
}

export function applyHighflyElementalFinisher(
  ctx: SimContext,
  source: Entity,
  target: Entity,
  mode: Exclude<HighflyWeaponElement, 'base'>,
): void {
  if (source.dead || target.dead || target.hp <= 0 || !ctx.isHostileTo(source, target)) return;
  const common = { sourceId: source.id };
  if (mode === 'fire') {
    ctx.applyAura(target, {
      ...common,
      id: `hf_q2_fire_${source.id}`,
      name: 'HIGHFLY Fire Finisher',
      kind: 'dot',
      remaining: 6,
      duration: 6,
      value: 2,
      tickInterval: 2,
      tickTimer: 2,
      school: 'fire',
      finalDamage: true,
    });
    return;
  }
  if (mode === 'frost') {
    ctx.applyAura(target, {
      ...common,
      id: `hf_q2_frost_${source.id}`,
      name: 'HIGHFLY Frost Finisher',
      kind: 'slow',
      remaining: 4,
      duration: 4,
      value: 0.65,
      school: 'frost',
    });
    return;
  }
  if (mode === 'lightning') {
    ctx.applyAura(target, {
      ...common,
      id: `hf_q2_lightning_${source.id}`,
      name: 'HIGHFLY Lightning Finisher',
      kind: 'stun',
      remaining: 0.35,
      duration: 0.35,
      value: 0,
      school: 'nature',
    });
    return;
  }
  // AIR: displacement only. It adds combo utility without rewriting the
  // original class skill kit or primary white-hit damage.
  ctx.applyKnockback(source, target, 2.5);
}
