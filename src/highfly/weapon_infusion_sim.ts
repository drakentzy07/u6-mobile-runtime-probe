// Isolated HIGHFLY Q1 lab input. Native ability content and resolution stay intact.
// State belongs to the opted-in entity, never a module-global gameplay cache.
import { ABILITIES } from '../data';
import type { SimContext } from '../sim_context';
import type { DamageEventKind, Entity } from '../types';

export type WeaponInfusionMode = 'base' | 'fire' | 'frost' | 'lightning';

export const WEAPON_INFUSION_RECEPTORS = [
  'heroic_leap', 'whirlwind', 'thunder_clap', 'cleave',
  'eviscerate', 'ambush', 'sinister_strike', 'rupture',
  'fireball', 'arcane_missiles', 'frostbolt', 'frost_nova',
] as const;

interface InfusionSnapshot {
  weaponId: string;
  mode: WeaponInfusionMode;
}

interface InfusionState extends InfusionSnapshot {
  casts: Record<string, InfusionSnapshot>;
  pending?: InfusionSnapshot & { abilityId: string };
}

type LabEntity = Entity & { highflyQ1WeaponInfusion?: InfusionState };

function receptorId(idOrName: string | null | undefined): string | undefined {
  if (!idOrName) return undefined;
  return WEAPON_INFUSION_RECEPTORS.find(
    (id) => id === idOrName || ABILITIES[id]?.name === idOrName,
  );
}

/** Explicit lab mastery/inlay input. A mismatched or unequipped weapon is refused. */
export function configureWeaponInfusion(
  entity: Entity,
  weaponId: string | null | undefined,
  mode: WeaponInfusionMode,
): void {
  const actor = entity as LabEntity;
  if (entity.kind !== 'player' || !weaponId || entity.mainhandItemId !== weaponId ||
      !['base', 'fire', 'frost', 'lightning'].includes(mode)) {
    // Already launched casts keep their accepted payload after a weapon swap.
    if (actor.highflyQ1WeaponInfusion) {
      actor.highflyQ1WeaponInfusion.mode = 'base';
      actor.highflyQ1WeaponInfusion.weaponId = '';
    }
    return;
  }
  const prior = actor.highflyQ1WeaponInfusion;
  // BASE with no prior opt-in leaves the ordinary entity byte-identical.
  if (mode === 'base' && !prior) return;
  actor.highflyQ1WeaponInfusion = {
    weaponId,
    mode,
    casts: prior?.casts ?? {},
    // First opting in during an ordinary windup cannot retrofit that cast.
    pending: prior?.pending ?? (
      entity.castingAbility && !entity.channeling && receptorId(entity.castingAbility)
        ? { abilityId: entity.castingAbility, weaponId, mode: 'base' }
        : undefined
    ),
  };
}

/** Native timed-cast admission pins the payload before any windup input changes. */
export function beginWeaponInfusionCast(entity: Entity, abilityId: string): void {
  const state = (entity as LabEntity).highflyQ1WeaponInfusion;
  if (!state) return;
  const id = receptorId(abilityId);
  state.pending = id ? {
    abilityId: id,
    weaponId: entity.mainhandItemId ?? '',
    mode: state.weaponId === entity.mainhandItemId ? state.mode : 'base',
  } : undefined;
}

export function clearPendingWeaponInfusion(entity: Entity): void {
  const state = (entity as LabEntity).highflyQ1WeaponInfusion;
  if (state) delete state.pending;
}

/** Capture only after native cast admission; zero-cooldown abilities count too. */
export function captureWeaponInfusion(entity: Entity, abilityId: string): void {
  const state = (entity as LabEntity).highflyQ1WeaponInfusion;
  const id = receptorId(abilityId);
  if (!state || !id) return;
  if (state.pending?.abilityId === id) {
    state.casts[id] = { weaponId: state.pending.weaponId, mode: state.pending.mode };
    delete state.pending;
    return;
  }
  state.casts[id] = {
    weaponId: entity.mainhandItemId ?? '',
    mode: state.weaponId === entity.mainhandItemId ? state.mode : 'base',
  };
}

/** Current inlay without an id, accepted-cast snapshot with an id or native name. */
export function getWeaponInfusion(
  entity: Entity,
  abilityIdOrName?: string | null,
): WeaponInfusionMode {
  const state = (entity as LabEntity).highflyQ1WeaponInfusion;
  if (!state) return 'base';
  if (abilityIdOrName === undefined)
    return state.weaponId === entity.mainhandItemId ? state.mode : 'base';
  const id = receptorId(abilityIdOrName);
  const snapshot = id ? state.casts[id] : undefined;
  return snapshot?.mode ?? 'base';
}

/** Adds a native aura after landed HP loss; never changes the primary damage. */
export function applyWeaponInfusionHit(
  ctx: SimContext,
  source: Entity | null,
  target: Entity,
  abilityIdOrName: string | null,
  kind: DamageEventKind,
  amount: number,
): void {
  if (!source || source.kind !== 'player' || source.dead || source.id === target.id ||
      target.dead || target.hp <= 0 || kind !== 'hit' || amount <= 0 ||
      !ctx.isHostileTo(source, target)) return;
  const mode = getWeaponInfusion(source, abilityIdOrName);
  if (mode === 'base') return;
  const common = {
    id: `hf_q1_weapon_${mode}_${source.id}`,
    sourceId: source.id,
  };
  if (mode === 'fire') {
    ctx.applyAura(target, {
      ...common,
      name: 'Weapon Fire Infusion',
      kind: 'dot',
      remaining: 6,
      duration: 6,
      value: 2,
      tickInterval: 2,
      tickTimer: 2,
      school: 'fire',
      finalDamage: true,
    });
  } else if (mode === 'frost') {
    ctx.applyAura(target, {
      ...common,
      name: 'Weapon Frost Infusion',
      kind: 'slow',
      remaining: 4,
      duration: 4,
      value: 0.65,
      school: 'frost',
    });
  } else {
    ctx.applyAura(target, {
      ...common,
      name: 'Weapon Lightning Infusion',
      kind: 'stun',
      remaining: 0.35,
      duration: 0.35,
      value: 0,
      school: 'nature',
    });
  }
}
