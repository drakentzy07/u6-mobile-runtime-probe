import type { Entity, SimEvent } from '../sim/types';
import { getWeaponInfusion } from '../sim/combat/highfly_weapon_affinity';
import { ABILITIES } from '../sim/data';

export function weaponInfusionColor(entity: Entity): number | null {
  const mode = getWeaponInfusion(entity);
  return mode === 'fire' ? 0xff782e : mode === 'frost' ? 0x8bdcff : mode === 'lightning' ? 0x65aaff : null;
}

type NativeVfx = {
  tick(id: number, school: string, color?: number): void;
  nova(id: number, school: string, color?: number): void;
  beam(source: number, target: number, school: string, color?: number): void;
};

// Additional particles use the existing budgeted pool. Native ability VFX still
// receive the original event and preserve their authored presentation.
export function paintWeaponInfusion(event: SimEvent, source: Entity | undefined, vfx: NativeVfx): void {
  if (!source || event.type !== 'damage' || event.kind !== 'hit' || event.amount <= 0) return;
  if (!event.abilityId && event.ability !== ABILITIES.heroic_leap.name) return;
  const mode = getWeaponInfusion(source, event.abilityId ?? event.ability ?? '');
  if (mode === 'fire') vfx.tick(event.targetId, 'fire', 0xff782e);
  if (mode === 'frost') vfx.nova(event.targetId, 'frost', 0x8bdcff);
  if (mode === 'lightning') {
    vfx.beam(event.sourceId, event.targetId, 'nature', 0x65aaff);
    vfx.tick(event.targetId, 'nature', 0xa7d5ff);
  }
}
