import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import sharp from 'sharp';
import { NodeIO } from '@gltf-transform/core';
import { dedup, prune } from '@gltf-transform/functions';
import { Matrix4, Vector3, Quaternion, Matrix3 } from 'three';
import { openGlb, saveGlb } from './scripts/asset_pipeline/lib/glb.mjs';

// Original authored anatomical weights are the authority. The adaptation only
// changes bind-space geometry and collapses donor bones into Rig_Medium names.
// No nearest-bone solver, animation replacement or gameplay adjustment is used.
const root = process.cwd();
const donorRoot = process.env.HF_Q1_DONOR_ROOT || path.resolve(root, '../../donors/originals');
const outRoot = path.resolve(root, 'public/models/chars/players/q0');
fs.mkdirSync(outRoot, { recursive: true });
const referencePath = path.resolve(root, 'public/models/chars/players/knight.glb');
const sha = (data) => crypto.createHash('sha256').update(data).digest('hex');
const hashAccessor = (accessor) => {
  const array = accessor.getArray();
  return sha(new Uint8Array(array.buffer, array.byteOffset, array.byteLength));
};
const round = (x) => +x.toFixed(6);

function bindMatrices(skin) {
  const array = skin.getInverseBindMatrices().getArray();
  return skin.listJoints().map((_, i) => new Matrix4().fromArray(array, i * 16).invert());
}
function position(matrix) { return new Vector3().setFromMatrixPosition(matrix); }
function adaptProportions(doc, donor) {
  const skin = doc.getRoot().listSkins()[0];
  const joints = skin.listJoints(), oldBind = bindMatrices(skin);
  const sourceJoints = donor.listJoints(), sourceBind = bindMatrices(donor);
  const sourceByName = new Map(sourceJoints.map((j, i) => [j.getName().toLowerCase(), position(sourceBind[i])]));
  const byJoint = new Map(joints.map((j, i) => [j, i]));
  const rootIndex = joints.findIndex((j) => j.getName() === 'root');
  const rootBind = oldBind[rootIndex];
  const sourceName = (name) => {
    if (name === 'root') return 'root';
    if (name === 'hips') return 'pelvis';
    if (name === 'spine') return 'spine_01';
    if (name === 'chest') return 'spine_03';
    if (name === 'head') return 'head';
    const [bone, side] = name.split('.');
    return `${({ upperarm: 'upperarm', lowerarm: 'lowerarm', wrist: 'hand', upperleg: 'thigh', lowerleg: 'calf', foot: 'foot', toes: 'ball' })[bone]}_${side}`;
  };
  const newBind = joints.map((j, i) => {
    const name = j.getName();
    let p;
    if (name.startsWith('hand.') || name.startsWith('handslot.')) {
      const side = name.endsWith('.l') ? 'l' : 'r';
      const wrist = sourceByName.get(`hand_${side}`);
      const direction = sourceByName.get(`middle_01_${side}`).clone().sub(wrist).normalize();
      // Palm-center grip is deliberately fitted to the authored donor hand.
      p = wrist.clone().addScaledVector(direction, name.startsWith('handslot.') ? 0.075 : 0.045);
    } else p = sourceByName.get(sourceName(name))?.clone();
    if (!p) throw new Error(`No anatomical proportion anchor for ${name}`);
    return oldBind[i].clone().setPosition(p.applyMatrix4(rootBind));
  });
  const translationMap = new Map();
  for (let i = 0; i < joints.length; i++) {
    const joint = joints[i], parent = joint.getParentNode(), parentIndex = byJoint.get(parent);
    if (parentIndex === undefined) continue;
    const local = newBind[parentIndex].clone().invert().multiply(newBind[i]);
    const before = joint.getTranslation(), after = position(local).toArray();
    translationMap.set(joint, { before, after });
    joint.setTranslation(after);
  }
  const buffer = doc.getRoot().listBuffers()[0];
  const ibm = new Float32Array(newBind.length * 16);
  newBind.forEach((b, i) => b.clone().invert().toArray(ibm, i * 16));
  skin.setInverseBindMatrices(doc.createAccessor().setArray(ibm).setType('MAT4').setBuffer(buffer));
  for (const animation of doc.getRoot().listAnimations()) for (const channel of animation.listChannels()) {
    if (channel.getTargetPath() !== 'translation') continue;
    const mapping = translationMap.get(channel.getTargetNode());
    if (!mapping) continue;
    const sampler = channel.getSampler(), original = sampler.getOutput();
    const array = new Float32Array(original.getArray());
    for (let i = 0; i < array.length; i++) array[i] += mapping.after[i % 3] - mapping.before[i % 3];
    sampler.setOutput(doc.createAccessor().setArray(array).setType(original.getType()).setBuffer(buffer));
  }
  return { method: 'canonical-rotation-clips-with-authored-anatomical-bind-and-translation-offsets', changedJointTranslations: [...translationMap].map(([j, { before, after }]) => ({ joint: j.getName(), before, after })) };
}
function digestRig(doc) {
  const skin = doc.getRoot().listSkins()[0];
  return {
    joints: skin.listJoints().map((j) => j.getName()),
    bindHash: hashAccessor(skin.getInverseBindMatrices()),
    clips: doc.getRoot().listAnimations().map((a) => ({
      name: a.getName(),
      channels: a.listChannels().map((c) => ({
        joint: c.getTargetNode()?.getName(), path: c.getTargetPath(),
        interpolation: c.getSampler().getInterpolation(),
        input: hashAccessor(c.getSampler().getInput()),
        output: hashAccessor(c.getSampler().getOutput()),
      })),
    })),
  };
}

async function readOriginal(file) {
  const json = JSON.parse(fs.readFileSync(file, 'utf8'));
  // Two normal filenames in the vendor export differ from the zip's names.
  // Missing optional normal maps are omitted; base color is mandatory and exact.
  for (const material of json.materials || []) {
    for (const key of ['normalTexture', 'occlusionTexture']) {
      const texture = material[key];
      if (!texture) continue;
      const image = json.images[json.textures[texture.index].source];
      if (!fs.existsSync(path.join(path.dirname(file), image.uri))) delete material[key];
    }
  }
  const resources = {};
  for (const b of json.buffers) resources[b.uri] = new Uint8Array(fs.readFileSync(path.join(path.dirname(file), b.uri)));
  for (const image of json.images || []) {
    const filePath = path.join(path.dirname(file), image.uri);
    resources[image.uri] = fs.existsSync(filePath) ? new Uint8Array(fs.readFileSync(filePath)) : new Uint8Array();
  }
  return new NodeIO().readJSON({ json, resources });
}

function makeTransfer(source, target) {
  const sj = source.listJoints();
  const tj = target.listJoints();
  const sb = bindMatrices(source);
  const tb = bindMatrices(target);
  const si = new Map(sj.map((j, i) => [j.getName().toLowerCase(), i]));
  const ti = new Map(tj.map((j, i) => [j.getName(), i]));
  const sp = (name) => position(sb[si.get(name.toLowerCase())]);
  const tp = (name) => position(tb[ti.get(name)]);
  const transverse = tp('head').distanceTo(tp('chest')) / sp('head').distanceTo(sp('spine_03'));
  const transforms = new Map();

  function segment(sourceName, sourceEnd, targetName, targetEnd, transverseFactor = 1) {
    const a = sp(sourceName), b = sp(sourceEnd);
    const c = tp(targetName), d = tp(targetEnd);
    const u = b.clone().sub(a), v = d.clone().sub(c);
    const lengthwise = v.length() / u.length();
    u.normalize(); v.normalize();
    const rotation = new Matrix4().makeRotationFromQuaternion(new Quaternion().setFromUnitVectors(u, v));
    // Scale along the actual anatomical bone, keeping the cross-section uniform.
    const cross = transverse * transverseFactor;
    const stretch = new Matrix4().set(
      cross + (lengthwise - cross) * u.x * u.x, (lengthwise - cross) * u.x * u.y, (lengthwise - cross) * u.x * u.z, 0,
      (lengthwise - cross) * u.y * u.x, cross + (lengthwise - cross) * u.y * u.y, (lengthwise - cross) * u.y * u.z, 0,
      (lengthwise - cross) * u.z * u.x, (lengthwise - cross) * u.z * u.y, cross + (lengthwise - cross) * u.z * u.z, 0,
      0, 0, 0, 1,
    );
    const matrix = new Matrix4().makeTranslation(c.x, c.y, c.z).multiply(rotation).multiply(stretch).multiply(new Matrix4().makeTranslation(-a.x, -a.y, -a.z));
    transforms.set(sourceName.toLowerCase(), { matrix, targetIndex: ti.get(targetName), sourceName, targetName, lengthwise, cross });
  }
  segment('pelvis', 'spine_01', 'hips', 'spine');
  segment('spine_01', 'spine_03', 'spine', 'chest');
  segment('spine_03', 'head', 'chest', 'head');
  const headMatrix = new Matrix4().makeTranslation(...tp('head').toArray()).multiply(new Matrix4().makeScale(transverse, transverse, transverse)).multiply(new Matrix4().makeTranslation(...sp('head').negate().toArray()));
  transforms.set('head', { matrix: headMatrix, targetIndex: ti.get('head'), sourceName: 'head', targetName: 'head', lengthwise: transverse, cross: transverse });
  for (const side of ['l', 'r']) {
    segment(`upperarm_${side}`, `lowerarm_${side}`, `upperarm.${side}`, `lowerarm.${side}`, 0.85);
    segment(`lowerarm_${side}`, `hand_${side}`, `lowerarm.${side}`, `wrist.${side}`, 0.85);
    const src = sp(`hand_${side}`), dst = tp(`wrist.${side}`);
    const handMatrix = new Matrix4().makeTranslation(...dst.toArray()).multiply(new Matrix4().makeScale(transverse * 0.85, transverse * 0.85, transverse * 0.85)).multiply(new Matrix4().makeTranslation(...src.negate().toArray()));
    transforms.set(`hand_${side}`, { matrix: handMatrix, targetIndex: ti.get(`wrist.${side}`), sourceName: `hand_${side}`, targetName: `wrist.${side}`, lengthwise: transverse * 0.85, cross: transverse * 0.85 });
    segment(`thigh_${side}`, `calf_${side}`, `upperleg.${side}`, `lowerleg.${side}`, 0.85);
    segment(`calf_${side}`, `foot_${side}`, `lowerleg.${side}`, `foot.${side}`, 0.85);
    segment(`foot_${side}`, `ball_${side}`, `foot.${side}`, `toes.${side}`, 0.85);
    const foot = transforms.get(`foot_${side}`);
    transforms.set(`ball_${side}`, { ...foot, targetIndex: ti.get(`toes.${side}`), sourceName: `ball_${side}`, targetName: `toes.${side}` });
  }
  const all = sj.map((joint) => {
    const name = joint.getName().toLowerCase();
    let key = name;
    if (name === 'root') key = 'pelvis';
    else if (name === 'spine_02') key = 'spine_01';
    else if (name.startsWith('clavicle')) key = 'spine_03';
    else if (name.startsWith('neck')) key = 'head';
    else if (/^(index|middle|pinky|ring|thumb)_/.test(name)) key = `hand_${name.endsWith('_l') ? 'l' : 'r'}`;
    else if (name.startsWith('ball_leaf')) key = `ball_${name.endsWith('_l') ? 'l' : 'r'}`;
    const transform = transforms.get(key);
    if (!transform) throw new Error(`Unmapped original anatomical joint: ${name}`);
    return { ...transform, normal: new Matrix3().getNormalMatrix(transform.matrix) };
  });
  return { all, transverse, map: all.map((t, i) => ({ source: sj[i].getName(), target: t.targetName })), primary: [...transforms.values()].map(({ sourceName, targetName, lengthwise, cross }) => ({ sourceName, targetName, lengthwise: round(lengthwise), cross: round(cross) })) };
}

async function copyMaterial(doc, material, textures) {
  if (!material?.getBaseColorTexture()) throw new Error('Original material has no base-color texture');
  const result = doc.createMaterial(material.getName()).setBaseColorFactor(material.getBaseColorFactor()).setMetallicFactor(material.getMetallicFactor()).setRoughnessFactor(material.getRoughnessFactor()).setDoubleSided(material.getDoubleSided());
  for (const slot of ['BaseColor', 'Normal', 'MetallicRoughness']) {
    const tex = material[`get${slot}Texture`]();
    if (!tex) continue;
    let copied = textures.get(tex);
    if (!copied) {
      const original = tex.getImage();
      if (!original?.length) continue;
      const image = await sharp(original).resize({ width: 1024, height: 1024, fit: 'inside', withoutEnlargement: true }).webp({ quality: 90 }).toBuffer();
      copied = doc.createTexture(tex.getName()).setImage(new Uint8Array(image)).setMimeType('image/webp');
      textures.set(tex, copied);
    }
    result[`set${slot}Texture`](copied);
  }
  return result;
}

async function build(job) {
  const doc = await openGlb(referencePath);
  const originalRig = digestRig(doc);
  const persistedSource = path.resolve(root, `../character_q1/work/q1_${job.id}_source.glb`);
  const source = fs.existsSync(persistedSource) ? await openGlb(persistedSource) : await readOriginal(path.join(donorRoot, job.source));
  if (!fs.existsSync(persistedSource)) {
    fs.mkdirSync(path.dirname(persistedSource), { recursive: true });
    await source.transform(prune(), dedup());
    for (const texture of source.getRoot().listTextures()) {
      const image = await sharp(texture.getImage()).resize({ width: 1024, height: 1024, fit: 'inside', withoutEnlargement: true }).webp({ quality: 95 }).toBuffer();
      texture.setImage(new Uint8Array(image)).setMimeType('image/webp');
    }
    await saveGlb(source, persistedSource);
  }
  const adaptation = adaptProportions(doc, source.getRoot().listSkins()[0]);
  const expectedRig = digestRig(doc);
  const rotationChannels = (rig) => rig.clips.map((clip) => ({ name: clip.name, channels: clip.channels.filter((c) => c.path !== 'translation') }));
  const rotationParity = JSON.stringify(rotationChannels(originalRig)) === JSON.stringify(rotationChannels(expectedRig));
  if (!rotationParity || JSON.stringify(originalRig.joints) !== JSON.stringify(expectedRig.joints)) throw new Error('Proportion adaptation changed canonical rotation samples or joint names');
  const target = doc.getRoot().listSkins()[0];
  const transfer = makeTransfer(source.getRoot().listSkins()[0], target);
  const rootDoc = doc.getRoot();
  for (const node of rootDoc.listNodes()) if (node.getMesh()) node.setMesh(null);
  for (const mesh of rootDoc.listMeshes()) mesh.dispose();
  const buffer = rootDoc.listBuffers()[0];
  const accessor = (array, type) => doc.createAccessor().setArray(array).setType(type).setBuffer(buffer);
  const textureCache = new Map();
  const materialCache = new Map();
  const mesh = doc.createMesh(`HIGHFLY_Q1_${job.id}`);
  let vertices = 0, tris = 0, sourceWeightError = 0, finalWeightError = 0;
  let lowerBodyTorsoLeak = 0, lowerBodyVertices = 0;
  const pb = [Infinity, Infinity, Infinity], qb = [-Infinity, -Infinity, -Infinity];
  for (const sourceMesh of source.getRoot().listMeshes()) for (const prim of sourceMesh.listPrimitives()) {
    const p = prim.getAttribute('POSITION').getArray();
    const n = prim.getAttribute('NORMAL')?.getArray();
    const j = prim.getAttribute('JOINTS_0').getArray();
    const w = prim.getAttribute('WEIGHTS_0').getArray();
    const newP = new Float32Array(p.length), newN = n ? new Float32Array(n.length) : null;
    const newJ = new Uint16Array(j.length), newW = new Float32Array(w.length);
    vertices += p.length / 3;
    tris += (prim.getIndices()?.getCount() || p.length / 3) / 3;
    for (let v = 0; v < p.length / 3; v++) {
      const original = new Vector3().fromArray(p, v * 3);
      const normal = n ? new Vector3().fromArray(n, v * 3) : null;
      const moved = new Vector3(), movedNormal = new Vector3();
      const collapsed = new Map();
      const total = Array.from(w.slice(v * 4, v * 4 + 4)).reduce((a, b) => a + b, 0);
      if (total < 0.999) throw new Error('Original donor has unweighted vertex');
      sourceWeightError = Math.max(sourceWeightError, Math.abs(1 - total));
      for (let k = 0; k < 4; k++) {
        const weight = w[v * 4 + k] / total;
        if (!weight) continue;
        const t = transfer.all[j[v * 4 + k]];
        moved.addScaledVector(original.clone().applyMatrix4(t.matrix), weight);
        if (normal) movedNormal.addScaledVector(normal.clone().applyMatrix3(t.normal).normalize(), weight);
        collapsed.set(t.targetIndex, (collapsed.get(t.targetIndex) || 0) + weight);
      }
      moved.toArray(newP, v * 3);
      if (newN) movedNormal.normalize().toArray(newN, v * 3);
      const sorted = [...collapsed].sort((a, b) => b[1] - a[1]);
      for (let k = 0; k < 4; k++) { newJ[v * 4 + k] = sorted[k]?.[0] || 0; newW[v * 4 + k] = sorted[k]?.[1] || 0; }
      const finalSum = Array.from(newW.slice(v * 4, v * 4 + 4)).reduce((a, b) => a + b, 0);
      finalWeightError = Math.max(finalWeightError, Math.abs(1 - finalSum));
      if (original.y < 0.45) {
        lowerBodyVertices++;
        for (const [index, weight] of sorted) if (!/^(upperleg|lowerleg|foot|toes)\./.test(target.listJoints()[index].getName()) && weight > 0.001) lowerBodyTorsoLeak++;
      }
      for (let axis = 0; axis < 3; axis++) { pb[axis] = Math.min(pb[axis], newP[v * 3 + axis]); qb[axis] = Math.max(qb[axis], newP[v * 3 + axis]); }
    }
    let material = materialCache.get(prim.getMaterial());
    if (!material) { material = await copyMaterial(doc, prim.getMaterial(), textureCache); materialCache.set(prim.getMaterial(), material); }
    const built = doc.createPrimitive().setMode(prim.getMode()).setMaterial(material).setAttribute('POSITION', accessor(newP, 'VEC3')).setAttribute('JOINTS_0', accessor(newJ, 'VEC4')).setAttribute('WEIGHTS_0', accessor(newW, 'VEC4'));
    if (newN) built.setAttribute('NORMAL', accessor(newN, 'VEC3'));
    const uv = prim.getAttribute('TEXCOORD_0');
    if (uv) built.setAttribute('TEXCOORD_0', accessor(new Float32Array(uv.getArray()), 'VEC2'));
    if (prim.getIndices()) built.setIndices(accessor(prim.getIndices().getArray().slice(), 'SCALAR'));
    mesh.addPrimitive(built);
  }
  rootDoc.listScenes()[0].addChild(doc.createNode('body').setMesh(mesh).setSkin(target));
  await doc.transform(prune(), dedup());
  const file = path.join(outRoot, job.out);
  await saveGlb(doc, file);
  const written = await openGlb(file);
  const actualRig = digestRig(written);
  const exactRig = JSON.stringify(expectedRig) === JSON.stringify(actualRig);
  if (!exactRig || lowerBodyTorsoLeak || finalWeightError > 1e-6) throw new Error(`${job.id}: canonical rig or anatomical weight gate failed`);
  return {
    file: job.out, bytes: fs.statSync(file).size, vertices, tris,
    method: 'authored-anatomical-weights-with-bind-space-segment-transfer',
    exactExportedAdaptedRigAndAllClipSamples: exactRig,
    canonicalJointVocabularyAndRotationClipsPreserved: rotationParity,
    anatomicalAdaptation: adaptation,
    joints: actualRig.joints, animations: actualRig.clips.map((a) => a.name),
    sourceWeightError, finalWeightError, lowerBodyVertices, lowerBodyTorsoLeak,
    bindBounds: { min: pb.map(round), max: qb.map(round) },
    materials: written.getRoot().listMaterials().map((m) => ({ name: m.getName(), baseColorTexture: m.getBaseColorTexture()?.getName(), baseColorFactor: m.getBaseColorFactor() })),
    textures: written.getRoot().listTextures().map((t) => ({ name: t.getName(), bytes: t.getImage()?.length, mime: t.getMimeType(), size: t.getSize() })),
    mapping: transfer.map, segments: transfer.primary,
  };
}

const report = { reference: 'knight.glb', candidates: {} };
for (const job of [
  { id: 'qmale', source: 'Superhero_Male_FullBody.gltf', out: 'hf_q0_male_rigmedium.glb' },
  { id: 'qfemale', source: 'Superhero_Female_FullBody.gltf', out: 'hf_q0_female_rigmedium.glb' },
]) report.candidates[job.id] = await build(job);
fs.writeFileSync(path.resolve(root, '../character-q1-rig-report.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
