import * as THREE from 'three';
import { ITEMS } from '../sim/data';

type Family = 'sword' | 'dagger' | 'axe' | 'shield' | 'spear' | 'bow' | 'staff' | 'other';

/**
 * Maximum visual long-axis / body-height ratio for a Q1 held prop.
 * Gameplay reach, hit authority, damage and attachment bones remain untouched.
 *
 * Q1 v1 used sword=.52/shield=.35, which was visibly oversized on the
 * Quaternius bodies because those values describe fantasy prop length, not a
 * believable held silhouette. These caps are intentionally conservative and
 * only SHRINK a prop that exceeds them.
 */
export const Q1_WEAPON_BODY_RATIOS: Record<Family, number> = {
  sword: 0.37,
  dagger: 0.18,
  axe: 0.30,
  shield: 0.28,
  spear: 0.66,
  bow: 0.46,
  staff: 0.64,
  other: 0.30,
};

function family(id: string | null | undefined, object: THREE.Object3D): Family {
  const item = id ? ITEMS[id] : undefined;
  const description =
    JSON.stringify(item ?? {}) +
    ' ' +
    object.name +
    ' ' +
    String(object.userData.q1PropUrl ?? '');
  if (/shield/i.test(description)) return 'shield';
  if (/dagger|knife|shiv/i.test(description)) return 'dagger';
  if (/spear|polearm|lance/i.test(description)) return 'spear';
  if (/crossbow|bow/i.test(description)) return 'bow';
  if (/staff|stave/i.test(description)) return 'staff';
  if (/axe|hatchet/i.test(description)) return 'axe';
  if (/sword|blade/i.test(description)) return 'sword';
  return 'other';
}

function bodyHeightAndHolders(model: THREE.Object3D): {
  height: number;
  holders: THREE.Object3D[];
} {
  model.updateMatrixWorld(true);
  const bodyBox = new THREE.Box3();
  const temp = new THREE.Box3();
  const holders: THREE.Object3D[] = [];
  model.traverse((o) => {
    if (o.userData.heldPropHolder) holders.push(o);
    const mesh = o as THREE.SkinnedMesh;
    if (!mesh.isSkinnedMesh || o.userData.weaponMesh) return;
    mesh.geometry.computeBoundingBox();
    if (mesh.geometry.boundingBox) {
      bodyBox.union(temp.copy(mesh.geometry.boundingBox).applyMatrix4(mesh.matrixWorld));
    }
  });
  return { height: bodyBox.max.y - bodyBox.min.y, holders };
}

/**
 * Render-only BODY x WEAPON fit.
 *
 * Every holder keeps the exact authored grip origin, rotation and attachment
 * bone. We cache its authored grip scale once and always recompute from that
 * baseline, so a weapon swap cannot inherit an older weapon's q1Fit scale.
 */
export function fitQ1Weapons(
  model: THREE.Object3D,
  key: string,
  mainhand?: string | null,
  offhand?: string | null,
): void {
  const female = key.endsWith('_qfemale');
  if (!female && !key.endsWith('_qmale')) return;

  const { height, holders } = bodyHeightAndHolders(model);
  if (!Number.isFinite(height) || height <= 0) return;

  for (const holder of holders) {
    const base =
      (holder.userData.q1BaseGripScale as [number, number, number] | undefined) ??
      ([holder.scale.x, holder.scale.y, holder.scale.z] as [number, number, number]);
    holder.userData.q1BaseGripScale = base;
    holder.scale.set(base[0], base[1], base[2]);
    model.updateMatrixWorld(true);

    const left =
      holder.userData.heldSlot === 1 ||
      /(?:hand|handslot)[._]?l$/i.test(String(holder.userData.q1PropBone ?? ''));
    const id = left ? offhand : mainhand;
    const kind = family(id, holder);
    const box = new THREE.Box3().setFromObject(holder);
    const extent = Math.max(
      box.max.x - box.min.x,
      box.max.y - box.min.y,
      box.max.z - box.min.z,
    );
    if (!(extent > 0)) continue;

    // Q-Female gets a tiny silhouette correction while preserving the same
    // family proportions as Q-Male.
    const bodyRatio = Q1_WEAPON_BODY_RATIOS[kind] * (female ? 0.95 : 1);
    const target = height * bodyRatio;
    const factor = Math.min(1, target / extent);
    holder.scale.set(base[0] * factor, base[1] * factor, base[2] * factor);
    model.updateMatrixWorld(true);

    const fitted = new THREE.Box3().setFromObject(holder);
    const fittedExtent = Math.max(
      fitted.max.x - fitted.min.x,
      fitted.max.y - fitted.min.y,
      fitted.max.z - fitted.min.z,
    );
    holder.userData.q1Fit = {
      family: kind,
      scale: factor,
      target,
      extentBefore: extent,
      extentAfter: fittedExtent,
      bodyHeight: height,
      bodyRatio: fittedExtent / height,
    };
  }
}
