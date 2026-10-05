import fs from 'node:fs';
import { Matrix4, Vector3, Quaternion } from 'three';
import { openGlb } from './scripts/asset_pipeline/lib/glb.mjs';

// Skin the exported geometry with the exact exported animation samples in CPU.
// Edge stretch detects joint seams and broken ankle influences independently of
// texture appearance. Ratios compare posed geometry with its own bind geometry.
function percentile(values, p) {
  values.sort((a, b) => a - b);
  return +(values[Math.floor((values.length - 1) * p)] || 0).toFixed(4);
}
function sample(accessor, i) { const out = []; return accessor.getElement(i, out); }
function animate(animation, time) {
  for (const c of animation.listChannels()) {
    const s = c.getSampler(), input = s.getInput(), output = s.getOutput();
    let i = 0;
    while (i + 1 < input.getCount() && sample(input, i + 1)[0] <= time) i++;
    const k = Math.min(i + 1, input.getCount() - 1);
    const a = sample(input, i)[0], b = sample(input, k)[0];
    const t = s.getInterpolation() === 'STEP' || b === a ? 0 : Math.max(0, Math.min(1, (time - a) / (b - a)));
    const va = sample(output, i), vb = sample(output, k);
    const node = c.getTargetNode();
    if (c.getTargetPath() === 'rotation') node.setRotation(new Quaternion().fromArray(va).slerp(new Quaternion().fromArray(vb), t).toArray());
    else if (c.getTargetPath() === 'translation') node.setTranslation(new Vector3().fromArray(va).lerp(new Vector3().fromArray(vb), t).toArray());
    else if (c.getTargetPath() === 'scale') node.setScale(new Vector3().fromArray(va).lerp(new Vector3().fromArray(vb), t).toArray());
  }
}
async function check(file) {
  const doc = await openGlb(file), root = doc.getRoot(), skin = root.listSkins()[0];
  const joints = skin.listJoints();
  const ibm = skin.getInverseBindMatrices().getArray();
  const transforms = root.listNodes().map((n) => ({ n, t: n.getTranslation(), r: n.getRotation(), s: n.getScale() }));
  const primitives = root.listMeshes().flatMap((m) => m.listPrimitives()).filter((p) => p.getAttribute('WEIGHTS_0'));
  const allRatios = [], lowerRatios = [], clips = [];
  for (const animation of root.listAnimations()) {
    let duration = 0;
    for (const sampler of animation.listSamplers()) duration = Math.max(duration, sampler.getInput().getMax([0])[0]);
    const clipRatios = [], clipLower = [];
    for (const phase of [0, 0.25, 0.5, 0.75, 1]) {
      for (const { n, t, r, s } of transforms) n.setTranslation(t).setRotation(r).setScale(s);
      animate(animation, duration * phase);
      const matrices = joints.map((j, i) => new Matrix4().fromArray(j.getWorldMatrix()).multiply(new Matrix4().fromArray(ibm, i * 16)));
      for (const prim of primitives) {
        const pos = prim.getAttribute('POSITION').getArray(), ja = prim.getAttribute('JOINTS_0').getArray(), wa = prim.getAttribute('WEIGHTS_0').getArray();
        const posed = new Float32Array(pos.length);
        for (let v = 0; v < pos.length / 3; v++) {
          const original = new Vector3().fromArray(pos, v * 3), result = new Vector3();
          for (let k = 0; k < 4; k++) if (wa[v * 4 + k]) result.addScaledVector(original.clone().applyMatrix4(matrices[ja[v * 4 + k]]), wa[v * 4 + k]);
          result.toArray(posed, v * 3);
        }
        const idx = prim.getIndices()?.getArray() || Array.from({ length: pos.length / 3 }, (_, i) => i);
        for (let i = 0; i < idx.length; i += 3) for (let k = 0; k < 3; k++) {
          const a = idx[i + k], b = idx[i + (k + 1) % 3];
          const va = new Vector3().fromArray(pos, a * 3), vb = new Vector3().fromArray(pos, b * 3);
          const distance = va.distanceTo(vb);
          if (distance < 0.005) continue;
          const ratio = new Vector3().fromArray(posed, a * 3).distanceTo(new Vector3().fromArray(posed, b * 3)) / distance;
          if (!Number.isFinite(ratio)) throw new Error('Nonfinite skinning output');
          clipRatios.push(ratio); allRatios.push(ratio);
          const names = [a, b].flatMap((v) => Array.from({ length: 4 }, (_, k) => wa[v * 4 + k] > 0.01 ? joints[ja[v * 4 + k]].getName() : ''));
          if (names.every((name) => !name || /^(lowerleg|foot|toes)\./.test(name))) { clipLower.push(ratio); lowerRatios.push(ratio); }
        }
      }
    }
    clips.push({ name: animation.getName(), p95: percentile(clipRatios, 0.95), p99: percentile(clipRatios, 0.99), maximum: percentile(clipRatios, 1), ankleP99: percentile(clipLower, 0.99), ankleMaximum: percentile(clipLower, 1) });
  }
  return { file, sampledClips: clips.length, phasesPerClip: 5, edgeSamples: allRatios.length, p95: percentile(allRatios, 0.95), p99: percentile(allRatios, 0.99), maximum: percentile(allRatios, 1), ankleEdgeSamples: lowerRatios.length, ankleP99: percentile(lowerRatios, 0.99), ankleMaximum: percentile(lowerRatios, 1), clips };
}
const report = [];
for (const file of process.argv.slice(2)) report.push(await check(file));
fs.writeFileSync('../character-q1-pose-report.json', JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
