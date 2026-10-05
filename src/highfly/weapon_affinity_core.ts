import {
  HIGHFLY_AFFINITY_AUDIT_V1,
  type HighflyAffinity,
  type HighflyClass,
} from './affinity_lab_loadouts';

export type WeaponAffinityMap = Record<string, HighflyAffinity>;
export const AFFINITY_MODES: readonly HighflyAffinity[] = ['base', 'fire', 'frost', 'lightning'];

export function weaponAffinity(
  map: WeaponAffinityMap,
  weaponId: string | null | undefined,
): HighflyAffinity {
  const mode = weaponId ? map[weaponId] : undefined;
  return mode && AFFINITY_MODES.includes(mode) ? mode : 'base';
}

export function affinityAbilityId(
  cls: HighflyClass,
  baseId: string,
  mode: HighflyAffinity,
): string {
  const receptor = HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.find((r) => r.variants.base === baseId);
  return receptor?.implemented ? (receptor.variants[mode] ?? baseId) : baseId;
}

export function canonicalAffinityAbilityId(id: string): string {
  return id.replace(/^hf_aff_(.+)_(?:fire|frost|lightning)_01$/, '$1');
}

export function buildAffinityHotbar(
  cls: HighflyClass,
  known: readonly {
    def: {
      id: string;
      passive?: boolean;
      hiddenFromPlayer?: boolean;
      spec?: string;
      requiresStealth?: boolean;
    };
  }[],
  _mode: HighflyAffinity = 'base',
): { type: 'ability'; id: string }[] {
  const eligible = known
    .filter((k) => !k.def.passive && !k.def.hiddenFromPlayer && !k.def.id.startsWith('hf_aff_'))
    .map((k) => k.def.id);
  const available = new Set(eligible);
  const priority = HIGHFLY_AFFINITY_AUDIT_V1[cls].receptors.map((r) => r.variants.base);
  const ids = [...new Set([...priority.filter((id) => available.has(id)), ...eligible])].slice(
    0,
    10,
  );
  // The equipped weapon infuses accepted native casts in SIM. Slot ids never change.
  return ids.map((id) => ({ type: 'ability', id }));
}

export function parseWeaponAffinities(serialized: string | null): WeaponAffinityMap {
  const map: WeaponAffinityMap = Object.create(null);
  try {
    const parsed: unknown = JSON.parse(serialized ?? '{}');
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      for (const [id, mode] of Object.entries(parsed)) {
        if (typeof mode === 'string' && AFFINITY_MODES.includes(mode as HighflyAffinity))
          map[id] = mode as HighflyAffinity;
      }
    }
  } catch {
    /* Ignore obsolete or invalid lab preferences. */
  }
  return map;
}
