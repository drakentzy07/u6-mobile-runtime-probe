import { type AnimationClip, type Object3D, PropertyBinding } from 'three';

const adaptedFor = new WeakMap<AnimationClip, Object3D>();

function normalizedName(name: string): string {
  return name.replace(/[[\].:/|]/g, '').toLowerCase();
}

function sceneNodes(scene: Object3D): Map<string, Object3D> {
  const nodes = new Map<string, Object3D>();
  scene.traverse((node) => {
    nodes.set(node.name, node);
    nodes.set(normalizedName(node.name), node);
  });
  return nodes;
}

/** Preserve donor timing, rotations and movement deltas on Q1's anatomical rig.
 * Position tracks in class donors contain the KayKit local rest translations.
 * Replace only that baseline with Q1's rest position; animation displacement
 * stays identical. The simulation continues to own travel and hit windows.
 * Call only for external animUrls, never for Q1's own already-adapted clips. */
export function retargetQ1Clip(
  source: AnimationClip,
  donorScene: Object3D,
  targetScene: Object3D,
): AnimationClip {
  if (donorScene === targetScene || adaptedFor.get(source) === targetScene) return source;
  const donors = sceneNodes(donorScene);
  const targets = sceneNodes(targetScene);
  const clip = source.clone();
  for (const track of clip.tracks) {
    const parsed = PropertyBinding.parseTrackName(track.name);
    if (parsed.propertyName !== 'position') continue;
    const name = parsed.objectName === 'bones' ? String(parsed.objectIndex) : parsed.nodeName;
    const donor = donors.get(name) ?? donors.get(normalizedName(name));
    const target = targets.get(name) ?? targets.get(normalizedName(name));
    if (!donor || !target) continue;
    const dx = target.position.x - donor.position.x;
    const dy = target.position.y - donor.position.y;
    const dz = target.position.z - donor.position.z;
    const values = track.values;
    // glTF cubic splines store in-tangent/value/out-tangent triplets. Shift
    // values only, preserving tangent derivatives and the interpolation mode.
    const cubic = track.getValueSize() === 9;
    const stride = cubic ? 9 : 3;
    const offset = cubic ? 3 : 0;
    for (let i = offset; i < values.length; i += stride) {
      values[i] += dx;
      values[i + 1] += dy;
      values[i + 2] += dz;
    }
  }
  adaptedFor.set(clip, targetScene);
  return clip;
}
