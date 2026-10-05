import * as THREE from 'three';
import { ITEMS } from '../sim/data';

type Family = 'sword' | 'dagger' | 'axe' | 'shield' | 'spear' | 'bow' | 'staff' | 'other';
const LENGTH: Record<Family, number> = { sword: .52, dagger: .23, axe: .43, shield: .35, spear: .99, bow: .60, staff: .93, other: .42 };

function family(id: string | null | undefined, object: THREE.Object3D): Family {
  const item = id ? ITEMS[id] : undefined;
  const description = JSON.stringify(item ?? {}) + ' ' + object.name + ' ' + (object.userData.q1PropUrl ?? '');
  if (/shield/i.test(description)) return 'shield';
  if (/dagger|knife|shiv/i.test(description)) return 'dagger';
  if (/spear|polearm|lance/i.test(description)) return 'spear';
  if (/crossbow|bow/i.test(description)) return 'bow';
  if (/staff|stave/i.test(description)) return 'staff';
  if (/axe|hatchet/i.test(description)) return 'axe';
  if (/sword|blade/i.test(description)) return 'sword';
  return 'other';
}

// Render-only fit, measured against the body. Keep the canonical grip origin,
// authored rotation, attachment bones, traces and gameplay reach unchanged.
export function fitQ1Weapons(model: THREE.Object3D, key: string, mainhand?: string | null, offhand?: string | null): void {
  const female = key.endsWith('_qfemale');
  if (!female && !key.endsWith('_qmale')) return;
  model.updateMatrixWorld(true);
  const bodyBox = new THREE.Box3();
  const temp = new THREE.Box3();
  const holders: THREE.Object3D[] = [];
  model.traverse(o => {
    if (o.userData.heldPropHolder) holders.push(o);
    const mesh = o as THREE.SkinnedMesh;
    if (!mesh.isSkinnedMesh || o.userData.weaponMesh) return;
    mesh.geometry.computeBoundingBox();
    if (mesh.geometry.boundingBox) bodyBox.union(temp.copy(mesh.geometry.boundingBox).applyMatrix4(mesh.matrixWorld));
  });
  const height = bodyBox.max.y - bodyBox.min.y;
  if (!Number.isFinite(height) || height <= 0) return;
  for (const holder of holders) {
    if (holder.userData.q1Fit) continue;
    const left = holder.userData.heldSlot === 1 || /(?:hand|handslot)[._]?l$/i.test(holder.userData.q1PropBone ?? '');
    const id = left ? offhand : mainhand;
    const kind = family(id, holder);
    const box = new THREE.Box3().setFromObject(holder);
    const extent = Math.max(box.max.x-box.min.x, box.max.y-box.min.y, box.max.z-box.min.z);
    if (extent > 0) {
      const target = height * LENGTH[kind] * (female ? .96 : 1);
      const scale = Math.min(1, target / extent);
      holder.scale.multiplyScalar(scale);
      // Existing offsets lie in the same prop-local units as its authored grip.
      holder.userData.q1Fit = { family: kind, scale, target };
    }
  }
  model.updateMatrixWorld(true);
}
